using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using OpenHellion.IO;
using OpenHellion.Net.Message;
using ZeroGravity;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace OpenHellion.Net;

public class StatusConnection
{
	private const int MAX_MESSAGE_SIZE = 100000;

	private readonly Socket _socket;

	private Thread _listeningThread;

	public StatusConnection(Socket soc)
	{
		_socket = soc;
	}

	public void Start()
	{
		_listeningThread = new Thread(Listen)
		{
			IsBackground = true
		};
		_listeningThread.Start();
	}

	public void Stop()
	{
		_socket.Close();
	}

	private async void Listen()
	{
		try
		{
			NetworkData data = await ProtoSerialiser.Unpack(new NetworkStream(_socket), MAX_MESSAGE_SIZE);
			string ipAddress = _socket.RemoteEndPoint.ToString().Split(":".ToCharArray(), 2)[0];

			switch (data)
			{
				case null:
					return;
				case ServerShutDownMessage msg:
				{
					if (Server.Instance.IsAddressAutorized(ipAddress))
					{
						Server.Restart = msg.Restrat;
						Server.CleanRestart = msg.CleanRestart;
						Server.SavePersistenceDataOnShutdown = (!Server.Restart && Server.PersistenceSaveInterval > 0.0) || (Server.Restart && !Server.CleanRestart);
						Server.IsRunning = false;
					}

					return;
				}
				case DeleteCharacterRequest dcr:
				{
					// TODO: Anyone who can reach this port could otherwise delete any character they can name.
					if (!Server.Instance.IsAddressAutorized(ipAddress))
					{
						Debug.LogInfo("Refused character deletion from unauthorized address.", ipAddress);
						return;
					}

					Server.Instance.DeleteCharacter(dcr.PlayerId);
					return;
				}
				case ServerStatusRequest ssr:
				{
					bool isPrivate = !Server.ServerPassword.IsNullOrEmpty();
					Player player = !isPrivate && ssr.PlayerId is not null
						? Server.Instance.GetPlayerFromPlayerId(ssr.PlayerId)
						: null;

					_socket.Send(await ProtoSerialiser.Pack(new ServerStatusResponse
					{
						Name = Server.ServerName,
						Description = Server.ServerDescription,
						CurrentPlayers = (short)Server.Instance.AllPlayers.Count(),
						AlivePlayers = (short)Server.Instance.AllPlayers.Count(static (Player m) => m.IsAlive),
						MaxPlayers = (short)NetworkController.MaxPlayers,
						Hash = Server.CombinedHash,
						IsOffline = Server.OfflineMode,
						IsPrivate = isPrivate,
						CharacterName = player?.Name
					}));
					return;
				}
				case JoinInfoRequest jir:
				{
					bool passwordAccepted = Server.ServerPassword.IsNullOrEmpty() || jir.Password == Server.ServerPassword;
					Player player = passwordAccepted && jir.PlayerId is not null
						? Server.Instance.GetPlayerFromPlayerId(jir.PlayerId)
						: null;

					_socket.Send(await ProtoSerialiser.Pack(new JoinInfoResponse
					{
						PasswordAccepted = passwordAccepted,
						CharacterData = player is null
							? null
							: new CharacterData
							{
								Name = player.Name,
								Gender = player.Gender,
								HeadType = player.HeadType,
								HairType = player.HairType
							},
						IsAlive = player is { IsAlive: true },
						CanContinue = player?.AuthorizedSpawnPoint is not null,
						SpawnPointsList = player is null ? null : Server.Instance.GetAvailableSpawnPoints(player)
					}));
					return;
				}
				case LatencyTestMessage:
					_socket.Send(await ProtoSerialiser.Pack(data));
					return;
			}
		}
		catch (SocketException)
		{
			Debug.LogError("Error when trying to listen to status connection, socket failed.");
		}
		catch (Exception ex)
		{
			Debug.LogException(ex);
		}
		finally
		{
			Stop();
		}
	}
}
