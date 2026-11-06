// GameTransport.cs
//
// Copyright (C) 2024, OpenHellion contributors
//
// Inspiration taken from WatsonTcp.
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using OpenHellion.IO;
using UnityEngine;
using ZeroGravity.Network;
using Cysharp.Threading.Tasks;


namespace OpenHellion.Net
{
	/// <summary>
	/// 	Lightweight single-connection game transport with framing.
	/// </summary>
	/// <remarks>
	/// 	Does not support TLS. Depends upon <c>ProtoSerialiser</c> and <c>NetworkData</c>.
	/// 	Insipred by WatsonTcp.
	/// </remarks>
	internal sealed class GameTransport
	{
		private const int TIMEOUT_MS = 4000;

		private const int MAX_MESSAGE_SIZE = 16000000;

		private Action _onDisconnected;
		private Socket _client;
		private NetworkStream _connectionStream;
		private bool _isConnectionOpen;
		private bool _disconnectPending;
		private bool _disconnectRaised;

		internal int ReceivedCount;
		internal int DroppedCount;

		private readonly ConcurrentQueue<(NetworkData Data, DateTime ReceivedUtc)> _inboundQueue = new();

		// Requests awaiting a matching sync response from the server, keyed by conversation.
		private readonly ConcurrentDictionary<Guid, PendingRequest> _pendingRequests = new();

		private CancellationTokenSource _cancellationToken = new CancellationTokenSource();

		private sealed class PendingRequest
		{
			public UniTaskCompletionSource<NetworkData> Completion;
			public DateTime ExpiresUtc;
		}

		internal GameTransport(Action onDisconnected)
		{
			_onDisconnected = onDisconnected;
		}

		/// <summary>
		/// 	Establish a connection to a specified game server.
		/// </summary>
		internal async UniTask Connect(string ip, int port)
		{
			_client = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
			{
				Blocking = true
			};

			try
			{
				await _client.ConnectAsync(new IPEndPoint(IPAddress.Parse(ip), port));
			}
			finally
			{
				await UniTask.SwitchToMainThread();
			}

			_connectionStream = new NetworkStream(_client, true);
			_isConnectionOpen = true;

			UniTask.RunOnThreadPool(() => ListenTask(_cancellationToken.Token), false).Forget();
		}

		internal async UniTask ListenTask(CancellationToken token)
		{
			while (_isConnectionOpen)
			{
				try
				{
					NetworkData networkData = await ProtoSerialiser.Unpack(_connectionStream, MAX_MESSAGE_SIZE, token);
					if (networkData != null)
					{
						_inboundQueue.Enqueue((networkData, DateTime.UtcNow));
					}
				}
				catch (OperationCanceledException)
				{
					// Expected when the connection is being torn down.
					break;
				}
				catch (ArgumentException ex)
				{
					// Malformed or oversized message; log it and keep listening.
					Debug.LogException(ex);
				}
				catch (Exception)
				{
					// Connection reset (IOException/SocketException) or closed by the remote host.
					Debug.Log("Socket terminated, disconnecting client.");
					DisconnectInternal();
					break;
				}
			}
		}

		/// <summary>
		/// 	Dispatch everything received since the last tick on the main thread.
		/// </summary>
		internal void Tick()
		{
			while (_inboundQueue.TryDequeue(out (NetworkData Data, DateTime ReceivedUtc) received))
			{
				NetworkData data = received.Data;
				if (data.SyncResponse)
				{
					if (_pendingRequests.TryRemove(data.ConversationGuid, out PendingRequest pending))
					{
						pending.Completion.TrySetResult(data);
					}
					else
					{
						Debug.LogWarning($"Received a sync response for conversation '{data.ConversationGuid}' with no matching pending request. It likely already timed out; ignoring.");
					}
				}
				else if (data.SyncRequest)
				{
					ProcessSyncRequest(data).Forget();
				}
				else if (received.ReceivedUtc <= data.ExpirationUtc)
				{
					EventSystem.Dispatch(data);
				}
				else
				{
					DroppedCount++;
					Diagnostics.Log($"dropped {data.GetType().Name}, expired {(received.ReceivedUtc - data.ExpirationUtc).TotalMilliseconds:0}ms before receive");
				}

				ReceivedCount++;
			}

			// Fail pending request on the main thread so their
			// awaiting callers resume here rather than on a socket thread.
			if (!_pendingRequests.IsEmpty)
			{
				foreach (var entry in _pendingRequests)
				{
					if (_disconnectPending)
					{
						if (_pendingRequests.TryRemove(entry.Key, out PendingRequest pending))
						{
							pending.Completion.TrySetResult(null);
						}
					}
					else if (DateTime.UtcNow >= entry.Value.ExpiresUtc && _pendingRequests.TryRemove(entry.Key, out PendingRequest pending))
					{
						pending.Completion.TrySetException(new TimeoutException("A response to a synchronous request was not received within the timeout window."));
					}
				}
			}

			// Notify game code of a disconnect on the main thread.
			if (_disconnectPending && !_disconnectRaised)
			{
				_disconnectRaised = true;
				_onDisconnected();
			}
		}

