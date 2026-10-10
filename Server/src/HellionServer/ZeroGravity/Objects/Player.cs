using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using OpenHellion;
using OpenHellion.Net;
using OpenHellion.Net.Message;
using ZeroGravity.Data;
using ZeroGravity.Math;
using ZeroGravity.Network;
using ZeroGravity.ShipComponents;

namespace ZeroGravity.Objects;

public class Player : SpaceObjectTransferable, IPersistantObject, IAirConsumer
{
	public enum HitBoxType
	{
		None = -1,
		Head,
		Torso,
		Arms,
		Legs,
		Abdomen
	}

	public float MaxHealth = 100f;

	private double _lastMeleeTime;

	private float _acummulatedDamage;

	private readonly Timer _healTimer;

	private float _amountToHeal;

	private float _amountToHealStep;

	public double LastMovementMessageSolarSystemTime = -1.0;

	public bool IsAlive;

	private bool _environmentReady;

	private bool _playerReady;

	public string Name;

	public string PlayerId;

	public Gender Gender;

	public byte HeadType;

	public byte HairType;

	public float MouseLook;

	public float FreeLookX;

	public float FreeLookY;

	public long FakeGuid;

	public CharacterAnimationData AnimationData;

	public sbyte[] JetpackDirection;

	public Dictionary<byte, RagdollItemData> RagdollData;

	public int AnimationStatsMask;

	private readonly HashSet<long> _subscribedToSpaceObjects = [];

	public HashSet<long> KnownView = [];

	/// <summary>
	/// 	The player's own motion, measured against the parent and in the parent's axes: the rate
	/// 	<see cref="SpaceObjectTransferable.LocalPosition" /> changes at.
	/// </summary>
	public Vector3D LocalVelocity = Vector3D.Zero;

	public override Vector3D Velocity => (Parent?.Velocity ?? Vector3D.Zero)
		+ (Parent?.Rotation ?? QuaternionD.Identity) * LocalVelocity;

	public long AnchorGuid { get; set; }

	public const double AnchorRebaseDistance = 200.0;

	public const double AnchorKeepDistance = AnchorRebaseDistance * 1.5;

	private double _lastMoveRequestTime = -1.0;

	private double _lastAnimationMessageTime = -1.0;

	// Movement requests and parent changes share one queue because only their relative order tells us
	// which parent each request was measured from.
	private readonly ConcurrentQueue<NetworkData> _pendingMoveRequests = new();

	public Vector3D? LastReportedPosition;

	public Vector3D? LastReportedVelocity;

	private const double MaxResimDeltaTime = 0.5;

	private const double ResimPositionSlack = 2.0;

	private const double AcceptedJumpWarningDistance = 5.0;

	private const double TransformCorrectionEpsilon = 0.01;

	public bool IsAdmin = false;

	private SpaceObject _parent;

	public Room CurrentRoom;

	private bool isOutsideRoom;

	public Inventory PlayerInventory;

	public float CoreTemperature = 37f;

	private const double DeathDisconnectGraceSeconds = 60.0;

	private double _deathDisconnectWait;

	public bool IsInsideSpawnPoint;

	public ConcurrentQueue<ShipStatsMessage> MessagesReceivedWhileLoading = new ConcurrentQueue<ShipStatsMessage>();

	private PlayerStatsMessage lastPlayerStatsMessage;

	public VesselObjectID LockedToTriggerID;

	public List<Quest> Quests;

	public List<ItemCompoundType> Blueprints = ObjectCopier.DeepCopy(StaticData.DefaultBlueprints);

	// Vessels this player has discovered by scanning. Combined with always-visible/distress vessels
	// to form the set the player is allowed to see on the navigation map.
	// TODO persist.
	public readonly HashSet<long> DiscoveredVessels = [];

	public bool Initialize = true;

	public override SpaceObjectType ObjectType => SpaceObjectType.Player;

	public bool EnvironmentReady
	{
		get => _environmentReady;
		private set
		{
			if (_environmentReady != value)
			{
				_environmentReady = value;
				if (PlayerReady && _environmentReady)
				{
					Initialize = false;
				}
			}
		}
	}

	public bool PlayerReady
	{
		get
		{
			return _playerReady;
		}
		private set
		{
			if (_playerReady = value)
			{
				_playerReady = value;
				if (PlayerReady && EnvironmentReady)
				{
					Initialize = false;
				}
			}
		}
	}

	public ShipSpawnPoint CurrentSpawnPoint { get; private set; }

	public ShipSpawnPoint AuthorizedSpawnPoint { get; private set; }

	public Helmet CurrentHelmet => PlayerInventory.Equipped<Helmet>();

	public Jetpack CurrentJetpack => PlayerInventory.Equipped<Jetpack>();

	public Item ItemInHands => PlayerInventory.HandsSlot.Item;

	public float Health { get; private set; }

	public override SpaceObject Parent
	{
		get
		{
			return _parent;
		}
		set
		{
			if (_parent is SpaceObjectVessel vessel)
			{
				vessel.RemovePlayerFromCrew(this);
			}
			_parent = value;
			if (_parent is SpaceObjectVessel objectVessel)
			{
				objectVessel.AddPlayerToCrew(this);
			}
		}
	}

	public bool GodMode { get; set; }

	public bool IsPilotingVessel { get; private set; }

	public float? AmbientTemperature
	{
		get
		{
			if (PlayerInventory.CurrOutfit != null)
			{
				return PlayerInventory.CurrOutfit.InternalTemperature;
			}
			if (Parent is SpaceObjectVessel)
			{
				return (Parent as SpaceObjectVessel).Temperature;
			}
			return null;
		}
	}

	public float AirQualityDegradationRate
	{
		get
		{
			if (IsInsideSpawnPoint || !IsAlive || (CurrentHelmet != null && (!CurrentHelmet.IsVisorToggleable || CurrentHelmet.IsVisorActive)))
			{
				return 0f;
			}
			return 0.05f;
		}
	}

	public float AirQuantityDecreaseRate => 0f;

	public bool AffectsQuality => AirQualityDegradationRate > 0f;

	public bool AffectsQuantity => false;

	public Player(long guid, Vector3D localPosition, QuaternionD localRotation)
		: base(guid, localPosition, localRotation)
	{
		Health = MaxHealth;

		_healTimer = new Timer(100.0)
		{
			Enabled = false
		};
		_healTimer.Elapsed += async delegate
		{
			await HealOverTimeStep();
		};
	}

	public static async Task<Player> CreatePlayerAsync(long guid, Vector3D localPosition, QuaternionD localRotation, string name, string playerId, Gender gender, byte headType, byte hairType, bool addToServerList = true, Player clone = null)
	{
		var player = new Player(guid, localPosition, localRotation)
		{
			FakeGuid = GUIDFactory.NextPlayerFakeGUID(),
			Name = name,
			PlayerId = playerId,
			Gender = gender,
			HeadType = headType,
			HairType = hairType,
		};
		player.PlayerInventory = new Inventory(player);
		player.Quests = await Quest.CreateQuestsAsync(StaticData.QuestsData, player);
		if (addToServerList)
		{
			Server.Instance.Add(player);
		}
		if (clone == null)
		{
			return player;
		}
		if (clone.PlayerInventory.OutfitSlot.Item != null)
		{
			player.PlayerInventory.AddItemToInventory(await clone.PlayerInventory.OutfitSlot.Item.GetCopy(), clone.PlayerInventory.OutfitSlot.SlotID);
			foreach (InventorySlot sl in clone.PlayerInventory.CurrOutfit.InventorySlots.Values.Where((InventorySlot m) => m.Item != null))
			{
				player.PlayerInventory.AddItemToInventory(await clone.PlayerInventory.CurrOutfit.InventorySlots[sl.SlotID].Item.GetCopy(), sl.SlotID);
			}
		}
		if (clone.PlayerInventory.HandsSlot.Item != null)
		{
			player.PlayerInventory.AddItemToInventory(await clone.PlayerInventory.HandsSlot.Item.GetCopy(), clone.PlayerInventory.HandsSlot.SlotID);
		}

		return player;
	}

