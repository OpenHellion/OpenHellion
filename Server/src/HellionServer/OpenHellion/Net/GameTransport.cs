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
		internal Action<NetworkData> syncResponseReceivedEvent;
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
					cancellationToken = cancelToken
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

						if (networkData.SyncResponse)
						{
							data.syncResponseReceivedEvent(networkData);
						}
						else if (networkData.SyncRequest || DateTime.UtcNow <= networkData.ExpirationUtc)
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

	/// <summary>
	/// 	Send network data to a client.
	/// </summary>
	/// <param name="guid">Guid of client to send to.</param>
	/// <param name="data">The data to send.</param>
	internal async Task SendAsyncInternal(long guid, NetworkData data)
	{
		try
		{
			if (_connections.TryGetValue(guid, out var connectionData))
			{
				if (data.ExpirationUtc != DateTime.MaxValue)
				{
					data.ExpirationUtc = DateTime.UtcNow.AddMilliseconds(TIMEOUT_MS);
				}
				var packedData = await ProtoSerialiser.Pack(data);
				if (packedData == null)
				{
					Debug.LogError($"Dropping a {data.GetType().Name} for client {guid} that could not be serialised.");
					return;
				}

				await connectionData.stream.WriteAsync(packedData, connectionData.cancellationToken.Token).ConfigureAwait(false);
			}
		}
		catch (IOException ex)
		{
			Debug.Log("Socket terminated during a send, disconnecting client", guid, ex.Message);
			DisconnectInternal(guid);
		}
	}

	/// <summary>
	/// 	Use request/response-like communication with async support.
	/// </summary>
	/// <param name="guid">Guid of client to send to.</param>
	/// <param name="data">The data to send.</param>
	internal async Task<NetworkData> SendReceiveAsyncInternal(long guid, NetworkData data)
	{
		try
		{
			if (_connections.TryGetValue(guid, out var connectionData))
			{
				data.ExpirationUtc = DateTime.UtcNow.AddMilliseconds(TIMEOUT_MS);
				data.SyncRequest = true;
				var packedData = await ProtoSerialiser.Pack(data);
				if (packedData == null)
				{
					Debug.LogError($"Dropping a {data.GetType().Name} for client {guid} that could not be serialised.");
					return null;
				}

				NetworkData response = null;
				CancellationTokenSource responseCancel = new();
				void responseHandler(NetworkData responseData)
				{
					if (data.ConversationGuid == responseData.ConversationGuid)
					{
						response = responseData;
						responseCancel.Cancel();
					}
				}
				connectionData.syncResponseReceivedEvent += responseHandler;

				await connectionData.stream.WriteAsync(packedData, connectionData.cancellationToken.Token).ConfigureAwait(false);

				await Task.Delay(TIMEOUT_MS, responseCancel.Token);
				connectionData.syncResponseReceivedEvent -= responseHandler;

				if (response != null)
				{
					return response;
				}
				else
				{
					throw new TimeoutException("A response to a synchronous request was not received within the timeout window.");
				}
			}
		}
		catch (IOException ex)
		{
			Debug.Log("Socket terminated during a synchronous send, disconnecting client", guid, ex.Message);
			DisconnectInternal(guid);
		}

		return null;
	}

	// Request to send to all clients.
	internal async Task SendToAllAsyncInternal(NetworkData data, long skipPlayerGuid = -1L)
	{
		if (_connections.Count == 0) return;
		if (data.ExpirationUtc != DateTime.MaxValue)
		{
			data.ExpirationUtc = DateTime.UtcNow.AddMilliseconds(TIMEOUT_MS);
		}
		var packedData = await ProtoSerialiser.Pack(data);
		if (packedData == null)
		{
			Debug.LogError($"Dropping a broadcast {data.GetType().Name} that could not be serialised.");
			return;
		}

		await Parallel.ForEachAsync(_connections, async (connection, _) =>
		{
			try
			{
				if (connection.Key == skipPlayerGuid) return;

				await connection.Value.stream.WriteAsync(packedData, _mainCancellationToken.Token).ConfigureAwait(false);
			}
			catch (IOException ex)
			{
				Debug.Log("Socket terminated during a broadcast, disconnecting client", connection.Key, ex.Message);
				DisconnectInternal(connection.Key);
			}
		});
	}

	internal async Task PrioritySendAsyncInternal(long guid, NetworkData data)
	{
		try
		{
			if (_connections.TryGetValue(guid, out var handler))
			{

				if (data.ExpirationUtc != DateTime.MaxValue)
				{
					data.ExpirationUtc = DateTime.UtcNow.AddMilliseconds(TIMEOUT_MS);
				}
				var packedData = await ProtoSerialiser.Pack(data);
				if (packedData == null)
				{
					Debug.LogError($"Dropping a priority {data.GetType().Name} for client {guid} that could not be serialised.");
					return;
				}

				await handler.stream.FlushAsync().ConfigureAwait(false);
				await handler.stream.WriteAsync(packedData).ConfigureAwait(false);
			}
			else
			{
				Debug.LogWarning("Priority send to disconnected client.");
			}
		}
		catch (IOException ex)
		{
			Debug.Log("Socket terminated during a priority send, disconnecting client", guid, ex.Message);
			DisconnectInternal(guid);
		}
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
