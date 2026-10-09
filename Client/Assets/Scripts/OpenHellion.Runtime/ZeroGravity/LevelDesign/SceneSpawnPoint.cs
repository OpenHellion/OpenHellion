using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nakama;
using OpenHellion;
using OpenHellion.Net;
using OpenHellion.Social;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;
using ZeroGravity.Data;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity.LevelDesign
{
	public class SceneSpawnPoint : MonoBehaviour, ISceneObject
	{
		public class PlayerInviteData
		{
			public string PlayerId;

			public string Name;

			public bool IsFriend;

			public bool AlreadyHasInvite;
		}

		public delegate void GetInvitedPlayersDelegate(List<PlayerInviteData> availablePlayers);

		[Tooltip("InSceneID will be assigned automatically")] [SerializeField]
		private int _inSceneID;

		public TagAction TagAction;

		public string Tags;

		public SpawnPointType SpawnType;

		[FormerlySerializedAs("Executer")] public SceneTriggerExecutor Executor;

		[FormerlySerializedAs("ExecuterState")] public string ExecutorState;

		[FormerlySerializedAs("ExecuterOccupiedStates")] public string ExecutorOccupiedStates;

		[HideInInspector] public SpaceObjectVessel ParentVessel;

		public GameObject StatusMesh;

		private Material DefaultMaterial;

		private GetInvitedPlayersDelegate onPlayersLodedDelegate;

		public int InSceneID
		{
			get => _inSceneID;
			set => _inSceneID = value;
		}

		public long PlayerGUID { get; private set; }

		public string PlayerName { get; private set; }

		public string PlayerId { get; private set; }

		public SpawnPointState State { get; private set; }

		public string InvitedPlayerName { get; private set; }

		public string InvitedPlayerId { get; private set; }

		private void Awake()
		{
			if (StatusMesh != null)
			{
				DecalProjector decal = StatusMesh.GetComponentInChildren<DecalProjector>();
				DefaultMaterial = decal.material;
				decal.material = Instantiate(DefaultMaterial);
			}
		}

		public void SetStats(SpawnPointStats details)
		{
			if (details.InSceneID == InSceneID)
			{
				if (details.NewType.HasValue)
				{
					SpawnType = details.NewType.Value;
				}

				if (details.NewState.HasValue)
				{
					State = details.NewState.Value;
				}

				if (details.PlayerGUID.HasValue)
				{
					PlayerGUID = details.PlayerGUID.Value;
					PlayerName = ((PlayerGUID <= 0) ? string.Empty : details.PlayerName);
					PlayerId = details.PlayerId;
				}

				InvitedPlayerName = details.InvitedPlayerName;
				if (InvitedPlayerName == null)
				{
					InvitedPlayerName = string.Empty;
				}

				InvitedPlayerId = details.InvitedPlayerId;
				UpdateState();
			}
		}

		public void UpdateState()
		{
			if (StatusMesh != null)
			{
				Material material = StatusMesh.GetComponentInChildren<DecalProjector>().material;
				if (State == SpawnPointState.Unlocked)
				{
					material.SetColor("_EmissionColor", Colors.Green);
				}
				else if (!InvitedPlayerName.IsNullOrEmpty())
				{
					material.SetColor("_EmissionColor", Colors.Blue);
				}
				else if (State == SpawnPointState.Locked)
				{
					material.SetColor("_EmissionColor", Colors.Red);
				}
				else if (State == SpawnPointState.Authorized)
				{
					material.SetColor("_EmissionColor", Colors.Yellow);
				}
			}

			if (MyPlayer.Instance.LockedToTrigger != null &&
			    MyPlayer.Instance.LockedToTrigger.TriggerType == SceneTriggerType.CryoPodPanel)
			{
				ParentVessel.World.InWorldPanels.Cryo.UpdateUI();
			}
		}

		public void ToggleLock(bool isLocked)
		{
			if (isLocked)
			{
				LockSpawnPoint();
			}
			else
			{
				UnlockSpawnPoint();
			}
		}

		public void LockSpawnPoint()
		{
			if (ParentVessel == null)
			{
				Debug.LogError("LockSpawnPoint, Spawn point vessel is NULL " + name + InSceneID);
			}
			else if (SpawnType != 0 && State != SpawnPointState.Locked && State != SpawnPointState.Authorized)
			{
				SpaceObjectVessel parentVessel = ParentVessel;
				SpawnPointStats spawnPoint = new SpawnPointStats
				{
					InSceneID = InSceneID,
					NewState = SpawnPointState.Locked,
					PlayerGUID = MyPlayer.Instance.Guid
				};
				parentVessel.ChangeStats(null, null, null, null, null, null, null, null, null, null, null, null,
					spawnPoint);
			}
		}

		public void UnlockSpawnPoint()
		{
			if (ParentVessel == null)
			{
				Debug.LogError("UnlockSpawnPoint, Spawn point vessel is NULL" + name + InSceneID);
			}
			else if (SpawnType != 0 && State != 0 && State != SpawnPointState.Authorized &&
			         PlayerGUID == MyPlayer.Instance.Guid)
			{
				SpaceObjectVessel parentVessel = ParentVessel;
				SpawnPointStats spawnPoint = new SpawnPointStats
				{
					InSceneID = InSceneID,
					NewState = SpawnPointState.Unlocked,
					PlayerGUID = MyPlayer.Instance.Guid
				};
				parentVessel.ChangeStats(null, null, null, null, null, null, null, null, null, null, null, null,
					spawnPoint);
			}
		}

		public void AuthorizeToSpawnPoint()
		{
			if (ParentVessel == null)
			{
				Debug.LogError("AuthorizeToSpawnPoint, Spawn point vessel is NULL" + name + InSceneID);
			}
			else if (SpawnType != 0 && State != SpawnPointState.Authorized)
			{
				SpaceObjectVessel parentVessel = ParentVessel;
				SpawnPointStats spawnPoint = new SpawnPointStats
				{
					InSceneID = InSceneID,
					NewState = SpawnPointState.Authorized,
					PlayerGUID = MyPlayer.Instance.Guid
				};
				parentVessel.ChangeStats(null, null, null, null, null, null, null, null, null, null, null, null,
					spawnPoint);
				MyPlayer.Instance.HomeStationGUID = ParentVessel.Guid;
			}
		}

		public void HackSpawnPoint()
		{
			if (ParentVessel == null)
			{
				Debug.LogError("HackSpawnPoint, Spawn point vessel is NULL" + name + InSceneID);
			}
			else if (SpawnType != 0 && State != 0 && State != SpawnPointState.Authorized &&
			         PlayerGUID != MyPlayer.Instance.Guid)
			{
				SpaceObjectVessel parentVessel = ParentVessel;
				SpawnPointStats spawnPoint = new SpawnPointStats
				{
					InSceneID = InSceneID,
					HackUnlock = true,
					NewState = SpawnPointState.Unlocked,
					PlayerGUID = MyPlayer.Instance.Guid
				};
				parentVessel.ChangeStats(null, null, null, null, null, null, null, null, null, null, null, null,
					spawnPoint);
			}
		}

		public void InvitePlayer(PlayerInviteData player)
		{
			if (ParentVessel == null)
			{
				Debug.LogError("InvitePlayer, Spawn point vessel is NULL" + name + InSceneID);
			}
			else
			{
				if (SpawnType is SpawnPointType.SimpleSpawn || State is SpawnPointState.Authorized ||
				    (State is SpawnPointState.Locked && PlayerGUID != MyPlayer.Instance.Guid))
				{
					return;
				}

				SpawnPointStats spawnPoint;
				if (player is null)
				{
					spawnPoint = new SpawnPointStats
					{
						InSceneID = InSceneID,
						PlayerInvite = false,
						InvitedPlayerId = string.Empty,
						InvitedPlayerName = string.Empty
					};
					ParentVessel.ChangeStats(null, null, null, null, null, null, null, null, null, null, null, null,
						spawnPoint);
					return;
				}

				spawnPoint = new SpawnPointStats
				{
					InSceneID = InSceneID,
					PlayerInvite = true,
					InvitedPlayerId = player.PlayerId,
					InvitedPlayerName = player.Name
				};
				ParentVessel.ChangeStats(null, null, null, null, null, null, null, null, null, null, null, null,
					spawnPoint);
			}
		}

		public async UniTaskVoid GetPlayersForInvite(bool getNakamaFriends, bool getPlayerFromServer,
			GetInvitedPlayersDelegate onPlayersLoaded)
		{
			if (SpawnType == SpawnPointType.SimpleSpawn || State == SpawnPointState.Authorized ||
			    (State == SpawnPointState.Locked && PlayerGUID != MyPlayer.Instance.Guid))
			{
				return;
			}

			if (getNakamaFriends)
			{
				List<PlayerInviteData> list = new List<PlayerInviteData>();

				// Loop through each friend and add them to the list.
				IApiFriend[] nakamaFriends = Profile.OfflineMode ? null : await NakamaClient.GetFriends();
				foreach (IApiFriend friend in nakamaFriends ?? Array.Empty<IApiFriend>())
				{
					// If friend is online.
					if (friend.User.Online)
					{
						list.Add(new PlayerInviteData
						{
							PlayerId = friend.User.Id,
							IsFriend = true,
							Name = friend.User.DisplayName,
							AlreadyHasInvite = false
						});
					}
				}

				onPlayersLoaded(list);
			}

			if (getPlayerFromServer)
			{
				NetworkController.SendAndForget(new PlayersOnServerRequest
				{
					SpawnPointID = new VesselObjectID
					{
						VesselGUID = ParentVessel.Guid,
						InSceneID = InSceneID
					}
				});
				onPlayersLodedDelegate = onPlayersLoaded;
			}
		}

		public async UniTaskVoid ParsePlayersOnServerResponse(PlayersOnServerResponse data)
		{
			if (onPlayersLodedDelegate == null)
			{
				return;
			}

			List<PlayerInviteData> list = new List<PlayerInviteData>();
			if (data.PlayersOnServer != null && data.PlayersOnServer.Count > 0)
			{
				foreach (PlayerOnServerData item in data.PlayersOnServer)
				{
					if (item.PlayerId != Profile.PlayerId)
					{
						list.Add(new PlayerInviteData
						{
							IsFriend = false,
							PlayerId = item.PlayerId,
							Name = item.Name,
							AlreadyHasInvite = item.AlreadyHasInvite
						});
					}
				}
			}

			if (list.Count > 0)
			{
				onPlayersLodedDelegate(list);
			}
		}
	}
}