	public void ConnectToNetworkController()
	{
		EnvironmentReady = false;
		PlayerReady = false;
		EventSystem.AddListener<MoveObjectRequest>(MoveObjectRequestListener);
		EventSystem.AddListener<ChangeParentMessage>(ChangeParentMessageListener);
		EventSystem.AddListener<CharacterAnimationMessage>(CharacterAnimationMessageListener);
		EventSystem.AddListener<EnvironmentReadyMessage>(EnvironmentReadyListener);
		EventSystem.AddListener<PlayerShootingMessage>(PlayerShootingListener);
		EventSystem.AddListener<PlayerStatsMessage>(PlayerStatsMessageListener);
		EventSystem.AddListener<PlayerDrillingMessage>(PlayerDrillingListener);
		EventSystem.AddListener<PlayerRoomMessage>(PlayerRoomMessageListener);
		EventSystem.AddListener<SuicideRequest>(SuicideListener);
		EventSystem.AddListener<AuthorizedVesselsRequest>(AuthorizedVesselsRequestListener);
		EventSystem.AddListener<LockToTriggerMessage>(LockToTriggerMessageListener);
		EventSystem.AddListener<QuestTriggerMessage>(QuestTriggerMessageListener);
		EventSystem.AddListener<SkipQuestMessage>(SkipQuestMessageListener);
		EventSystem.AddListener<ScanForObjectsRequest>(ScanForObjectsRequestListener);
	}

	public void DisconnectFromNetworkController()
	{
		EnvironmentReady = false;
		PlayerReady = false;
		EventSystem.RemoveListener<MoveObjectRequest>(MoveObjectRequestListener);
		EventSystem.RemoveListener<ChangeParentMessage>(ChangeParentMessageListener);
		EventSystem.RemoveListener<CharacterAnimationMessage>(CharacterAnimationMessageListener);
		EventSystem.RemoveListener<EnvironmentReadyMessage>(EnvironmentReadyListener);
		EventSystem.RemoveListener<PlayerShootingMessage>(PlayerShootingListener);
		EventSystem.RemoveListener<PlayerStatsMessage>(PlayerStatsMessageListener);
		EventSystem.RemoveListener<PlayerDrillingMessage>(PlayerDrillingListener);
		EventSystem.RemoveListener<PlayerRoomMessage>(PlayerRoomMessageListener);
		EventSystem.RemoveListener<SuicideRequest>(SuicideListener);
		EventSystem.RemoveListener<AuthorizedVesselsRequest>(AuthorizedVesselsRequestListener);
		EventSystem.RemoveListener<LockToTriggerMessage>(LockToTriggerMessageListener);
		EventSystem.RemoveListener<QuestTriggerMessage>(QuestTriggerMessageListener);
		EventSystem.RemoveListener<SkipQuestMessage>(SkipQuestMessageListener);
		EventSystem.RemoveListener<ScanForObjectsRequest>(ScanForObjectsRequestListener);
	}

	public async Task RemovePlayerFromTrigger()
	{
		if (lastPlayerStatsMessage != null)
		{
			lastPlayerStatsMessage.LockedToTriggerID = null;
			await NetworkController.SendToClientsSubscribedTo(lastPlayerStatsMessage, Guid, Parent);
		}
	}

	private async void PlayerStatsMessageListener(NetworkData data)
	{
		var message = data as PlayerStatsMessage;
		if (FakeGuid == message.GUID)
		{
			lastPlayerStatsMessage = message;
			if (message.AnimationMaskChanged.HasValue && message.AnimationMaskChanged.Value)
			{
				AnimationStatsMask = message.AnimationStatesMask;
			}
			else
			{
				message.AnimationStatesMask = AnimationStatsMask;
			}
			LockedToTriggerID = message.LockedToTriggerID;
			IsPilotingVessel = message.IsPilotingVessel;
			await NetworkController.SendToClientsSubscribedTo(message, message.Sender, Parent);
		}
	}

	private static async Task PassTroughtShootMessage(PlayerShootingMessage psm)
	{
		PlayerShootingMessage sending = new PlayerShootingMessage
		{
			HitIndentifier = -1,
			ShotData = psm.ShotData,
			HitGUID = psm.HitGUID,
			GUID = psm.GUID
		};
		await NetworkController.SendToAllAsync(sending, psm.Sender);
	}

	protected async void PlayerShootingListener(NetworkData data)
	{
		var message = data as PlayerShootingMessage;
		if (message.Sender != Guid)
		{
			return;
		}
		Weapon wep = PlayerInventory.GetHandsItemIfType<Weapon>() as Weapon;
		if (wep == null && !message.ShotData.IsMeleeAttack)
		{
			return;
		}
		bool rateValid = false;
		if (message.ShotData.IsMeleeAttack)
		{
			if (Server.Instance.SolarSystem.CurrentTime - _lastMeleeTime > 1.0)
			{
				rateValid = true;
				_lastMeleeTime = Server.Instance.SolarSystem.CurrentTime;
			}
		}
		else if (wep != null && await wep.CanShoot())
		{
			rateValid = true;
		}
		if (!rateValid)
		{
			return;
		}
		if (message.HitGUID == -1)
		{
			message.HitGUID = -2L;
			await PassTroughtShootMessage(message);
			return;
		}
		SpaceObject sp = Server.Instance.GetSpaceObject(message.HitGUID);
		float damage = wep == null ? message.ShotData.IsMeleeAttack ? 30f : 0f : message.ShotData.IsMeleeAttack ? wep.MeleeDamage : wep.Damage;
		if (sp is DynamicObject dynamicObject)
		{
			await dynamicObject.Item.TakeDamage(new Dictionary<TypeOfDamage, float> {
			{
				TypeOfDamage.Hit,
				damage
			} });
		}
		if (Server.Instance.GetSpaceObject(message.HitGUID) is Player hitPlayer)
		{
			await NetworkController.SendToClientsSubscribedTo(message, Guid, Parent, hitPlayer.Parent);
			await hitPlayer.TakeHitDamage(damage, (HitBoxType)message.ShotData.colliderType, message.ShotData.IsMeleeAttack, message.ShotData.Orientation.ToVector3D());
		}
	}

