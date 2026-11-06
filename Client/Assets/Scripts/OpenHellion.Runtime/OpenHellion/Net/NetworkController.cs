// NetworkController.cs
//
// Copyright (C) 2024, OpenHellion contributors
//
// SPDX-License-Identifier: GPL-3.0-or-later
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
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Steamworks;
using System;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using OpenHellion.IO;
using OpenHellion.Social.RichPresence;
using ZeroGravity.Network;
using Cysharp.Threading.Tasks;

#if UNITY_EDITOR
using System.Linq;
#endif

namespace OpenHellion.Net
{
	public class NetworkController : MonoBehaviour
	{
		private static GameTransport _gameTransport;

		private bool _getP2PPacketsThreadActive;

		private static NetworkController _instance;

		public static int MessagesReceived => _gameTransport?.ReceivedCount ?? 0;

		public static int MessagesDropped => _gameTransport?.DroppedCount ?? 0;

		public static NetworkController Instance
		{
			get
			{
				if (_instance == null)
				{
					Debug.LogError("Tried to get network controller before it has been initialised.");
				}

				return _instance;
			}
		}

		private void Awake()
		{
			// Only one instance allowed.
			if (_instance != null)
			{
				Destroy(this);
				return;
			}

			_instance = this;
		}

		private void Update()
		{
			_gameTransport?.Tick();

			// Handle Steam P2P packets.
			if (RichPresenceManager.HasSteam && !_getP2PPacketsThreadActive)
			{
				//UniTask.Void(P2PPacketListener);
			}
		}

		public async static UniTask ConnectToGame(string ipAddress, int port, Action onDisconnected)
		{
			_gameTransport?.DisconnectImmediateInternal();
			_gameTransport = new GameTransport(() =>
			{
				_gameTransport = null;
				onDisconnected();
			});

			await _gameTransport.Connect(ipAddress, port);
		}


		/// <summary>
		/// 	Send network data to the server.
		/// 	This method does not wait for the server to receive the data.
		/// 	Use this for non-critical data that does not require a response.
		/// </summary>
		/// <param name="data">The data to send.</param>
		public static void SendAndForget(NetworkData data)
		{
			if (_gameTransport == null) return;
			_gameTransport.SendInternal(data).Forget();
		}

		/// <summary>
		/// 	Send network data to the server and wait for it to complete.
		/// 	Useful when you need to ensure the data is sent before continuing.
		/// </summary>
		/// <param name="data">The data to send.</param>
		public static async UniTask SendAsync(NetworkData data)
		{
			if (_gameTransport == null) return;
			await _gameTransport.SendAsyncInternal(data);
		}

		/// <summary>
		/// 	Use request/response-like communication with async support.
		/// 	A <a cref="TimeoutException"/> is thrown when no response is received within the configured timeframe.
		/// </summary>
		/// <param name="data">The data to send.</param>
		/// <exception cref="TimeoutException"/>
		public static UniTask<NetworkData> SendReceiveAsync(NetworkData data)
		{
			return _gameTransport.SendReceiveAsyncInternal(data);
		}

		/// <summary>
		/// 	Use request/response-like communication with async support.
		/// 	A <a cref="TimeoutException"/> is thrown when no response is received within the configured timeframe.
		/// </summary>
		/// <param name="data">The data to send.</param>
		/// <param name="timeout">Milliseconds to wait before timing out.</param>
		/// <exception cref="TimeoutException"/>
		public static UniTask<NetworkData> SendReceiveAsync(NetworkData data, int timeout)
		{
			return _gameTransport.SendReceiveAsyncInternal(data, timeout);
		}

		/// <summary>
		/// 	Round trip time to a server's status port, or -1 when it could not be reached.
		/// </summary>
		public static async UniTask<int> LatencyTest(string address, int port)
		{
			long start = System.Diagnostics.Stopwatch.GetTimestamp();
			if (await SendTcp(new LatencyTestMessage(), address, port) is null)
			{
				return -1;
			}

			return (int)((System.Diagnostics.Stopwatch.GetTimestamp() - start) * 1000L / System.Diagnostics.Stopwatch.Frequency);
		}

		/// <summary>
		/// 	Send a request directly to a TCP endpoint and optionally wait for the reply.
		/// 	Used for the status port, which is reachable without being connected to the game.
		/// </summary>
		public static async UniTask<NetworkData> SendTcp(NetworkData data, string address, int port,
			bool getResponse = true, bool logException = false, int timeoutMs = 4000)
		{
			using CancellationTokenSource timeout = new CancellationTokenSource(timeoutMs);

			try
			{
				using TcpClient tcpClient = new TcpClient();
				await tcpClient.ConnectAsync(address, port).AsUniTask().AttachExternalCancellation(timeout.Token);

				NetworkStream networkStream = tcpClient.GetStream();
				byte[] rawData = await ProtoSerialiser.Pack(data, timeout.Token);

				await networkStream.WriteAsync(rawData, 0, rawData.Length, timeout.Token);
				await networkStream.FlushAsync(timeout.Token);

				if (getResponse)
				{
					return await ProtoSerialiser.Unpack(networkStream, 10000, timeout.Token);
				}
			}
			catch (Exception ex)
			{
				if (logException)
				{
					Debug.LogException(ex);
				}
			}

			return null;
		}

		private void OnDestroy()
		{
			_gameTransport?.DisconnectInternal();
		}

		/// <summary>
		/// 	Disconnect after sending all queued data.
		/// </summary>
		public static void Disconnect()
		{
			_gameTransport?.DisconnectInternal();
		}

		/// <summary>
		/// 	Read and invoke P2P packets sent though Steam.<br/>
		/// </summary>
		private async UniTaskVoid P2PPacketListener()
		{
			_getP2PPacketsThreadActive = true;

			// Create pointer array and put data in it.
			IntPtr[] ptr = new IntPtr[1];
			int msgSize = SteamNetworkingMessages.ReceiveMessagesOnChannel(0, ptr, 1);
			if (msgSize == 0)
			{
				return;
			}

			try
			{
				SteamNetworkingMessage_t netMessage = Marshal.PtrToStructure<SteamNetworkingMessage_t>(ptr[0]);
				if (netMessage.m_cbSize != 0)
				{
					// Copy payload of the message and put it in a byte array.
					byte[] message = new byte[netMessage.m_cbSize];
					Marshal.Copy(netMessage.m_pData, message, 0, message.Length);

					// Deseralise data and invoke code.
					NetworkData networkData = await ProtoSerialiser.Unpack(new MemoryStream(message), 1000000);
					Debug.Log(networkData);
					if (networkData is ISteamP2PMessage)
					{
						EventSystem.Dispatch(networkData);
					}
				}
			}
			finally
			{
				Marshal.DestroyStructure<SteamNetworkingMessage_t>(ptr[0]);
			}

			_getP2PPacketsThreadActive = false;
		}
	}
}
