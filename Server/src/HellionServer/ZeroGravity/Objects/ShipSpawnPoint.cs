using System.Collections.Generic;
using System.Threading.Tasks;
using OpenHellion.Net;
using ZeroGravity.Data;
using ZeroGravity.Math;
using ZeroGravity.Network;

namespace ZeroGravity.Objects;

public class ShipSpawnPoint
{
	public int SpawnPointID;

	public Vector3D RelativePosition;

	public QuaternionD RelativeRotation;

	public SpawnPointType Type;

	public Player Player;

	public bool IsPlayerInSpawnPoint;

	public string InvitedPlayerId;

	public string InvitedPlayerName;

	public SceneTriggerExecutor Executor;

	public int ExecutorStateID;

	public Ship Ship;

	public SpawnPointState State;

	public List<int> ExecutorOccupiedStateIDs;

	public async Task<SpawnPointStats> SetStats(SpawnPointStats stats, Player sender)
	{
		if (Type == SpawnPointType.SimpleSpawn)
		{
			return null;
		}
		if (stats.PlayerInvite.HasValue)
		{
			if (State == SpawnPointState.Authorized || (State == SpawnPointState.Locked && sender != Player))
			{
				return null;
			}
			State = SpawnPointState.Locked;
			Player = sender;
			if (!await Server.Instance.PlayerInviteChanged(this, stats.InvitedPlayerId, stats.InvitedPlayerName, sender))
			{
				return new SpawnPointStats
				{
					InSceneID = SpawnPointID,
					NewState = State,
					PlayerGUID = Player is not null ? Player.FakeGuid : -1,
					PlayerName = Player is not null ? Player.Name : "",
					PlayerId = Player is not null ? Player.PlayerId : "",
					InvitedPlayerName = InvitedPlayerName,
					InvitedPlayerId = InvitedPlayerId
				};
			}
			return null;
		}
		if (!stats.NewState.HasValue)
		{
			return null;
		}
		if (stats.NewState == SpawnPointState.Unlocked && State == SpawnPointState.Locked && sender == Player)
		{
			State = SpawnPointState.Unlocked;
			Player = null;
			if (InvitedPlayerId.IsNullOrEmpty())
			{
				return new SpawnPointStats
				{
					InSceneID = SpawnPointID,
					NewState = State,
					PlayerGUID = -1L
				};
			}
			await Server.Instance.PlayerInviteChanged(this, "", "", sender);
		}
		else if (stats.HackUnlock.HasValue && stats.HackUnlock.Value && stats.NewState == SpawnPointState.Unlocked && State == SpawnPointState.Locked && sender != Player && sender.ItemInHands != null && ItemTypeRange.IsHackingTool(sender.ItemInHands.Type))
		{
			State = SpawnPointState.Unlocked;
			Player = null;
			await ((DisposableHackingTool)sender.ItemInHands).Use();
			if (InvitedPlayerId.IsNullOrEmpty())
			{
				return new SpawnPointStats
				{
					InSceneID = SpawnPointID,
					NewState = State,
					PlayerGUID = -1L
				};
			}
			await Server.Instance.PlayerInviteChanged(this, "", "", sender);
		}
		else
		{
			if (stats.NewState == SpawnPointState.Locked && Player == null && State == SpawnPointState.Unlocked)
			{
				State = SpawnPointState.Locked;
				Player = sender;
				return new SpawnPointStats
				{
					InSceneID = SpawnPointID,
					NewState = State,
					PlayerGUID = sender.FakeGuid,
					PlayerName = sender.Name,
					PlayerId = sender.PlayerId
				};
			}
			if (stats.NewState == SpawnPointState.Authorized)
			{
				if (sender.AuthorizedSpawnPoint is { State: SpawnPointState.Authorized })
				{
					sender.AuthorizedSpawnPoint.State = SpawnPointState.Locked;
					ShipStatsMessage retMsg = new ShipStatsMessage();
					retMsg.Guid = sender.AuthorizedSpawnPoint.Ship.Guid;
					retMsg.Temperature = sender.AuthorizedSpawnPoint.Ship.Temperature;
					retMsg.Health = sender.AuthorizedSpawnPoint.Ship.Health;
					retMsg.Armor = sender.AuthorizedSpawnPoint.Ship.Armor;
					retMsg.VesselObjects = new VesselObjects();
					retMsg.VesselObjects.SpawnPoints = new List<SpawnPointStats>();
					retMsg.VesselObjects.SpawnPoints.Add(new SpawnPointStats
					{
						InSceneID = sender.AuthorizedSpawnPoint.SpawnPointID,
						NewState = sender.AuthorizedSpawnPoint.State,
						PlayerGUID = sender.FakeGuid,
						PlayerName = sender.Name,
						PlayerId = sender.PlayerId
					});
					retMsg.SelfDestructTime = sender.AuthorizedSpawnPoint.Ship.SelfDestructTimer?.Time;
					await NetworkController.SendToClientsSubscribedTo(retMsg, -1L, sender.AuthorizedSpawnPoint.Ship);
				}
				State = SpawnPointState.Authorized;
				Player = sender;
				sender.SetSpawnPoint(this);
				return new SpawnPointStats
				{
					InSceneID = SpawnPointID,
					NewState = State,
					PlayerGUID = sender.FakeGuid,
					PlayerName = sender.Name,
					PlayerId = sender.PlayerId
				};
			}
		}
		return null;
	}