	protected async void PlayerDrillingListener(NetworkData data)
	{
		var message = data as PlayerDrillingMessage;
		if (message.Sender == Guid && ItemInHands is HandDrill drill)
		{
			PlayerDrillingMessage pdmForOtherChar = new PlayerDrillingMessage
			{
				DrillersGUID = FakeGuid,
				dontPlayEffect = message.dontPlayEffect,
				isDrilling = message.isDrilling
			};
			await NetworkController.SendToClientsSubscribedTo(pdmForOtherChar, Guid, Parent);
			if (drill.CanDrill && message.MiningPointID != null && Server.Instance.GetVessel(message.MiningPointID.VesselGUID) is Asteroid asteroid && asteroid.MiningPoints.TryGetValue(message.MiningPointID.InSceneID, out var miningPoint))
			{
				await drill.Battery.ChangeQuantity((0f - drill.BatteryUsage) * message.MiningTime * drill.TierMultiplier);
				await drill.Canister.ChangeQuantityByAsync(0, miningPoint.ResourceType, drill.DrillingStrength * drill.TierMultiplier * message.MiningTime);
				miningPoint.Quantity = MathHelper.Clamp(miningPoint.Quantity - drill.DrillingStrength * drill.TierMultiplier * message.MiningTime, 0f, miningPoint.MaxQuantity);
				await drill.DrillBit.TakeDamage(TypeOfDamage.Degradation, drill.DrillBit.UsageWear * message.MiningTime * drill.DrillBit.TierMultiplier, forceTakeDamage: true);
			}
		}
	}

	protected async void EnvironmentReadyListener(NetworkData data)
	{
		var message = data as EnvironmentReadyMessage;
		if (message.Sender != Guid)
		{
			return;
		}

		MessagesReceivedWhileLoading = new ConcurrentQueue<ShipStatsMessage>();
		foreach (SpaceObjectVessel ves in from m in _subscribedToSpaceObjects
			select Server.Instance.GetSpaceObject(m) into m
			where m is SpaceObjectVessel
			select m as SpaceObjectVessel)
		{
			VesselObjects vesselObjects = ves.GetVesselObjects();
			if (vesselObjects.SceneTriggerExecutors != null)
			{
				foreach (SceneTriggerExecutorDetails sted in vesselObjects.SceneTriggerExecutors)
				{
					sted.IsImmediate = true;
				}
			}
			await NetworkController.SendAsync(Guid, new ShipStatsMessage
			{
				Guid = ves.Guid,
				VesselObjects = vesselObjects
			});
		}
		_lastMoveRequestTime = -1.0;
		_lastAnimationMessageTime = -1.0;
		IsAlive = true;
		EnvironmentReady = true;
	}

	private async void SuicideListener(NetworkData data)
	{
		if (data.Sender == Guid)
		{
			await KillPlayer(HurtType.Suicide);
		}
	}

	private async void AuthorizedVesselsRequestListener(NetworkData data)
	{
		if (data.Sender == Guid)
		{
			await SendAuthorizedVesselsResponse();
		}
	}

	public async Task SendAuthorizedVesselsResponse()
	{
		AuthorizedVesselsResponse avr = new AuthorizedVesselsResponse
		{
			GUIDs = (from m in Server.Instance.AllVessels
				where m.AuthorizedPersonel.FirstOrDefault((AuthorizedPerson n) => n.PlayerId == PlayerId) != null
				select m.Guid).ToArray()
		};
		await NetworkController.SendAsync(Guid, avr);
	}

	private async void QuestTriggerMessageListener(NetworkData data)
	{
		var message = data as QuestTriggerMessage;
		if (message.Sender != Guid)
		{
			return;
		}
		Quest quest = Quests.FirstOrDefault((Quest m) => m.ID == message.QuestID);
		if (quest == null || (quest.DependencyQuests != null && Quests.FirstOrDefault((Quest m) => quest.DependencyQuests.Contains(m.ID) && m.Status != QuestStatus.Completed) != null))
		{
			return;
		}
		QuestTrigger qt = quest.QuestTriggers.FirstOrDefault((QuestTrigger m) => m.ID == message.TriggerID);
		if (qt is not { Status: QuestStatus.Active })
		{
			return;
		}
		await qt.SetQuestStatusAsync(QuestStatus.Completed);
		await qt.UpdateDependentTriggers(quest);
		List<Task> tasks = new List<Task>();
		if (qt.Type == QuestTriggerType.Activate && quest.Status == QuestStatus.Inactive)
		{
			quest.UpdateActivation();
			if (quest.ActivationDependencyTpe == QuestTriggerDependencyTpe.Any)
			{
				foreach (QuestTrigger aqt in quest.QuestTriggers.Where((QuestTrigger m) => m.Type == QuestTriggerType.Activate))
				{
					await aqt.SetQuestStatusAsync(QuestStatus.Completed);
				}
			}
		}
		else if (qt.Type == QuestTriggerType.Complete && !quest.IsFineshed)
		{
			quest.UpdateCompletion();
			if (quest.Status == QuestStatus.Completed)
			{
				foreach (Quest aaq in Quests.Where((Quest m) => m.AutoActivate && m.DependencyQuests.Contains(quest.ID) && m.Status == QuestStatus.Inactive))
				{
					List<Quest> depQuests = aaq.DependencyQuests.Select((uint m) => Quests.First((Quest n) => n.ID == m)).ToList();
					if (depQuests.Count() == depQuests.Count((Quest m) => m.Status == QuestStatus.Completed))
					{
						aaq.Status = QuestStatus.Active;
						foreach (QuestTrigger aaqt in aaq.QuestTriggers.Where((QuestTrigger m) => m.Type == QuestTriggerType.Activate && m.Status != QuestStatus.Completed))
						{
							await aaqt.SetQuestStatusAsync(QuestStatus.Completed);
							await aaqt.UpdateDependentTriggers(aaq);
						}
					}
					tasks.Add(
						NetworkController.SendAsync(message.Sender, new QuestStatsMessage
						{
							QuestDetails = aaq.GetDetails()
						}
					));
				}
			}
		}
		await NetworkController.SendAsync(message.Sender, new QuestStatsMessage
		{
			QuestDetails = quest.GetDetails()
		});
		foreach (Task t in tasks)
		{
			await t;
		}
	}

	private async void SkipQuestMessageListener(NetworkData data)
	{
		var message = data as SkipQuestMessage;
		if (message.Sender != Guid)
		{
			return;
		}
		Quest quest = Quests.FirstOrDefault((Quest m) => m.ID == message.QuestID);
		if (quest == null)
		{
			return;
		}
		quest.Status = QuestStatus.Completed;
		foreach (QuestTrigger qt in quest.QuestTriggers)
		{
			await qt.SetQuestStatusAsync(QuestStatus.Completed);
		}
		List<Task> tasks = new List<Task>();
		foreach (Quest aaq in Quests.Where((Quest m) => m.AutoActivate && m.DependencyQuests.Contains(quest.ID) && m.Status == QuestStatus.Inactive))
		{
			List<Quest> depQuests = aaq.DependencyQuests.Select((uint m) => Quests.First((Quest n) => n.ID == m)).ToList();
			if (depQuests.Count() == depQuests.Count((Quest m) => m.Status == QuestStatus.Completed))
			{
				aaq.Status = QuestStatus.Active;
				foreach (QuestTrigger aaqt in aaq.QuestTriggers.Where((QuestTrigger m) => m.Type == QuestTriggerType.Activate && m.Status != QuestStatus.Completed))
				{
					await aaqt.SetQuestStatusAsync(QuestStatus.Completed);
					await aaqt.UpdateDependentTriggers(aaq);
				}
			}
			tasks.Add(NetworkController.SendAsync(message.Sender, new QuestStatsMessage
				{
					QuestDetails = aaq.GetDetails()
				}));
		}
		await NetworkController.SendAsync(message.Sender, new QuestStatsMessage
		{
			QuestDetails = quest.GetDetails()
		});
		foreach (Task t in tasks)
		{
			await t;
		}
	}

