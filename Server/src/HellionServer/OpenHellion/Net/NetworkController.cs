using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using OpenHellion.IO;
using ZeroGravity;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace OpenHellion.Net;

public static class NetworkController
{
	private static readonly GameTransport _transport;

	private static StatusConnectionListener _statusPortConnectionListener;

	public static string ServerId { get; set; }

	public static int MaxPlayers = 100;

	public static int ReceivedCount => _transport.ReceivedCount;

	public static int SentCount => _transport.SentCount;

	public static int Connections => _transport.Connections;

	static NetworkController()
	{
		_transport = new GameTransport(OnClientConnected, OnDisconnected, maxConnections: () => MaxPlayers);
		EventSystem.AddListener<LogOutRequest>(LogOutRequestListener);
	}

	private static async Task<long> OnClientConnected(NetworkStream stream, long[] otherConnections, int maxMessageSize)
	{
		try
		{
			var loginData = await ProtoSerialiser.Unpack(stream, maxMessageSize) as LogInRequest;
			if (loginData is null)
			{
				Debug.LogError("Connected client did not send a login request on connect.");
				return -1;
			}

			async Task<long> Reject(string reason)
			{
				Debug.LogInfo("Rejected login.", reason);
				await stream.WriteAsync(await ProtoSerialiser.Pack(new LogInResponse
				{
					SyncResponse = true,
					ConversationGuid = loginData.ConversationGuid,
					Status = NetworkData.MessageStatus.Failure
				})).ConfigureAwait(false);
				return -1;
			}

			Debug.LogInfoFormat("Received login request for player {0} with id {1}.", loginData.CharacterData?.Name,
				loginData.PlayerId);

			if (loginData.ClientHash != Server.CombinedHash)
			{
				return await Reject($"client hash {loginData.ClientHash} does not match server hash {Server.CombinedHash}");
			}

			// Offline clients pick their own player id, so they must never reach a server that trusts main
			// server identities, and vice versa.
			if (loginData.IsOffline != Server.OfflineMode)
			{
				return await Reject(loginData.IsOffline
					? "client is in offline mode but this server uses the main server"
					: "client expects a main server but this server runs in offline mode");
			}

			if (!Server.ServerPassword.IsNullOrEmpty() && loginData.Password != Server.ServerPassword)
			{
				return await Reject("wrong server password");
			}

			if (!Guid.TryParse(loginData.PlayerId, out _))
			{
				return await Reject($"player id {loginData.PlayerId} is not a valid guid");
			}

			long guid = GUIDFactory.PlayerIdToGuid(loginData.PlayerId);
			if (otherConnections.Contains(guid))
			{
				return await Reject($"client with guid {guid} is already connected");
			}

			var player = await Server.Instance.GetOrCreateConnectedPlayerAsync(guid, loginData.PlayerId, loginData.CharacterData);
			if (player is null)
			{
				return await Reject($"could not create a player for id {loginData.PlayerId}");
			}

			if (!player.PlayerReady || !player.EnvironmentReady)
			{
				player.Initialize = true;
			}

			player.ConnectToNetworkController();

			var loginResponse = new LogInResponse
			{
				Status = NetworkData.MessageStatus.Success,
				SyncResponse = true,
				ConversationGuid = loginData.ConversationGuid,
				GUID = player.FakeGuid,
				Data = new CharacterData
				{
					Name = player.Name,
					Gender = player.Gender,
					HairType = player.HairType,
					HeadType = player.HeadType
				},
				ServerTime = Server.Instance.SolarSystem.CurrentTime,
				IsAlive = player.IsAlive,
				DebrisFields = Server.Instance.GetDebrisFieldsDetails(),
				ItemsIngredients = StaticData.ItemsIngredients,
				Quests = StaticData.QuestsData,
				ExposureRange = StaticData.SolarSystem.ExposureRange,
				VesselExposureValues = StaticData.SolarSystem.VesselExposureValues,
				PlayerExposureValues = StaticData.SolarSystem.PlayerExposureValues,
				VesselDecayRateMultiplier = Server.VesselDecayRateMultiplier
			};

			var packedData = await ProtoSerialiser.Pack(loginResponse);
			await stream.WriteAsync(packedData).ConfigureAwait(false);
			return guid;
		}
		catch (SocketException)
		{
			Debug.Log("Client disconnected when processing login.");
			return -1;
		}
	}