		private async UniTaskVoid ProcessSyncRequest(NetworkData data)
		{
			NetworkData response = await EventSystem.InvokeSyncRequest(data);
			response.ConversationGuid = data.ConversationGuid;
			response.SyncResponse = true;
			SendInternal(response).Forget();
		}

		internal async UniTaskVoid SendInternal(NetworkData data)
		{
			if (!_isConnectionOpen) return;
			try
			{
				if (data.ExpirationUtc != DateTime.MaxValue)
				{
					data.ExpirationUtc = DateTime.UtcNow.AddMilliseconds(TIMEOUT_MS);
				}
				byte[] packedData = await ProtoSerialiser.Pack(data);
				if (packedData != null)
				{
					await _connectionStream.WriteAsync(packedData).ConfigureAwait(false);
				}
			}
			catch (Exception ex) when (ex is IOException or SocketException)
			{
				Debug.LogWarning("Socket terminated, disconnecting client.");
				DisconnectInternal();
			}
		}

		internal async UniTask SendAsyncInternal(NetworkData data)
		{
			if (!_isConnectionOpen)
			{
				await UniTask.SwitchToMainThread();
				return;
			}

			try
			{
				if (data.ExpirationUtc != DateTime.MaxValue)
				{
					data.ExpirationUtc = DateTime.UtcNow.AddMilliseconds(TIMEOUT_MS);
				}
				byte[] packedData = await ProtoSerialiser.Pack(data);
				if (packedData != null)
				{
					await _connectionStream.WriteAsync(packedData);
				}
			}
			catch (Exception ex) when (ex is IOException or SocketException)
			{
				Debug.LogWarning("Socket terminated, disconnecting client.");
				DisconnectInternal();
			}

			await UniTask.SwitchToMainThread();
		}

		internal async UniTask<NetworkData> SendReceiveAsyncInternal(NetworkData data, int timeout = TIMEOUT_MS)
		{
			if (!_isConnectionOpen) return null;

			data.SyncRequest = true;
			data.ExpirationUtc = DateTime.UtcNow.AddMilliseconds(timeout);

			PendingRequest pending = new()
			{
				Completion = new UniTaskCompletionSource<NetworkData>(),
				ExpiresUtc = DateTime.UtcNow.AddMilliseconds(timeout)
			};
			_pendingRequests[data.ConversationGuid] = pending;

			byte[] packedData = await ProtoSerialiser.Pack(data);
			if (packedData == null)
			{
				_pendingRequests.TryRemove(data.ConversationGuid, out _);
				return null;
			}

			try
			{
				await _connectionStream.WriteAsync(packedData);
			}
			catch (Exception ex) when (ex is IOException or SocketException)
			{
				Debug.LogWarning("Socket terminated, disconnecting client.");
				DisconnectInternal();
			}

			return await pending.Completion.Task;
		}

		internal void DisconnectInternal()
		{
			if (!_isConnectionOpen)
			{
				return;
			}

			_isConnectionOpen = false;
			_disconnectPending = true;
			try
			{
				_client?.Shutdown(SocketShutdown.Both);
			}
			catch (SocketException)
			{
				// Ignored.
			}
			finally
			{
				_connectionStream?.Close();
			}
			_cancellationToken.Cancel();
			_client = null;
			_connectionStream = null;
		}

		internal void DisconnectImmediateInternal()
		{
			_isConnectionOpen = false;
			_connectionStream?.Close();
			_cancellationToken.Cancel();
			Debug.Log("Used internal disconnect.");
		}
	}
}