	private async void LockToTriggerMessageListener(NetworkData data)
	{
		var message = data as LockToTriggerMessage;
		if (message.Sender == Guid)
		{
			if (message.TriggerID == null)
			{
				LockedToTriggerID = null;
				IsPilotingVessel = message.IsPilotingVessel;
			}
			else if (Server.Instance.AllPlayers.FirstOrDefault((Player m) => m.Guid != Guid && m.LockedToTriggerID != null && m.LockedToTriggerID.Equals(message.TriggerID)) == null)
			{
				LockedToTriggerID = message.TriggerID;
				IsPilotingVessel = message.IsPilotingVessel;
				await NetworkController.SendAsync(message.Sender, message);
			}
		}
	}

	private void MoveObjectRequestListener(NetworkData data)
	{
		var message = data as MoveObjectRequest;
		if (message.Guid != FakeGuid || message.Sender != Guid)
		{
			return;
		}

		_pendingMoveRequests.Enqueue(message);
	}

	private void ChangeParentMessageListener(NetworkData data)
	{
		var message = data as ChangeParentMessage;
		if (message.Guid != FakeGuid || message.Sender != Guid)
		{
			return;
		}

		_pendingMoveRequests.Enqueue(message);
	}

	/// <summary>
	/// 	Changes which object the player belongs to. Only bookkeeping: crew, rooms, life support. The
	/// 	player does not move, so nothing here reads a position off the message.
	/// </summary>
	private async Task ChangeParent(ChangeParentMessage message)
	{
		if (Parent == null || message.PreviousParentGuid != Parent.Guid)
		{
			// Already moved on; this describes a parent we no longer have.
			return;
		}

		SpaceObject newParent;
		if (message.ParentGuid == FakeGuid)
		{
			if (Parent is not SpaceObjectVessel vesselLeft)
			{
				Debug.LogWarning("Cannot create a pivot without a vessel to leave", Guid, Name,
					"parent", Parent.Guid, "type", Parent.GetType().Name);
				return;
			}

			Pivot pivot = new Pivot(this, vesselLeft.MainVessel);
			pivot.Orbit.SetLastChangeTime(Server.SolarSystemTime);
			newParent = pivot;
		}
		else if (Server.Instance.TryGetSpaceObject(message.ParentGuid, out SpaceObject requestedParent)
			&& requestedParent is ArtificialBody requestedBody)
		{
			newParent = requestedBody;
		}
		else
		{
			Debug.LogWarning("Ignored a parent change naming a parent nothing can be measured against",
				Guid, Name, "requested", message.ParentGuid);
			return;
		}

		SpaceObject oldParent = Parent;
		foreach (Player pl in Server.Instance.AllPlayers)
		{
			if (pl.IsSubscribedTo(oldParent.Guid))
			{
				pl.SubscribeTo(newParent);
			}
		}

		ReparentKeepingWorldPose(newParent);
		if (oldParent is Pivot oldPivot)
		{
			await oldPivot.Destroy();
		}
	}

	/// <summary>
	/// 	Moves the player to another parent without moving it in the world, re-expressing its pose in the
	/// 	new parent's structure frame.
	/// </summary>
	private void ReparentKeepingWorldPose(SpaceObject newParent)
	{
		Vector3D worldPosition = Position;
		QuaternionD worldRotation = Rotation;
		Vector3D worldVelocity = Velocity;

		Parent = newParent;

		QuaternionD parentRotationInverse = QuaternionD.Inverse(Parent.Rotation);
		Vector3D localPosition = parentRotationInverse * (worldPosition - Parent.Position);
		LocalPosition = Parent is SpaceObjectVessel vessel ? vessel.LocalToStructurePosition(localPosition) : localPosition;
		LocalRotation = parentRotationInverse * worldRotation;
		LocalVelocity = parentRotationInverse * (worldVelocity - Parent.Velocity);
	}

	/// <summary>
	/// 	Chooses the body everything sent to this player is measured from.
	/// </summary>
	public void UpdateAnchor()
	{
		// Logged in but not yet attached to anything. Position is meaningless until then, and asking
		// for it logs an error.
		if (Parent == null)
		{
			return;
		}

		if (Parent is SpaceObjectVessel { MainVessel: { } parentMainVessel })
		{
			AnchorGuid = parentMainVessel.Guid;
			return;
		}

		// Outside: A real vessel in range is always better than a pivot.
		if (Server.Instance.SolarSystem.NearestSpaceObjectVessel(Position, AnchorKeepDistance)?.MainVessel
			is { IsWarping: false } nearest)
		{
			double reach = nearest.Guid == AnchorGuid ? AnchorKeepDistance : AnchorRebaseDistance;
			if ((Position - nearest.Position).Magnitude <= reach)
			{
				AnchorGuid = nearest.Guid;
				return;
			}
		}

		// Nothing in range but the pivot the player is on.
		if (Parent is Pivot pivot)
		{
			AnchorGuid = pivot.Guid;
		}
	}

	/// <summary>
	/// 	Applies every movement request queued since the last tick.
	/// </summary>
	public async Task ApplyPendingMoveRequests()
	{
		// Measures movement and time for all messages, not per request.
		double deltaTime = Server.SolarSystemTime - _lastMoveRequestTime;
		bool gateArmed = _lastMoveRequestTime >= 0.0 && deltaTime >= 0.0 && deltaTime <= MaxResimDeltaTime;
		Vector3D drainStartPosition = LocalPosition;
		double fastest = LocalVelocity.Magnitude;
		bool acceptedAny = false;

		while (_pendingMoveRequests.TryDequeue(out NetworkData queued))
		{
			if (!IsAlive || Parent == null)
			{
				continue;
			}

			if (queued is ChangeParentMessage parentChange)
			{
				await ChangeParent(parentChange);

				drainStartPosition = LocalPosition;
				fastest = LocalVelocity.Magnitude;
				continue;
			}

			var message = (MoveObjectRequest)queued;

			if (message.ParentGuid != Parent.Guid)
			{
				continue;
			}

			if (message.StabiliseToTargetGuid > 0 && Parent is Pivot stabilisePivot
				&& Server.Instance.GetVessel(message.StabiliseToTargetGuid) is { } stabiliseTarget)
			{
				SpaceObjectVessel refVessel = stabiliseTarget.MainVessel;
				if (refVessel.StabilizeToTargetObj != null)
				{
					refVessel = refVessel.StabilizeToTargetObj;
				}
				stabilisePivot.AdjustPositionAndVelocity(Vector3D.Zero, refVessel.Velocity - stabilisePivot.Velocity);
				stabilisePivot.Orbit.SetLastChangeTime(Server.SolarSystemTime);
			}

			Vector3D reportedPosition = message.Position.ToVector3D();
			QuaternionD reportedRotation = message.Rotation.ToQuaternionD();
			Vector3D newVelocity = message.Velocity.ToVector3D();

			// What the client believes, whether or not we go on to accept it. The movement message
			// compares this against where the player really is to decide whether we owe it a correction.
			LastReportedPosition = reportedPosition;
			LastReportedVelocity = newVelocity;

			fastest = System.Math.Max(fastest, newVelocity.Magnitude);
			if (gateArmed)
			{
				double allowedDistance = fastest * deltaTime * 2.0 + ResimPositionSlack;

				if ((reportedPosition - drainStartPosition).Magnitude > allowedDistance)
				{
					Debug.LogWarning("Refused implausible movement", Guid, Name, "reported", reportedPosition,
						"previous", drainStartPosition, "dt", deltaTime, "allowed", allowedDistance,
						"parent", Parent.Guid);
					continue;
				}

				float impactSpeed = (float)(newVelocity - LocalVelocity).Magnitude;
				if (impactSpeed > 0f)
				{
					await DoCollisionDamage(impactSpeed, "move-impact");
				}
			}

			double acceptedJump = (reportedPosition - LocalPosition).Magnitude;
			if (acceptedJump > AcceptedJumpWarningDistance)
			{
				Debug.LogWarning("Accepted large movement", Guid, Name, "jump", acceptedJump, "reported",
					reportedPosition, "previous", LocalPosition, "dt", deltaTime, "gate",
					gateArmed ? "armed" : "disarmed", "parent", Parent.Guid);
			}

			LocalPosition = reportedPosition;
			LocalRotation = reportedRotation;
			LocalVelocity = newVelocity;
			acceptedAny = true;

			if (message.HitDebrisField)
			{
				await DoCollisionDamage(9f, "debris-field");
			}

			PlayerReady = true;
		}

		if (acceptedAny)
		{
			_lastMoveRequestTime = Server.SolarSystemTime;
		}
	}