	public static void Start()
	{
		_statusPortConnectionListener = new StatusConnectionListener();
		_statusPortConnectionListener.Start(Server.StatusPort);
		_transport.Start(Server.GamePort);
	}

	private static async void LogOutRequestListener(NetworkData data)
	{
		await _transport.PrioritySendAsyncInternal(data.Sender, new LogOutResponse
		{
			Sender = 0L,
			Status = NetworkData.MessageStatus.Success,
		});
		DisconnectClient(data.Sender);
	}


	public static bool HasConnectedClients => _transport.Connections > 0;

	public static Task Tick()
	{
		return _transport.Tick();
	}

	public static Task Flush()
	{
		return _transport.Flush();
	}

	/// <summary>
	/// 	Get a list of all the players on the server.
	/// </summary>
	public static Player[] GetAllConnectedPlayers()
	{
		return (from guid in _transport.GetConnectionsGuidAsync() select Server.Instance.GetPlayer(guid)).ToArray();
	}

	/// <summary>
	/// 	Send data to a client with specified guid.
	/// </summary>
	/// <param name="guid">Guid of client.</param>
	/// <param name="data">Data to send.</param>
	public static Task SendAsync(long guid, NetworkData data)
	{
		return _transport.SendAsyncInternal(guid, data);
	}

	/// <summary>
	/// 	Send a message to all clients.<br />
	/// 	You can choose to skip one player.
	/// </summary>
	/// <param name="data">The data to send.</param>
	/// <param name="skipPlayerGuid">Guid of a player to skip.</param>
	public static Task SendToAllAsync(NetworkData data, long skipPlayerGuid = -1L)
	{
		return _transport.SendToAllAsyncInternal(data, skipPlayerGuid);
	}

	/// <summary>
	/// 	Sends a provided network message to clients that have the provided space object in view.
	/// </summary>
	/// <param name="spaceObjects">The clients that have this space object in view will receive the message.</param>
	// TODO sending multiple spaceObjects might cause issues, and is only really used by SendToClientsViewingParents.
	public static async Task SendToClientsViewing(NetworkData data, long skipPlayerGuid = -1L, params SpaceObject[] spaceObjects)
	{
		if (spaceObjects.Length == 0 || !HasConnectedClients)
		{
			return;
		}
		await Parallel.ForEachAsync(from m in GetAllConnectedPlayers() where m != null && m.Guid != skipPlayerGuid select m, async (player, ct) =>
		{
			HashSet<long> view = player.KnownView;
			if (player.IsAlive && player.EnvironmentReady && spaceObjects.Any(m => m != null && view.Contains(m.Guid)))
			{
				await SendAsync(player.Guid, data);
			}
		});
	}

	[Obsolete("Probably a symptom of bad design")]
	// TODO Will be made deprecated if space stations (now collections of ships), are added as its own space object type.
	public static async Task SendToClientsViewingParents(NetworkData data, SpaceObject spaceObject, long skipPlayerGuid = -1L, int depth = 4)
	{
		List<SpaceObject> parents = new List<SpaceObject>
		{
			spaceObject
		};

		SpaceObject parent = spaceObject.Parent;
		while (parent != null && depth > 0)
		{
			parents.Add(parent);
			parent = parent.Parent;
			depth--;
		}
		await SendToClientsViewing(data, skipPlayerGuid, parents.ToArray());
	}

	/// <summary>
	/// 	Simple check to see if a client is connected.
	/// </summary>
	public static bool IsPlayerConnected(long guid)
	{
		return _transport.IsClientConnected(guid);
	}

	/// <summary>
	/// 	Disconnect currently connected client.<br />
	/// 	Terminates their connection and logs them out of the game.
	/// </summary>
	public static void DisconnectClient(long guid)
	{
		_transport.DisconnectInternal(guid);
	}

	/// <summary>
	/// 	Disconnect all currently connected clients.<br />
	/// 	Terminates their connection and logs them out of the game.
	/// </summary>
	public static void DisconnectAllClients()
	{
		_transport.DisconnectAll();
	}

	private static async void OnDisconnected(long guid)
	{
		var player = Server.Instance.GetPlayer(guid);
		await player?.RemovePlayerFromTrigger();
		player?.LogoutDisconnectReset();
		player?.DisconnectFromNetworkController();

		Debug.LogInfo("Player disconnected:", player?.Name, guid);
	}

	public static void Stop()
	{
		DisconnectAllClients();
		_statusPortConnectionListener.Stop();
		_transport.StopInternal();
	}
}
