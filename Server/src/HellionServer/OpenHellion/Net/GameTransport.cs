// GameTransport.cs
//
// Copyright (C) 2026, OpenHellion contributors
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using OpenHellion.IO;
using ZeroGravity.Network;

namespace OpenHellion.Net;

/// <summary>
/// 	Lightweight single-connection game transport with framing.
/// </summary>
/// <remarks>
/// 	Needs TLS support. Depends upon <c>ProtoSerialiser</c> and <c>NetworkData</c>.
/// 	Insipred by WatsonTcp.
/// </remarks>
internal sealed class GameTransport
{
	private const int TIMEOUT_MS = 4000;

	private const int MAX_MESSAGE_SIZE = 16000000;

	// How long a closing socket keeps reading before it is forced shut.
	private const int DrainTimeoutMs = 500;

	private readonly Dictionary<long, ConnectionData> _connections = [];

	private readonly ConcurrentQueue<(long Guid, NetworkData Data)> _received = new();

	private readonly ConcurrentQueue<long> _disconnectQueue = new();

	private Socket _server;

	private readonly Func<NetworkStream, long[], int, Task<long>> _onConnected;

	private readonly Action<long> _onDisconnected;

	internal int ReceivedCount;

	internal int SentCount;

	private readonly Func<int> _maxConnections;

	private readonly CancellationTokenSource _mainCancellationToken = new CancellationTokenSource();

	internal int Connections
	{
		get
		{
			return _connections.Count;
		}
	}

	struct ConnectionData
	{
		internal Socket socket;
		internal NetworkStream stream;
		internal CancellationTokenSource cancellationToken;
		internal ConcurrentQueue<NetworkData> outbound;
	}

	internal GameTransport(Func<NetworkStream, long[], int, Task<long>> onConnected, Action<long> onDisconnected, Func<int> maxConnections)
	{
		_onConnected = onConnected;
		_onDisconnected = onDisconnected;
		_maxConnections = maxConnections;
	}

	internal void Start(int port)
	{
		_server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
		{
			Blocking = true,
		};
		_server.Bind(new IPEndPoint(IPAddress.Any, port));
		_server.Listen();

		Task.Run(() => AcceptConnections(_mainCancellationToken.Token));
	}

	internal async Task AcceptConnections(CancellationToken token)
	{
		Debug.Log("Started looking for connections.");
		while (true)
		{
			token.ThrowIfCancellationRequested();
			await Task.Delay(5, token);
			try
			{
				Socket handler = await _server.AcceptAsync(token);
				if (_connections.Count >= _maxConnections())
				{
					Debug.LogWarning("Maximum number of players exceeded.");
					return;
				}

				Debug.LogFormat("Received connection from {0}.", handler.RemoteEndPoint.ToString());

				var stream = new NetworkStream(handler, true);
				long guid = await _onConnected(stream, _connections.Keys.ToArray(), MAX_MESSAGE_SIZE);

				if (guid is -1)
				{
					Debug.LogWarning("Got connection with guid of -1.");
					try
					{
						handler.Shutdown(SocketShutdown.Both);
					}
					finally
					{
						stream.Close();
					}
					return;
				}

				if (_connections.ContainsKey(guid))
				{
					Debug.LogWarning("Got connection with guid already logged in.");
					try
					{
						handler.Shutdown(SocketShutdown.Both);
					}
					finally
					{
						stream.Close();
					}
					return;
				}

				var cancelToken = CancellationTokenSource.CreateLinkedTokenSource(token);
				var connection = new ConnectionData()
				{
					socket = handler,
					stream = stream,
					cancellationToken = cancelToken,
					outbound = new ConcurrentQueue<NetworkData>()
				};

				new Thread(() => ListenerThread(guid, connection))
				{
					IsBackground = true
				}.Start();

				_connections.Add(guid, connection);
				Debug.LogFormat("Storing new connection with id: {0}.", _connections.Count);
			}
			catch (TaskCanceledException)
			{
				break;
			}
		}

		Debug.Log("Stopped looking for connections.");
	}

	private async void ListenerThread(long guid, ConnectionData data)
	{
		Debug.Log("Started network listener for client", guid);

		while (true)
		{
			try
			{
				if (data.stream.DataAvailable)
				{
					NetworkData networkData = await ProtoSerialiser.Unpack(data.stream, MAX_MESSAGE_SIZE, guid, data.cancellationToken.Token);
					if (networkData != null)
					{
						networkData.Sender = guid;

						if (networkData.SyncRequest || DateTime.UtcNow <= networkData.ExpirationUtc)
						{
							_received.Enqueue((guid, networkData));
						}
						else
						{
							Debug.LogWarning($"Discarding expired messages from client {guid}; the loop is behind, or the clocks disagree");
						}
					}
				}
			}
			catch (ProtoSerialiser.ZeroDataException ex)
			{
				Debug.Log("Client closed the connection, disconnecting", guid, ex.Message);
				DisconnectInternal(guid);
				break;
			}
			catch (IOException ex)
			{
				Debug.Log("Socket terminated in the receive loop, disconnecting client", guid, ex.Message);
				DisconnectInternal(guid);
				break;
			}
			catch (ObjectDisposedException)
			{
				// Our own DisconnectInternal closed the stream; nothing to report.
				break;
			}
			catch (ArgumentException ex)
			{
				// Unpack throws this for a frame declaring more than MAX_MESSAGE_SIZE.
				Debug.LogException(ex);
			}
			catch (Exception ex)
			{
				// Connection reset (IOException/SocketException) or closed by the remote host.
				Debug.LogError("Unhandled fault in the receive loop, disconnecting client", guid, ex);
				DisconnectInternal(guid);
				break;
			}
		}
	}