	/// <summary>
	/// 	If the player has moved in a way that warrants a transform correction.
	/// </summary>
	public bool NeedsTransformCorrection()
	{
		return LastReportedPosition is not { } reportedPosition || LastReportedVelocity is not { } reportedVelocity
			|| (LocalPosition - reportedPosition).Magnitude > TransformCorrectionEpsilon
			|| (LocalVelocity - reportedVelocity).Magnitude > TransformCorrectionEpsilon;
	}

	private async void CharacterAnimationMessageListener(NetworkData data)
	{
		var message = data as CharacterAnimationMessage;
		if (message.Guid != FakeGuid || message.Sender != Guid)
		{
			return;
		}

		AnimationData = message.AnimationData;
		MouseLook = message.MouseLook;
		FreeLookX = message.FreeLookX;
		FreeLookY = message.FreeLookY;
		JetpackDirection = message.JetpackDirection;
		RagdollData = message.RagdollData;

		// The nozzle directions are the only report that the jetpack is firing, so propellant is spent
		// over the interval this message covers.
		double burnTime = _lastAnimationMessageTime >= 0.0 ? Server.SolarSystemTime - _lastAnimationMessageTime : 0.0;
		_lastAnimationMessageTime = Server.SolarSystemTime;
		if (burnTime > 0.0 && CurrentJetpack != null && JetpackDirection != null
			&& JetpackDirection.Any(nozzle => nozzle != 0))
		{
			await CurrentJetpack.ConsumeResources(CurrentJetpack.PropellantConsumption * (float)burnTime);
		}
	}

	public override async Task UpdateTimers(double deltaTime)
	{
		if (!IsAlive)
		{
			return;
		}
		UpdateTemperature(deltaTime);
		if (CoreTemperature is < 20f or > 45f)
		{
		}
		float suffocateDamage = 0f;
		float pressureDamage = 0f;
		float exposureDamage = Parent is not Ship ? StaticData.GetPlayerExposureDamage(Parent.Position.Magnitude) * (float)deltaTime : 0f;
		if (!IsInsideSpawnPoint)
		{
			if (CurrentHelmet != null && (!CurrentHelmet.IsVisorToggleable || CurrentHelmet.IsVisorActive))
			{
				if (CurrentJetpack is { HasOxygen: true })
				{
					await CurrentJetpack.ConsumeResources(null, CurrentJetpack.OxygenConsumption * (float)deltaTime);
				}
				else
				{
					suffocateDamage = 1f * (float)deltaTime;
				}
			}
			else if (CurrentRoom == null && Parent is not Ship)
			{
				suffocateDamage = 1f * (float)deltaTime;
			}
			else if (CurrentRoom is { Breathability: < 1f })
			{
				suffocateDamage = 1f * (1f - CurrentRoom.Breathability) * (float)deltaTime;
			}
		}
		if (((CurrentRoom == null && Parent is not Ship) || CurrentRoom is { AirPressure: < 0.3f }) && (PlayerInventory.CurrOutfit == null || CurrentHelmet == null || (CurrentHelmet.IsVisorToggleable && !CurrentHelmet.IsVisorActive)))
		{
			pressureDamage = 2f * (float)deltaTime;
		}
		if (!Initialize && (suffocateDamage > float.Epsilon || pressureDamage > float.Epsilon || exposureDamage > float.Epsilon))
		{
			await TakeDamage((float)deltaTime, "environment", new PlayerDamage
			{
				HurtType = HurtType.Suffocate,
				Amount = suffocateDamage
			}, new PlayerDamage
			{
				HurtType = HurtType.Pressure,
				Amount = pressureDamage
			}, new PlayerDamage
			{
				HurtType = HurtType.SpaceExposure,
				Amount = exposureDamage
			});
		}
	}

	[Obsolete("This subscribe system needs to be replaced with a more permanent solution that works better with the new movement architecture.")]
	public void SubscribeTo(SpaceObject spaceObject)
	{
		lock (_subscribedToSpaceObjects)
		{
			_subscribedToSpaceObjects.Add(spaceObject.Guid);
			if (spaceObject is SpaceObjectVessel ves)
			{
				if (ves.IsDocked)
				{
					_subscribedToSpaceObjects.Add(ves.DockedToMainVessel.Guid);
					{
						foreach (SpaceObjectVessel obj2 in ves.DockedToMainVessel.AllDockedVessels)
						{
							_subscribedToSpaceObjects.Add(obj2.Guid);
						}
						return;
					}
				}
				if (ves.AllDockedVessels is not { Count: > 0 })
				{
					return;
				}
				foreach (SpaceObjectVessel obj in ves.AllDockedVessels)
				{
					_subscribedToSpaceObjects.Add(obj.Guid);
				}
			}
		}
	}

	[Obsolete("This subscribe system needs to be replaced with a more permanent solution that works better with the new movement architecture.")]
	public void UnsubscribeFrom(SpaceObject spaceObject)
	{
		lock (_subscribedToSpaceObjects)
		{
			_subscribedToSpaceObjects.Remove(spaceObject.Guid);
		}
	}

	[Obsolete("This subscribe system needs to be replaced with a more permanent solution that works better with the new movement architecture.")]
	public void UnsubscribeFromAll()
	{
		lock (_subscribedToSpaceObjects)
		{
			_subscribedToSpaceObjects.Clear();
		}
	}

	[Obsolete("This subscribe system needs to be replaced with a more permanent solution that works better with the new movement architecture.")]
	public bool IsSubscribedTo(SpaceObject spaceObject, bool checkParent)
	{
		lock (_subscribedToSpaceObjects)
		{
			if (!checkParent)
			{
				return _subscribedToSpaceObjects.Contains(spaceObject.Guid);
			}
			return _subscribedToSpaceObjects.Contains(spaceObject.Guid) || (spaceObject.Parent != null && _subscribedToSpaceObjects.Contains(spaceObject.Parent.Guid));
		}
	}

	[Obsolete("This subscribe system needs to be replaced with a more permanent solution that works better with the new movement architecture.")]
	public bool IsSubscribedTo(long guid)
	{
		lock (_subscribedToSpaceObjects)
		{
			return _subscribedToSpaceObjects.Contains(guid);
		}
	}