	public async Task SetInvitation(string id, string name, bool sendMessage)
	{
		if (!(id == InvitedPlayerId) && (!InvitedPlayerId.IsNullOrEmpty() || !id.IsNullOrEmpty()))
		{
			InvitedPlayerId = id;
			InvitedPlayerName = name;
			if (sendMessage)
			{
				ShipStatsMessage retMsg = new ShipStatsMessage();
				retMsg.Guid = Ship.Guid;
				retMsg.Temperature = Ship.Temperature;
				retMsg.Health = Ship.Health;
				retMsg.Armor = Ship.Armor;
				retMsg.VesselObjects = new VesselObjects();
				retMsg.VesselObjects.SpawnPoints = new List<SpawnPointStats>();
				retMsg.VesselObjects.SpawnPoints.Add(new SpawnPointStats
				{
					InSceneID = SpawnPointID,
					NewState = State,
					PlayerGUID = Player is not null ? Player.FakeGuid : -1,
					PlayerName = Player is not null ? Player.Name : "",
					PlayerId = Player is not null ? Player.PlayerId : "",
					PlayerInvite = true,
					InvitedPlayerName = InvitedPlayerName,
					InvitedPlayerId = InvitedPlayerId
				});
				retMsg.SelfDestructTime = Ship.SelfDestructTimer?.Time;
				await NetworkController.SendToClientsSubscribedTo(retMsg, -1L, Ship);
			}
		}
	}

	public async Task AuthorizePlayerToSpawnPoint(Player pl, bool sendMessage)
	{
		if (Type != 0 && (Player != pl || State != SpawnPointState.Authorized))
		{
			State = SpawnPointState.Authorized;
			Player = pl;
			pl.SetSpawnPoint(this);
			if (sendMessage)
			{
				ShipStatsMessage retMsg = new ShipStatsMessage();
				retMsg.Guid = Ship.Guid;
				retMsg.Temperature = Ship.Temperature;
				retMsg.Health = Ship.Health;
				retMsg.Armor = Ship.Armor;
				retMsg.VesselObjects = new VesselObjects();
				retMsg.VesselObjects.SpawnPoints = new List<SpawnPointStats>();
				retMsg.VesselObjects.SpawnPoints.Add(new SpawnPointStats
				{
					InSceneID = SpawnPointID,
					NewState = State,
					PlayerGUID = Player.FakeGuid,
					PlayerName = Player.Name,
					PlayerId = Player.PlayerId
				});
				retMsg.SelfDestructTime = Ship.SelfDestructTimer?.Time;
				await NetworkController.SendToClientsSubscribedTo(retMsg, -1L, Ship);
			}
		}
	}
}