	internal async Task Tick()
	{
		while (_received.TryDequeue(out var received))
		{
			Interlocked.Increment(ref ReceivedCount);
			try
			{
				if (received.Data.SyncRequest)
				{
					NetworkData res = await EventSystem.InvokeSyncRequest(received.Data);
					res.ConversationGuid = received.Data.ConversationGuid;
					res.SyncResponse = true;
					await SendAsyncInternal(received.Guid, res);
				}
				else
				{
					EventSystem.Invoke(received.Data);
				}
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}

		while (_disconnectQueue.TryDequeue(out long guid))
		{
			_onDisconnected(guid);
		}
	}

	internal Task SendAsyncInternal(long guid, NetworkData data)
	{
		if (_connections.TryGetValue(guid, out var connectionData))
		{
			if (data.ExpirationUtc != DateTime.MaxValue)
			{
				data.ExpirationUtc = DateTime.UtcNow.AddMilliseconds(TIMEOUT_MS);
			}
			connectionData.outbound.Enqueue(data);
		}
		return Task.CompletedTask;
	}

	internal Task SendToAllAsyncInternal(NetworkData data, long skipPlayerGuid = -1L)
	{
		if (data.ExpirationUtc != DateTime.MaxValue)
		{
			data.ExpirationUtc = DateTime.UtcNow.AddMilliseconds(TIMEOUT_MS);
		}
		foreach (var connection in _connections.ToArray())
		{
			if (connection.Key != skipPlayerGuid)
			{
				connection.Value.outbound.Enqueue(data);
			}
		}
		return Task.CompletedTask;
	}

	internal async Task PrioritySendAsyncInternal(long guid, NetworkData data)
	{
		if (!_connections.TryGetValue(guid, out var connectionData))
		{
			Debug.LogWarning("Priority send to disconnected client.");
			return;
		}

		connectionData.outbound.Clear();
		if (data.ExpirationUtc != DateTime.MaxValue)
		{
			data.ExpirationUtc = DateTime.UtcNow.AddMilliseconds(TIMEOUT_MS);
		}
		byte[] packedData = await ProtoSerialiser.Pack(data);
		if (packedData == null)
		{
			Debug.LogError($"Dropping a priority {data.GetType().Name} for client {guid} that could not be serialised.");
			return;
		}

		try
		{
			await connectionData.stream.WriteAsync(packedData, connectionData.cancellationToken.Token).ConfigureAwait(false);
			Interlocked.Increment(ref SentCount);
		}
		catch (Exception ex) when (ex is IOException or OperationCanceledException or ObjectDisposedException)
		{
			Debug.Log("Socket terminated during a priority send, disconnecting client", guid, ex.Message);
			DisconnectInternal(guid);
		}
	}

	internal async Task Flush()
	{
		await Parallel.ForEachAsync(_connections.ToArray(), async (connection, _) =>
		{
			if (connection.Value.outbound.IsEmpty)
			{
				return;
			}

			using MemoryStream batch = new();
			while (connection.Value.outbound.TryDequeue(out NetworkData data))
			{
				byte[] packedData = await ProtoSerialiser.Pack(data);
				if (packedData == null)
				{
					Debug.LogError($"Dropping a {data.GetType().Name} for client {connection.Key} that could not be serialised.");
					continue;
				}
				batch.Write(packedData);
				Interlocked.Increment(ref SentCount);
			}

			try
			{
				await connection.Value.stream.WriteAsync(batch.GetBuffer().AsMemory(0, (int)batch.Length), connection.Value.cancellationToken.Token).ConfigureAwait(false);
			}
			catch (Exception ex) when (ex is IOException or OperationCanceledException or ObjectDisposedException)
			{
				Debug.Log("Socket terminated during a send, disconnecting client", connection.Key, ex.Message);
				DisconnectInternal(connection.Key);
			}
		});
	}

	internal bool IsClientConnected(long guid)
	{
		return _connections.ContainsKey(guid);
	}

	internal long[] GetConnectionsGuidAsync()
	{
		return _connections.Keys.ToArray();
	}

	// Disconnect a client with the provided id.
	internal void DisconnectInternal(long guid)
	{
		if (!_connections.TryGetValue(guid, out ConnectionData connection)) return;

		_connections.Remove(guid);
		_disconnectQueue.Enqueue(guid);
		connection.cancellationToken.Cancel();

		Task.Run(() =>
		{
			try
			{
				connection.socket.Shutdown(SocketShutdown.Send);
				connection.socket.ReceiveTimeout = DrainTimeoutMs;

				byte[] drain = new byte[4096];
				long deadline = Environment.TickCount64 + DrainTimeoutMs;
				while (Environment.TickCount64 < deadline && connection.socket.Receive(drain) > 0)
				{
				}
			}
			catch (Exception)
			{
				// The peer is already gone, which is the state this was closing towards anyway.
			}
			finally
			{
				try { connection.stream.Close(); } catch (Exception) { }
				try { connection.socket.Close(); } catch (Exception) { }
			}
		});
	}

	/// <summary>
	/// 	Disconnects all clients.
	/// </summary>
	internal void DisconnectAll()
	{
		foreach (var guid in _connections.Keys.ToArray())
		{
			DisconnectInternal(guid);
		}
	}

	internal void StopInternal()
	{
		_mainCancellationToken.Cancel();
		DisconnectAll();
		_server.Close();
	}
}