	public void PlayerRoomMessageListener(NetworkData data)
	{
		var message = data as PlayerRoomMessage;
		if (message.Sender != Guid)
		{
			return;
		}
		isOutsideRoom = message.IsOutsideRoom.HasValue && message.IsOutsideRoom.Value;
		Room newRoom = null;
		if (message.ID != null)
		{
			SpaceObjectVessel newRoomVessel = Server.Instance.GetVessel(message.ID.VesselGUID);
			if (newRoomVessel != null)
			{
				newRoom = newRoomVessel.Rooms.FirstOrDefault((Room m) => m.ID.Equals(message.ID));
			}
		}
		if (CurrentRoom != null)
		{
			CurrentRoom.RemoveAirConsumer(this);
		}
		newRoom?.AddAirConsumer(this);
		CurrentRoom = newRoom;
	}

	public ObjectsInfoResponse.PlayerData GetPlayerData(Player pl)
	{
		int spawnPointId = 0;
		if (!IsAlive && CurrentSpawnPoint != null)
		{
			spawnPointId = CurrentSpawnPoint.SpawnPointID;
		}

		return new ObjectsInfoResponse.PlayerData
		{
			Guid = FakeGuid,
			Position = LocalPosition.ToFloatArray(),
			Rotation = LocalRotation.ToFloatArray(),
			ParentId = Parent != null ? Parent.Guid : -1,
			PlayerId = PlayerId,
			SpawnPointId = spawnPointId,
			Gender = Gender,
			HeadType = HeadType,
			HairType = HairType,
			Name = Name,
			DynamicObjects = DynamicObject.GetCarriedDetails(this, pl),
			AnimationStatsMask = AnimationStatsMask,
			LockedToTriggerID = LockedToTriggerID
		};
	}

	public override async Task Destroy()
	{
		DisconnectFromNetworkController();
		foreach (DynamicObject dobj in Server.Instance.ItemsInSpaceObject(this))
		{
			await dobj.Destroy();
		}
		foreach (SpaceObjectVessel ves in Server.Instance.AllVessels)
		{
			if (ves is Ship ship)
			{
				await ship.ResetSpawnPointsForPlayer(this, sendStatsMessage: true);
			}
		}
		Server.Instance.Remove(this);
		await base.Destroy();
	}

	private void UpdateTemperature(double deltaTime)
	{
		UpdateOutfitTemperature(deltaTime);
		if (AmbientTemperature.HasValue)
		{
			CoreTemperature += (float)((AmbientTemperature - CoreTemperature) * 0.01 * deltaTime).Value;
		}
		else
		{
			CoreTemperature = SpaceExposureTemperature(CoreTemperature, 10000f, 20f, 80f, deltaTime);
		}
	}

	private void UpdateOutfitTemperature(double deltaTime)
	{
		Outfit outfit = PlayerInventory.CurrOutfit;
		if (outfit != null)
		{
			if (Parent is Pivot)
			{
				outfit.ExternalTemperature = SpaceExposureTemperature(outfit.ExternalTemperature, 10000f, 20f, 80f, deltaTime);
			}
			else if (Parent is SpaceObjectVessel)
			{
				outfit.ExternalTemperature += (float)(((Parent as SpaceObjectVessel).Temperature - outfit.ExternalTemperature) * 0.001 * deltaTime);
			}
			float outfitInsulationFactor = 0.1f;
			outfit.InternalTemperature += (float)((outfit.ExternalTemperature - outfit.InternalTemperature) * 0.1 * deltaTime * outfitInsulationFactor);
			if (CurrentHelmet is { IsVisorActive: true } && CurrentJetpack != null)
			{
				float outfitTempRegulation = 5f;
				float tempCorr = (float)MathHelper.Clamp(37f - outfit.InternalTemperature, (0f - outfitTempRegulation) * deltaTime, outfitTempRegulation * deltaTime);
				outfit.InternalTemperature += tempCorr;
			}
		}
	}

	public async Task KillPlayer(HurtType causeOfDeath, bool createCorpse = true)
	{
		IsAlive = false;
		SpaceObject killParent = Parent;
		Corpse corpse = null;
		if (createCorpse)
		{
			corpse = new Corpse(this);
		}
		else
		{
			foreach (DynamicObject dobj in Server.Instance.ItemsInSpaceObject(this))
			{
				await dobj.Destroy();
			}
		}
		PlayerInventory = new Inventory(this);
		UnsubscribeFromAll();
		Health = 100;
		VesselDamageType vesselDamageType = VesselDamageType.None;
		if (Parent is Ship)
		{
			Ship ship = Parent as Ship;
			if (ship.Health <= 0f)
			{
				vesselDamageType = ship.LastVesselDamageType;
			}
			ship.RemovePlayerFromRoom(this);
			ship.RemovePlayerFromCrew(this, checkDetails: true);
		}
		else if (Parent is Pivot)
		{
			await (Parent as Pivot).Destroy();
		}
		if (CurrentSpawnPoint is { Type: SpawnPointType.SimpleSpawn })
		{
			CurrentSpawnPoint.Player = null;
			CurrentSpawnPoint.IsPlayerInSpawnPoint = false;
			CurrentSpawnPoint = null;
		}
		Parent = null;
		CurrentRoom = null;
		isOutsideRoom = false;
		var killMsg = new KillPlayerMessage
		{
			Guid = FakeGuid,
			CauseOfDeath = causeOfDeath,
			VesselDamageType = vesselDamageType
		};
		if (NetworkController.IsPlayerConnected(Guid))
		{
			await NetworkController.SendAsync(Guid, killMsg);
			_deathDisconnectWait = 0.0;
			Server.Instance.SubscribeToTimer(UpdateTimer.TimerStep.Step_0_1_sec, DisconnectAfterDeath);
		}
		if (killParent != null)
		{
			await NetworkController.SendToClientsSubscribedToParents(killMsg, killParent, Guid);
		}
		foreach (var q in Quests.Where(q => q.Status == QuestStatus.Active))
		{
			q.Status = QuestStatus.Inactive;
			foreach (QuestTrigger qt in q.QuestTriggers)
			{
				if (qt.Type == QuestTriggerType.Activate)
				{
					await qt.SetQuestStatusAsync(QuestStatus.Active);
					continue;
				}
				await qt.SetQuestStatusAsync(QuestStatus.Inactive);
				if (qt.SpawnRuleName is null or "")
				{
					continue;
				}
				QuestTrigger.QuestTriggerID qtid = qt.GetQuestTriggerID();
				foreach (var vessel in Server.Instance.AllVessels.Where(vessel => vessel.QuestTriggerID == qtid))
				{
					vessel.SelfDestructTimer = new SelfDestructTimer(vessel, 1f)
					{
						CheckPlayersDistance = 1000.0
					};
					vessel.AuthorizedPersonel.RemoveAll((AuthorizedPerson m) => m.PlayerId == PlayerId);
				}
			}
		}
	}

	private Task DisconnectAfterDeath(double deltaTime)
	{
		// The client closed it first, which is the normal path.
		if (!NetworkController.IsPlayerConnected(Guid))
		{
			Server.Instance.UnsubscribeFromTimer(UpdateTimer.TimerStep.Step_0_1_sec, DisconnectAfterDeath);
			_deathDisconnectWait = 0.0;
			return Task.CompletedTask;
		}

		_deathDisconnectWait += deltaTime;
		if (_deathDisconnectWait > DeathDisconnectGraceSeconds)
		{
			Debug.LogInfo("Reclaiming the connection of a dead player that never closed it.", Name, Guid);
			NetworkController.DisconnectClient(Guid);
			Server.Instance.UnsubscribeFromTimer(UpdateTimer.TimerStep.Step_0_1_sec, DisconnectAfterDeath);
			_deathDisconnectWait = 0.0;
		}
		return Task.CompletedTask;
	}

	public void LogoutDisconnectReset()
	{
		if (Parent is Ship)
		{
			(Parent as Ship).RemovePlayerFromExecutors(this);
		}
		UnsubscribeFromAll();
		EnvironmentReady = false;
		PlayerReady = false;
		LastMovementMessageSolarSystemTime = -1.0;
		_lastMoveRequestTime = -1.0;
		_lastAnimationMessageTime = -1.0;
		MessagesReceivedWhileLoading = new ConcurrentQueue<ShipStatsMessage>();
		try
		{
			if (Parent is Ship ship && isOutsideRoom)
			{
				Pivot pivot = new Pivot(this, ship.MainVessel);
				pivot.StabilizeToTarget(ship, forceStabilize: true);
				ReparentKeepingWorldPose(pivot);
			}
		}
		catch (Exception)
		{
		}
	}

	public PersistenceObjectData GetPersistenceData()
	{
		PersistenceObjectDataPlayer data = new PersistenceObjectDataPlayer();
		data.GUID = Guid;
		data.FakeGUID = FakeGuid;
		if (Parent != null)
		{
			data.ParentGUID = Parent.Guid;
			data.ParentType = Parent.ObjectType;
			if (Parent.ObjectType == SpaceObjectType.PlayerPivot)
			{
				data.ParentPosition = Parent.Position.ToArray();
				data.ParentVelocity = Parent.Velocity.ToArray();
			}
		}
		else
		{
			data.ParentGUID = -1L;
			data.ParentType = SpaceObjectType.None;
		}
		data.LocalPosition = LocalPosition.ToArray();
		data.LocalRotation = LocalRotation.ToArray();
		data.IsAlive = IsAlive;
		data.Name = Name;
		data.PlayerId = PlayerId;
		data.Gender = Gender;
		data.HeadType = HeadType;
		data.HairType = HairType;
		data.Health = Health;
		data.MaxHealth = MaxHealth;
		data.AnimationData = ObjectCopier.DeepCopy(AnimationData);
		data.AnimationStatsMask = AnimationStatsMask;
		data.Velocity = LocalVelocity.ToArray();
		if (CurrentRoom != null)
		{
			data.CurrentRoomID = CurrentRoom.ID.InSceneID;
		}
		data.CoreTemperature = CoreTemperature;
		data.ChildObjects = new List<PersistenceObjectData>();
		List<DynamicObject> dynamicObjects = Server.Instance.ItemsInSpaceObject(this);
		DynamicObject outfitItem = dynamicObjects.FirstOrDefault(m => m.Item is { Slot.SlotID: -2 });
		if (outfitItem != null)
		{
			data.ChildObjects.Add(outfitItem.Item != null ? outfitItem.Item.GetPersistenceData() : outfitItem.GetPersistenceData());
		}
		foreach (DynamicObject dobj in dynamicObjects)
		{
			if (dobj != outfitItem)
			{
				data.ChildObjects.Add(dobj.Item != null ? dobj.Item.GetPersistenceData() : dobj.GetPersistenceData());
			}
		}
		data.Quests = Quests.Select((Quest m) => m.GetDetails()).ToList();
		data.Blueprints = Blueprints;
		return data;
	}

	public async Task LoadPersistenceData(PersistenceObjectData persistenceData)
	{
		if (persistenceData is not PersistenceObjectDataPlayer data)
		{
			Debug.LogError("PersistenceObjectDataPlayer data is null", Guid);
			return;
		}

		Guid = data.GUID;
		FakeGuid = data.FakeGUID;
		LocalPosition = data.LocalPosition.ToVector3D();
		LocalRotation = data.LocalRotation.ToQuaternionD();
		IsAlive = data.IsAlive;
		Name = data.Name;
		PlayerId = data.PlayerId;
		Gender = data.Gender;
		HeadType = data.HeadType;
		HairType = data.HairType;
		MaxHealth = data.MaxHealth;
		Health = data.Health;
		AnimationData = ObjectCopier.DeepCopy(data.AnimationData);
		AnimationStatsMask = data.AnimationStatsMask;
		LocalVelocity = data.Velocity.ToVector3D();
		CoreTemperature = data.CoreTemperature;
		SpaceObject parent = null;
		if (data.ParentType == SpaceObjectType.PlayerPivot)
		{
			parent = new Pivot(this, data.ParentPosition.ToVector3D(), data.ParentVelocity.ToVector3D());
		}
		else if (data.ParentGUID != -1)
		{
			parent = Server.Instance.GetSpaceObject(data.ParentGUID);
		}
		if (parent != null)
		{
			Parent = parent;
			if (data.CurrentRoomID.HasValue && Parent is SpaceObjectVessel)
			{
				CurrentRoom = (Parent as SpaceObjectVessel).Rooms.FirstOrDefault((Room m) => m.ID.InSceneID == data.CurrentRoomID.Value);
			}
		}
		else
		{
			if (data.ParentGUID != -1 && parent == null)
			{
				Debug.LogError("Player parent object not found, SAVE MIGHT BE CORRUPTED", Guid, data.ParentGUID, data.ParentType);
				return;
			}
			Parent = null;
			await KillPlayer(HurtType.None, createCorpse: false);
		}
		if (Parent != null)
		{
			foreach (PersistenceObjectDataDynamicObject dobjData in data.ChildObjects.Cast<PersistenceObjectDataDynamicObject>())
			{
				await Persistence.CreateDynamicObject(dobjData, this);
			}
		}
		if (data.Quests != null)
		{
			foreach (QuestDetails det in data.Quests)
			{
				Quest quest = Quests.FirstOrDefault((Quest m) => m.ID == det.ID);
				if (quest == null)
				{
					continue;
				}
				quest.Status = det.Status;
				foreach (QuestTriggerDetails qtDet in det.QuestTriggers)
				{
					QuestTrigger questTrigger = quest.QuestTriggers.FirstOrDefault((QuestTrigger m) => m.ID == qtDet.ID);
					if (questTrigger != null)
					{
						await questTrigger.SetQuestStatusAsync(qtDet.Status);
					}
				}
			}
		}
		if (data.Blueprints != null)
		{
			Blueprints = data.Blueprints;
		}
		Server.Instance.Add(this);
	}

	public void SetSpawnPoint(ShipSpawnPoint spawnPoint)
	{
		if (spawnPoint is { Type: SpawnPointType.WithAuthorization })
		{
			AuthorizedSpawnPoint = spawnPoint;
		}
		CurrentSpawnPoint = spawnPoint;
		if (spawnPoint != null && !IsAlive)
		{
			LocalPosition = spawnPoint.RelativePosition;
			LocalRotation = spawnPoint.RelativeRotation;
		}
	}

	public void ClearAuthorizedSpawnPoint()
	{
		AuthorizedSpawnPoint = null;
	}

	public CharacterData GetCharacterData()
	{
		return new CharacterData
		{
			Name = Name,
			Gender = Gender,
			HeadType = HeadType,
			HairType = HairType
		};
	}

	private async void ScanForObjectsRequestListener(NetworkData data)
	{
		ScanForObjectsRequest request = data as ScanForObjectsRequest;
		if (request.Sender != Guid)
		{
			return;
		}

		SubSystemRadar radar = (Parent as SpaceObjectVessel)?.MainVessel.MainDistributionManager?.GetSubSystems()
			.OfType<SubSystemRadar>().FirstOrDefault();
		if (radar == null)
		{
			return;
		}

		List<long> detected;
		if (request.ScanDirection != null)
		{
			// Directional active scan: power the radar (this starts the active-scan cooldown) and sweep the cone.
			await radar.GoOnLine();
			detected = radar.ActiveScan(request.ScanDirection.ToVector3D(), request.ScanAngle);
		}
		else
		{
			detected = radar.PassiveScan();
		}

		foreach (long guid in detected)
		{
			DiscoveredVessels.Add(guid);
		}
	}

	public Task TakeDamage(HurtType hurtType, float amount, string source = null)
	{
		return TakeDamage(1f, source, new PlayerDamage
		{
			HurtType = hurtType,
			Amount = amount
		});
	}

	public Task TakeDamage(float deltaTime, string source, params PlayerDamage[] damages)
	{
		return TakeDamage(null, deltaTime, source, damages);
	}

	/// <summary>
	/// 	Make the player lose health.
	/// </summary>
	public async Task TakeDamage(Vector3D? shotDirection, float deltaTime, string source,
		params PlayerDamage[] damages)
	{
		if (GodMode || CurrentSpawnPoint is { Executor: not null, IsPlayerInSpawnPoint: true })
		{
			return;
		}

		float unarmoured = damages.Where((PlayerDamage m) => m.Amount > 0f && m.HurtType is HurtType.Suffocate or HurtType.Pressure).Sum((PlayerDamage m) => m.Amount);
		float armoured = damages.Where((PlayerDamage m) => m.Amount > 0f && m.HurtType is not (HurtType.Suffocate or HurtType.Pressure)).Sum((PlayerDamage m) => m.Amount);
		if (PlayerInventory.CurrOutfit != null)
		{
			armoured = MathHelper.Clamp(armoured - PlayerInventory.CurrOutfit.Armor * deltaTime, 0f, float.MaxValue);
		}
		float amount = unarmoured + armoured;
		if (amount <= float.Epsilon)
		{
			return;
		}

		bool unarmouredDominates = armoured <= unarmoured;
		HurtType causeOfDeath = damages
			.Where((PlayerDamage m) => m.Amount > 0f
				&& (m.HurtType is HurtType.Suffocate or HurtType.Pressure) == unarmouredDominates)
			.MaxBy((PlayerDamage m) => m.Amount)?.HurtType ?? HurtType.None;
		float tmpHealthBefore = Health;
		Health = MathHelper.Clamp(Health - amount, 0f, MaxHealth);
		if (Health <= float.Epsilon)
		{
			await KillPlayer(causeOfDeath);
			return;
		}
		_acummulatedDamage += amount;
		PlayerStatsMessage message = new PlayerStatsMessage();
		foreach (PlayerDamage dmg in damages.Where((PlayerDamage m) => m.Amount > 0f))
		{
			PlayerDamage pd = message.DamageList.FirstOrDefault((PlayerDamage m) => m.HurtType == dmg.HurtType);
			if (pd == null)
			{
				message.DamageList.Add(dmg);
			}
			else
			{
				pd.Amount += dmg.Amount;
			}
		}
		message.ShotDirection = shotDirection.HasValue ? shotDirection.Value.ToFloatArray() : null;
		if (_acummulatedDamage > 1f)
		{
			message.GUID = FakeGuid;
			message.Health = (int)Health;
			await NetworkController.SendAsync(Guid, message);
			_acummulatedDamage = 0f;
		}
	}

	public async Task Heal(float amount)
	{
		amount = amount > 0f ? amount : 0f;
		if (amount <= float.Epsilon || Health == MaxHealth)
		{
			return;
		}
		Health = MathHelper.Clamp(Health + amount, 0f, MaxHealth);
		PlayerStatsMessage message = new PlayerStatsMessage
		{
			GUID = FakeGuid,
			Health = (int)Health
		};
		await NetworkController.SendAsync(Guid, message);
	}

	private async Task HealOverTimeStep()
	{
		_amountToHeal -= _amountToHealStep;
		if (_amountToHeal <= 0f)
		{
			await Heal(_amountToHealStep);
			_healTimer.Enabled = false;
		}
		else
		{
			await Heal(_amountToHealStep);
		}
	}

	public void HealOverTime(float amountOverSec, float duration)
	{
		if (_healTimer.Enabled)
		{
			_amountToHeal += amountOverSec * duration;
			_amountToHealStep = (_amountToHealStep + amountOverSec * 0.1f) * 0.5f;
		}
		else
		{
			_amountToHeal = amountOverSec * duration;
			_amountToHealStep = amountOverSec * 0.1f;
			_healTimer.Enabled = true;
		}
	}

	public Task DoCollisionDamage(float speed, string source)
	{
		double threshold = 6.5;
		float hp = 0f;
		if (speed >= threshold)
		{
			hp = (float)((speed - threshold) * (speed - threshold) / 10.0 + speed) * (PlayerInventory.CurrOutfit != null ? PlayerInventory.CurrOutfit.CollisionResistance : 1f);
		}
		return TakeDamage(HurtType.Impact, hp, source);
	}

	public async Task<float> TakeHitDamage(float damage, HitBoxType hitType, bool isMelee, Vector3D? direction = null, float duration = 1f, string source = "shot")
	{
		Outfit outfit = PlayerInventory.CurrOutfit;
		Helmet helmet = CurrentHelmet;
		float resistanceMulti = 1f;
		float reductionValue = 0f;
		float bodyDmgMulti = 1f;
		switch (hitType)
		{
		case HitBoxType.None:
			resistanceMulti = 0f;
			Debug.LogError("UNKNOWN HITBOX TYPE", Guid);
			break;
		case HitBoxType.Head:
			bodyDmgMulti = 10f;
			resistanceMulti = helmet?.DamageResistance ?? 1f;
			reductionValue = helmet?.DamageReduction ?? 0f;
			break;
		case HitBoxType.Torso:
			bodyDmgMulti = 5f;
			if (outfit != null)
			{
				resistanceMulti = outfit.DamageResistanceTorso;
				reductionValue = outfit.DamageReductionTorso;
			}
			break;
		case HitBoxType.Arms:
			bodyDmgMulti = 1f;
			if (outfit != null)
			{
				resistanceMulti = outfit.DamageResistanceArms;
				reductionValue = outfit.DamageReductionArms;
			}
			break;
		case HitBoxType.Legs:
			bodyDmgMulti = 1f;
			if (outfit != null)
			{
				resistanceMulti = outfit.DamageResistanceLegs;
				reductionValue = outfit.DamageReductionLegs;
			}
			break;
		case HitBoxType.Abdomen:
			bodyDmgMulti = 2f;
			if (outfit != null)
			{
				resistanceMulti = outfit.DamageResistanceAbdomen;
				reductionValue = outfit.DamageReductionAbdomen;
			}
			break;
		default:
			Debug.LogError($"UNKNOWN HITBOX TYPE: {hitType}. damage={damage:F2}, isMelee={isMelee}, duration={duration:F2}, player={Guid}, health={Health}/{MaxHealth}, outfit={(outfit != null ? outfit.GetType().Name : "null")}, helmet={(helmet != null ? helmet.GetType().Name : "null")}");
			break;
		}
		if (isMelee)
		{
			bodyDmgMulti = 1f;
		}
		float amount = (damage - reductionValue * duration) * resistanceMulti * bodyDmgMulti;
		await TakeDamage(direction, duration, source, new PlayerDamage
		{
			HurtType = HurtType.Shot,
			Amount = amount
		});
		return amount;
	}
}
