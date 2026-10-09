using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using OpenHellion;
using OpenHellion.IO;
using OpenHellion.Net;
using OpenHellion.Net.Message;
using OpenHellion.Social.RichPresence;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using ZeroGravity.CharacterMovement;
using ZeroGravity.Data;
using ZeroGravity.LevelDesign;
using ZeroGravity.Math;
using ZeroGravity.Network;
using ZeroGravity.ShipComponents;
using ZeroGravity.UI;
using Random = UnityEngine.Random;

namespace ZeroGravity.Objects
{
	public class MyPlayer : Player
	{
		public delegate void InteractLockDelegate();

		public enum HandAnimationStates
		{
			Clear,
			Left,
			Right,
			Forward,
			Back,
			Top,
			Bottom
		}

		public enum PlayerStance
		{
			Passive = 1,
			Active,
			Special
		}

		public static float SendMovementInterval = 0.1f;

		private float _sendMovementTime;

		/// <summary>Set by <see cref="MyCharacterController"/> on the frame a debris field particle hits the player;
		/// consumed and cleared by the next <see cref="MoveObjectRequest"/>.</summary>
		internal bool DebrisFieldImpactPending;

		[HideInInspector] public InteractLockDelegate OnIteractStart;

		[HideInInspector] public InteractLockDelegate OnIteractComplete;

		[HideInInspector] public InteractLockDelegate OnLockStart;

		[HideInInspector] public InteractLockDelegate OnLockComplete;

		private bool _wasJetpackOn;

		public bool SittingOnPilotSeat;

		public float MeleeRange;

		[HideInInspector] public Vector3 OldGravity;

		[SerializeField] private MyCharacterController characterController;

		public bool PlayerReady;

		public int Health;

		[SerializeField] private RagdollHelper ragdollComponent;

		[HideInInspector] public Transform SunCameraRoot;

		private Transform _sunCamera;

		[HideInInspector] public Transform PlanetsCameraRoot;

		private Transform _planetsCamera;

		[NonSerialized] public BaseSceneTrigger LookingAtTrigger;

		public HealthPostEffect healthEffect;

		public EnterAtmosphereEffect burningEffect;

		private float _pressure;

		public SoundEffect HealthSounds;

		private Vector3 _shipThrust;

		private Vector3 _shipRotation;

		private float _shipRotationStrength = 0.01f;

		private float _shipThrustStrength;

		[NonSerialized] public Player LookingAtPlayer;

		private Item _lookingAtItem;

		private Item _prevLookingAtItem;

		private RagdollCollider _lookingAtCorpseCollider;

		private const float NearRaycastDistance = 1.3f;

		private const float FarRaycastDistance = 2.2f;

		public float HideCanvasDistance = 5f;

		[Title("Outfit")]
		private InventoryUI _inventoryUI;

		public Outfit CurrentOutfit;

		public Helmet CurrentHelmet;

		public Transform Outfit;

		public Transform BasicOutfitHolder;

		public SkinnedMeshRenderer HeadSkin;

		public SkinnedMeshRenderer ReferenceHead;

		public List<SkinnedMeshRenderer> ArmSkins;

		private const float InventoryThreshold = 1f;

		private float _inventoryQuickTime = -1f;

		private float _dropThrowStartTime;

		[SerializeField] private float pickUpWhenFullTreshold = 0.5f;

		private float _pickUpWhenFullCounter;

		private float _switchItemTime;

		private const float CameraFovZoomThreshold = 0.2f;

		private float _cameraFovZoomCounter;

		private const float CameraFovZoomMinValue = 30f;

		private float _cameraFovLerpFrom;

		private float _cameraFovLerpTo;

		private float _cameraFovLerpValue;

		private const float CameraFovLerpStrength = 7.5f;

		public float CurrentPanelFov;

		public bool InLadderTrigger;

		public SceneTriggerLadder LadderTrigger;

		private PlayerStance _currentStance = PlayerStance.Passive;

		private bool _reloadCalled;

		private float _reloadButtonPressedTime;

		private const float SpecialMenuThreshold = 0.4f;

		public Rigidbody rigidBody;

		private Light _shipExteriorSunLight;

		private int _planetsRaycastLayer;

		private int _playerLookRaycastMask;

		private int _shootingRaycastMask;

		private int _highlightAttachPointMask;

		public AnimatorHelper animHelper;

		public Transform HelmetPlacementTransform;

		public Transform MuzzleFlashTransform;

		public float RcsThrustModifier = 1f;

		public SceneTriggerRoom MyRoomTrigger;

		private static MyPlayer _instance;

		private int _prevStatsMask;

		public long HomeStationGUID = -1L;

		public bool SendDockUndockMsg;

		private readonly List<GameObject> _higlightedAttachPoints = new List<GameObject>();

		private const float HighlightAttachPointsRange = 5f;

		private bool _cruiseControl;

		private float _prevEngineThrustDirection;

		private bool _pickingUpItem;

		public DebrisField InDebrisField;

		public Vector3D DebrisFieldVelocityDirection = Vector3D.Zero;

		public ShipControlMode ShipControlMode;

		private bool _sendStats;

		private bool _quickSwitchItemRunning;

		[NonSerialized] public List<ItemCompoundType> Blueprints = new List<ItemCompoundType>();

		[NonSerialized] public bool IsAdmin;

		[NonSerialized] public Vector3 ShipRotationCursor = Vector3.zero;

		private float _lastShipRotationCursorChangeTime;

		private readonly Queue<string> _shotDebugList = new Queue<string>();

		private float _healthLerpHelper;

		public static bool IsAudioDebug;

		private Item _newReloadingItem;

		private Item _currentReloadingItem;

		private bool _changeEngineThrust;

		private float _changeEngineThrustTime;

		[SerializeField] private int _showSystemsDetails;

		public bool ShowGUIElements;

		private Vector3? _cameraLerpPosFrom;

		private Vector3? _cameraLerpPosTo;

		private Quaternion? _cameraLerpRotFrom;

		private Quaternion? _cameraLerpRotTo;

		private bool _cameraLerpLocal;

		private float _cameraLerpHelper;

		private bool _isRagdolled;

		private bool _isRagdollFinished = true;

		private bool _wasInStance;

		private OcSector _currOcSector;

		private SpaceObjectVessel _prevStickToVessel;

		private Vector3 _lastSentPosition;

		private Quaternion _lastSentRotation;

		private Vector3 _lastSentVelocity;

		[NonSerialized] public bool IsAlive = true;

		public bool InIteractLayer { get; private set; }

		public bool InLockState { get; private set; }

		public bool InInteractState { get; private set; }

		public bool InLerpingState { get; set; }

		public override SpaceObjectType Type => SpaceObjectType.Player;

		public MyCharacterController FpsController => characterController;

		public bool IsDrivingShip => ShipControlMode == ShipControlMode.Piloting;

		public override BaseSceneTrigger LockedToTrigger
		{
			get => base.LockedToTrigger;
			set
			{
				if (base.LockedToTrigger != value)
				{
					_sendStats = true;
				}

				base.LockedToTrigger = value;
			}
		}

		public float Pressure
		{
			get => _pressure;
			set
			{
				_pressure = value;
				AkSoundEngine.SetRTPCValue(SoundManager.Pressure, value);
			}
		}

		private Item LookingAtItem
		{
			get => _lookingAtItem;
			set
			{
				if (_lookingAtItem != value)
				{
					_prevLookingAtItem = _lookingAtItem;
					_lookingAtItem = value;
				}
			}
		}

		public PlayerStance CurrentStance => _currentStance;

		public static MyPlayer Instance => _instance;

		public bool UseGravity => CurrentRoomTrigger != null && CurrentRoomTrigger.UseGravity;

		public new bool IsInsideSpaceObject => Parent is SpaceObjectVessel;

		public Vector3 MyVelocity => rigidBody.linearVelocity;

		public SceneTriggerExecutor CancelInteractExecutor { get; set; }

		public override SpaceObject Parent
		{
			get => base.Parent;
			set
			{
				SpaceObject previousParent = base.Parent;
				bool parentChanged = previousParent is not SpaceObjectVessel
					|| value is not SpaceObjectVessel
					|| !((previousParent as SpaceObjectVessel).MainVessel == (value as SpaceObjectVessel).MainVessel);
				base.Parent = value;

				if (PlayerReady && previousParent != null && value != null && previousParent != value)
				{
					NetworkController.SendAndForget(new ChangeParentMessage
					{
						Guid = Guid,
						ParentGuid = value.Guid,
						PreviousParentGuid = previousParent.Guid
					});
				}

				if (parentChanged)
				{
					RichPresenceManager.UpdateStatus();
				}
			}
		}

		private Vector3 ThrustForward
		{
			get
			{
				if (Parent is SpaceObjectVessel && (ShipControlMode == ShipControlMode.Docking ||
													LockedToTrigger is SceneTriggerDockingPanel))
				{
					DockingPanel docking = World.InWorldPanels.Docking;
					Transform cameraPosition = docking.DockingPort.CameraPosition;
					SpaceObjectVessel mainVessel = (Parent as SpaceObjectVessel).MainVessel;
					return mainVessel.transform.rotation *
						   cameraPosition.rotation * Vector3.forward *
						   docking.ThrustModifier;
				}

				return Parent.transform.forward;
			}
		}

		private Vector3 ThrustUp
		{
			get
			{
				if (Parent is SpaceObjectVessel && (ShipControlMode == ShipControlMode.Docking ||
													LockedToTrigger is SceneTriggerDockingPanel))
				{
					DockingPanel docking = World.InWorldPanels.Docking;
					Transform cameraPosition = docking.DockingPort.CameraPosition;
					SpaceObjectVessel mainVessel = (Parent as SpaceObjectVessel).MainVessel;
					return mainVessel.transform.rotation *
						   cameraPosition.rotation * Vector3.up *
						   docking.ThrustModifier;
				}

				return Parent.transform.up;
			}
		}

		private Vector3 ThrustRight
		{
			get
			{
				if (Parent is SpaceObjectVessel && (ShipControlMode == ShipControlMode.Docking ||
													LockedToTrigger is SceneTriggerDockingPanel))
				{
					DockingPanel docking = World.InWorldPanels.Docking;
					Transform cameraPosition = docking.DockingPort.CameraPosition;
					SpaceObjectVessel mainVessel = (Parent as SpaceObjectVessel).MainVessel;
					return mainVessel.transform.rotation *
						   cameraPosition.rotation *
						   Vector3.Cross(-Vector3.forward, Vector3.up).normalized * docking.ThrustModifier;
				}

				return Parent.transform.right;
			}
		}

		public bool MeshRenderersEnabled { get; private set; }

		public bool ShouldMoveCamera
		{
			get
			{
				if (LockedToTrigger == null)
				{
					return true;
				}

				return LockedToTrigger.CameraMovementAllowed;
			}
		}

		private void Awake()
		{
			if (_instance != null)
			{
				Debug.LogError("Created new local player (MyPlayer.cs) when one already exists.");
				Destroy(gameObject);
				return;
			}

			_instance = this;

			CurrentPanelFov = Globals.Instance.DefaultCameraFov;

			_planetsRaycastLayer = 1 << LayerMask.NameToLayer("Planets");
			_playerLookRaycastMask = (1 << LayerMask.NameToLayer("Default")) |
									 (1 << LayerMask.NameToLayer("DynamicObject")) |
									 (1 << LayerMask.NameToLayer("InteractiveTriggers")) |
									 (1 << LayerMask.NameToLayer("Player"));
			_shootingRaycastMask = (1 << LayerMask.NameToLayer("Default")) |
								   (1 << LayerMask.NameToLayer("DynamicObject")) |
								   (1 << LayerMask.NameToLayer("Player"));
			_highlightAttachPointMask = (1 << LayerMask.NameToLayer("Default")) |
										(1 << LayerMask.NameToLayer("InteractiveTriggers"));

			EventSystem.AddListener(typeof(PlayerShootingMessage), PlayerShootingMessageListener);
			EventSystem.AddListener(typeof(PlayerStatsMessage), PlayerStatMessageListener);
			EventSystem.AddListener(typeof(TextChatMessage), TextChatMessageListener);
			EventSystem.AddListener(typeof(LockToTriggerMessage), LockToTriggerMessageListener);
			EventSystem.AddListener(typeof(QuestStatsMessage), QuestStatsMessageListener);
			EventSystem.AddListener(typeof(UpdateBlueprintsMessage), UpdateBlueprintsMessageListener);
		}

		private void TextChatMessageListener(NetworkData data)
		{
			TextChatMessage textChatMessage = data as TextChatMessage;
			if (textChatMessage.GUID == -1 && textChatMessage.MessageType.HasValue)
			{
				World.InGameGUI.TextChat.CreateSystemMessage(textChatMessage.MessageType.Value,
					textChatMessage.MessageParam);
			}
			else
			{
				World.InGameGUI.TextChat.CreateMessageElement(textChatMessage.Name, textChatMessage.MessageText);
			}
		}

		protected void Start()
		{
			if (CurrentHelmet != null && (Parent is Pivot || Parent is Asteroid))
			{
				CurrentHelmet.HudUI.Radar.CanRadarWork = true;
				CurrentHelmet.HudUI.Radar.ToggleTargeting(val: true);
			}

			CheckCameraShake();
			CheckRoomTrigger(null);
			FpsController.HeadBobStrength = Settings.SettingsData.GameSettings.HeadBobStrength;
			InitializeCameraEffects();
			StartCoroutine(DebrisFieldCheckCoroutine());
			World.Invoke(nameof(LatencyTestMessage), 1f);
			Globals.SoundEffect.SwitchAmbience(SoundManager.SpaceAmbience);
		}

		public void InitializeCameraEffects()
		{
			PostProcessLayer.Antialiasing antialiasingOption =
				(PostProcessLayer.Antialiasing)Settings.SettingsData.VideoSettings.AntialiasingIndex;
			FpsController.MainCamera.GetComponent<PostProcessLayer>().antialiasingMode = antialiasingOption;
		}

		public void ItemAddedToHands(Item item)
		{
			animHelper.SetParameter(null, null, null, null, null, null, null, null, true);
			ChangeStance(PlayerStance.Passive, item.PassiveSpeedMultiplier);
			ResetCameraFov();
		}

		public void SetHandsBoxCollider(BoxCollider collider)
		{
			FpsController.SetHandsBoxCollider(collider);
		}

		public void ChangeStance(PlayerStance newStance, float speedMultiplier)
		{
			_currentStance = newStance;
			FpsController.SetStateSpeedMultiplier(speedMultiplier);
			AnimatorHelper animatorHelper = animHelper;
			PlayerStance? playerStance = _currentStance;
			animatorHelper.SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null,
				null, null, null, null, playerStance);
			if (newStance == PlayerStance.Special)
			{
				World.InGameGUI.ToggleCrosshair(false);
			}
		}

		public void Attack(ShotData shotData, Item activeItem, float thrust, float rotation, bool otherPlayer = false)
		{
			if (shotData.Range == 1f)
			{
				shotData.Range = MeleeRange;
				shotData.IsMeleeAttack = true;
			}

			SpaceObject parentSpaceObject = World.GetObject(shotData.parentGUID, shotData.parentType);
			SpaceObject spaceObject =
				Parent is not SpaceObjectVessel ? Parent : (Parent as SpaceObjectVessel).MainVessel;
			Vector3 vector =
				spaceObject.transform.rotation.Inverse() * shotData.Position.ToVector3() +
				parentSpaceObject.transform.position;
			Vector3 vector2 = spaceObject.transform.rotation.Inverse() *
							  shotData.Orientation.ToVector3();
			float range = shotData.Range;
			if (!UseGravity)
			{
				FpsController.AddForce(vector2.normalized * thrust, ForceMode.Acceleration);
				FpsController.AddTorque(transform.up * (rotation * (Mathf.PI / 180f)),
					ForceMode.Acceleration);
			}

			OtherPlayer playerHit = null;
			bool corpseHit = false;
			long hitGUID = -1L;
			RaycastHit[] array;
			if (shotData.IsMeleeAttack)
			{
				array = (from x in Physics.SphereCastAll(vector, 0.1f, vector2, range, _shootingRaycastMask)
					orderby x.distance
					select x).ToArray();
			}
			else
			{
				Debug.DrawRay(vector, vector2 * range, Color.gray, 2f);
				array = (from x in Physics.RaycastAll(vector, vector2, range, _shootingRaycastMask,
						QueryTriggerInteraction.Collide)
					orderby x.distance
					select x).ToArray();
			}

			if (array != null && array.Length > 0)
			{
				RaycastHit hit = array.FirstOrDefault(delegate(RaycastHit m)
				{
					bool flag = m.transform.GetComponentInParent<MyPlayer>() != null;
					bool flag2 = m.transform.GetComponentInParent<RagdollCollider>() != null;
					playerHit = m.transform.gameObject.GetComponentInParent<OtherPlayer>();
					corpseHit = m.transform.gameObject.GetComponentInParent<Corpse>() != null;
					return !flag && ((flag2 && (playerHit != null || corpseHit)) ||
									 (!flag2 && playerHit == null && !corpseHit));
				});
				if (hit.collider != null)
				{
					if (playerHit != null || corpseHit)
					{
						if (playerHit != null && !corpseHit)
						{
							hitGUID = playerHit.Guid;
						}

						RagdollCollider component = hit.transform.gameObject.GetComponent<RagdollCollider>();
						if (component != null)
						{
							shotData.colliderType = (byte)component.ColType;
						}

						if (activeItem is Weapon weapon)
						{
							weapon.ConsumeShotAndPlayEffect(hit, BulletImpact.BulletImpactType.Flesh, vector2);
						}

						if (corpseHit)
						{
							Rigidbody hitRigidbody = hit.rigidbody;
							if (hitRigidbody != null)
							{
								hitRigidbody.AddForce(
									Quaternion.LookRotation(FpsController.MainCamera.transform.forward,
										FpsController.MainCamera.transform.up) * new Vector3(0f, 0f, 400f),
									ForceMode.Force);
							}
						}
					}
					else
					{
						DynamicObject componentInChildren = hit.transform.GetComponentInChildren<DynamicObject>();
						if (componentInChildren != null)
						{
							if (!componentInChildren.IsAttached && componentInChildren.IsKinematic)
							{
								componentInChildren.Master = true;
								componentInChildren.ToggleKinematic(value: false);
							}

							hitGUID = componentInChildren.Guid;
							componentInChildren.AddForce(
								Quaternion.LookRotation(FpsController.MainCamera.transform.forward,
									FpsController.MainCamera.transform.up) * new Vector3(0f, 0f, 400f),
								ForceMode.Force);
							if (activeItem is Weapon weapon)
							{
								weapon.ConsumeShotAndPlayEffect(hit, BulletImpact.BulletImpactType.Object, vector2);
							}
						}
						else if (activeItem is Weapon weapon)
						{
							weapon.ConsumeShotAndPlayEffect(hit, BulletImpact.BulletImpactType.Metal, vector2);
						}
					}
				}
				else if (activeItem is Weapon weapon)
				{
					weapon.ConsumeShotAndPlayEffect(hit, BulletImpact.BulletImpactType.Object, vector2);
				}
			}
			else if (activeItem is Weapon weapon)
			{
				weapon.ConsumeShotAndPlayEffect(default(RaycastHit), BulletImpact.BulletImpactType.Object, vector2);
			}

			if (!otherPlayer)
			{
				PlayerShootingMessage playerShootingMessage = new PlayerShootingMessage
				{
					HitGUID = hitGUID,
					ShotData = shotData,
					GUID = Guid
				};
				PlayerShootingMessage data = playerShootingMessage;
				NetworkController.SendAndForget(data);
			}
		}

		private void PlayerShootingMessageListener(NetworkData data)
		{
			PlayerShootingMessage playerShootingMessage = data as PlayerShootingMessage;
			OtherPlayer player = World.GetPlayer(playerShootingMessage.GUID);
			if (player != null)
			{
				Attack(playerShootingMessage.ShotData, player.CurrentActiveItem, 0f, 0f, otherPlayer: true);
			}
		}

		private void FixedUpdate()
		{
			if (!PlayerReady || !IsAlive)
			{
				return;
			}

			int AnimationStatsMask;
			CharacterAnimationData animationData = animHelper.GetAnimationData(FpsController.IsJump, isDraw: false,
				isHolster: false, cancelInteract: false, FpsController.AirTime,
				FpsController.IsEquippingAnimationTriggered, FpsController.MeleeTriggered,
				FpsController.UseConsumableTriggered, out AnimationStatsMask);
			if (AnimationStatsMask != _prevStatsMask || _sendStats)
			{
				_sendStats = false;
				PlayerStatsMessage playerStatsMessage = new PlayerStatsMessage
				{
					GUID = Guid
				};
				if (AnimationStatsMask != _prevStatsMask)
				{
					_prevStatsMask = AnimationStatsMask;
					playerStatsMessage.AnimationMaskChanged = true;
					playerStatsMessage.Health = Health;
					playerStatsMessage.ReloadType =
						(int)animHelper.GetParameterFloat(AnimatorHelper.Parameter.ReloadType);
					playerStatsMessage.AnimationStatesMask = AnimationStatsMask;
				}

				if (LockedToTrigger != null)
				{
					playerStatsMessage.LockedToTriggerID = LockedToTrigger.GetID();
					playerStatsMessage.IsPilotingVessel = LockedToTrigger.TriggerType == SceneTriggerType.ShipControl ||
														  LockedToTrigger.TriggerType == SceneTriggerType.DockingPanel;
				}
				else
				{
					playerStatsMessage.LockedToTriggerID = null;
				}

				NetworkController.SendAndForget(playerStatsMessage);
			}

			ResetTriggerBools();
			bool stickToVesselChanged = _prevStickToVessel != FpsController.StickToVessel;
			if (stickToVesselChanged || _sendMovementTime + SendMovementInterval <= Time.fixedTime)
			{
				_sendMovementTime = Time.fixedTime;
				_prevStickToVessel = FpsController.StickToVessel;

				Transform frame = Parent.TransferableObjectsRoot.transform;
				Vector3 reportedPosition = frame.InverseTransformPoint(transform.position);
				Quaternion reportedRotation = Quaternion.Inverse(frame.rotation) * transform.rotation;
				Vector3 reportedVelocity = frame.InverseTransformDirection(rigidBody.linearVelocity);

				bool moved = reportedPosition != _lastSentPosition
					|| reportedRotation != _lastSentRotation
					|| reportedVelocity != _lastSentVelocity;

				if (moved || stickToVesselChanged || DebrisFieldImpactPending)
				{
					_lastSentPosition = reportedPosition;
					_lastSentRotation = reportedRotation;
					_lastSentVelocity = reportedVelocity;

					MoveObjectRequest moveRequest = new MoveObjectRequest
					{
						Guid = Guid,
						ParentGuid = Parent.Guid,
						Position = reportedPosition.ToArray(),
						Rotation = reportedRotation.ToArray(),
						Velocity = reportedVelocity.ToArray(),
						StabiliseToTargetGuid = FpsController.StickToVessel != null ? FpsController.StickToVessel.Guid : 0L,
						HitDebrisField = DebrisFieldImpactPending
					};
					DebrisFieldImpactPending = false;
					NetworkController.SendAndForget(moveRequest);
				}

				// Cosmetic state the server only relays to other players (never affects legality).
				CharacterAnimationMessage animationMessage = new CharacterAnimationMessage
				{
					Guid = Guid,
					AnimationData = animationData,
					MouseLook = FpsController.MouseLookXAngle
				};
				if (FpsController.IsFreeLook)
				{
					Vector2 freeLookAngle = FpsController.FreeLookAngle;
					animationMessage.FreeLookY = 0f - freeLookAngle.y;
					animationMessage.FreeLookX = 0f - freeLookAngle.x;
				}

				if (FpsController.IsZeroG && FpsController.IsJetpackOn)
				{
					animationMessage.JetpackDirection = new sbyte[4]
					{
						FpsController.CurrentJetpack.NozzleDir[0],
						FpsController.CurrentJetpack.NozzleDir[1],
						FpsController.CurrentJetpack.NozzleDir[2],
						FpsController.CurrentJetpack.NozzleDir[3]
					};
				}

				if (!_isRagdollFinished)
				{
					animationMessage.RagdollData = GetRagdollData();
				}

				NetworkController.SendAndForget(animationMessage);
			}

			if (FpsController.StickToVessel != null && CurrentRoomTrigger == null)
			{
				Quaternion quaternion = FpsController.StickToVessel.transform.rotation *
										FpsController.StickToVesselRotation.Inverse();
				FpsController.StickToVesselRotation = FpsController.StickToVessel.transform.rotation;
				Vector3 position = transform.position;
				transform.position = quaternion * transform.position;
				transform.rotation = quaternion * transform.rotation;
				FpsController.StickToVesselTangentialVelocity = (transform.position - position) / Time.deltaTime;
			}
		}

		private void LateUpdate()
		{
			FpsController.CameraController.UpdateSpineTransform();
			UpdateCameraPositions();
		}

		private void ResetTriggerBools()
		{
			FpsController.IsEquippingAnimationTriggered = false;
			FpsController.MeleeTriggered = false;
			FpsController.UseConsumableTriggered = false;
		}

		public void EquipAnimStart()
		{
			Inventory.AnimationItem_EventStart();
		}

		public void EquipAnimationEnd(int equipping)
		{
			Inventory.AnimationItem_EventEnd(equipping);
		}

		private IEnumerator LerpHealthEffect()
		{
			while (_healthLerpHelper < 1f)
			{
				_healthLerpHelper += Time.deltaTime;
				yield return new WaitForEndOfFrame();
			}

			yield return null;
		}

		private void PlayerStatMessageListener(NetworkData data)
		{
			PlayerStatsMessage playerStatsMessage = data as PlayerStatsMessage;
			if (playerStatsMessage.GUID != Guid)
			{
				return;
			}

			float num = playerStatsMessage.DamageList == null
				? 0f
				: playerStatsMessage.DamageList.Where((PlayerDamage m) => m.HurtType == HurtType.Shot)
					.Sum((PlayerDamage m) => m.Amount);
			if (num > float.Epsilon)
			{
				if (playerStatsMessage.ShotDirection != null)
				{
					Vector3 vector = playerStatsMessage.ShotDirection.ToVector3();
					Vector3 from = Vector3.ProjectOnPlane(vector, transform.up);
					if (Vector3.Angle(from, -transform.forward) < 45f)
					{
						healthEffect.Hit(num, HealthPostEffect.side.Front);
					}

					if (Vector3.Angle(from, transform.forward) < 45f)
					{
						healthEffect.Hit(num, HealthPostEffect.side.Back);
					}

					if (Vector3.Angle(from, -transform.right) < 45f)
					{
						healthEffect.Hit(num, HealthPostEffect.side.Right);
					}

					if (Vector3.Angle(from, transform.right) < 45f)
					{
						healthEffect.Hit(num, HealthPostEffect.side.Left);
					}
				}

				HealthSounds.Play(3);
			}

			float num2 = playerStatsMessage.DamageList == null
				? 0f
				: playerStatsMessage.DamageList.Where((PlayerDamage m) => m.HurtType == HurtType.Pressure)
					.Sum((PlayerDamage m) => m.Amount);
			if (num2 > float.Epsilon)
			{
				healthEffect.LowPressureHit(num2);
				HealthSounds.Play(1);
			}

			float num3 = playerStatsMessage.DamageList == null
				? 0f
				: playerStatsMessage.DamageList.Where((PlayerDamage m) => m.HurtType == HurtType.Impact)
					.Sum((PlayerDamage m) => m.Amount);
			if (num3 > float.Epsilon)
			{
				int num4 = Random.Range(0, 3);
				if (num4 == 0)
				{
					healthEffect.Hit(num3, HealthPostEffect.side.Front);
				}

				if (num4 == 1)
				{
					healthEffect.Hit(num3, HealthPostEffect.side.Back);
				}

				if (num4 == 2)
				{
					healthEffect.Hit(num3, HealthPostEffect.side.Left);
				}

				if (num4 == 3)
				{
					healthEffect.Hit(num3, HealthPostEffect.side.Right);
				}

				HealthSounds.Play(3);
			}

			float num5 = playerStatsMessage.DamageList == null
				? 0f
				: playerStatsMessage.DamageList.Where((PlayerDamage m) => m.HurtType == HurtType.Suffocate)
					.Sum((PlayerDamage m) => m.Amount);
			if (num5 > float.Epsilon)
			{
				healthEffect.SuffocationHit(num5);
				HealthSounds.Play(1);
			}

			float num6 = playerStatsMessage.DamageList == null
				? 0f
				: playerStatsMessage.DamageList.Where((PlayerDamage m) => m.HurtType == HurtType.Frost)
					.Sum((PlayerDamage m) => m.Amount);
			if (num6 > float.Epsilon)
			{
			}

			float num7 = playerStatsMessage.DamageList == null
				? 0f
				: playerStatsMessage.DamageList.Where((PlayerDamage m) => m.HurtType == HurtType.Heat)
					.Sum((PlayerDamage m) => m.Amount);
			if (num7 > float.Epsilon)
			{
				burningEffect.BurnEffect(3f);
				healthEffect.LowPressureHit(num7 * 2f);
				HealthSounds.Play(3);
			}

			float num8 = playerStatsMessage.DamageList == null
				? 0f
				: playerStatsMessage.DamageList.Where((PlayerDamage m) => m.HurtType == HurtType.Shred)
					.Sum((PlayerDamage m) => m.Amount);
			if (num8 > float.Epsilon)
			{
				burningEffect.BurnEffect(3f);
				healthEffect.LowPressureHit(num8 * 2f);
				HealthSounds.Play(3);
			}

			float num9 = playerStatsMessage.DamageList == null
				? 0f
				: playerStatsMessage.DamageList.Where((PlayerDamage m) => m.HurtType == HurtType.SpaceExposure)
					.Sum((PlayerDamage m) => m.Amount);
			if (num9 > float.Epsilon)
			{
				burningEffect.BurnEffect(3f);
				healthEffect.LowPressureHit(num9 * 2f);
				HealthSounds.Play(3);
			}

			Health = playerStatsMessage.Health;
			healthEffect.Health = Health;
			AkSoundEngine.SetRTPCValue(SoundManager.Health, Health);
			if (_inventoryUI.gameObject.activeInHierarchy)
			{
				_inventoryUI.UpdateArmorAndHealth();
			}
		}

		/// <summary>
		/// 	Updates the positions of the cameras looking at planets, essentially moving where the planets are in the skybox.
		/// </summary>
		public void UpdateCameraPositions()
		{
			if (Parent != null)
			{
				Vector3D currentWorldPosition = World.LocalToWorldPosition(transform.position);
				SunCameraRoot.position = (currentWorldPosition / 149597870.7).ToVector3();
				_sunCamera.localRotation = FpsController.MainCamera.transform.rotation;
				SunCameraRoot.rotation = Quaternion.identity;
				_planetsCamera.localPosition = transform.position / 1000000.0f;
				if (CurrentStance == PlayerStance.Special && Inventory.ItemInHands is Weapon &&
					(Inventory.ItemInHands as Weapon).CanZoom)
				{
					_planetsCamera.localRotation = (Inventory.ItemInHands as MilitaryCorpRailgunSniper).zoomCamera
						.transform.rotation;
				}
				else
				{
					_planetsCamera.localRotation = FpsController.MainCamera.transform.rotation;
				}

				World.PlanetsRootTransform.rotation = Quaternion.identity;
				World.PlanetsSunLightTransform.forward = currentWorldPosition.Normalized.ToVector3();
				World.ShipSunLightTransform.forward = currentWorldPosition.Normalized.ToVector3();
				World.ShipSunLightTransform.position = transform.position;
				UpdateSunFlareAndItensity(Quaternion.identity, currentWorldPosition);
			}
		}

		private void UpdateSunFlareAndItensity(Quaternion lookRotation, Vector3D playerPosition)
		{
			Vector3 vector = lookRotation.Inverse() * -playerPosition.Normalized.ToVector3();
			bool flag = true;
			if (Physics.Raycast(_planetsCamera.position, vector, out var _, float.PositiveInfinity,
					_planetsRaycastLayer))
			{
				flag = false;
			}

			if (flag && _shipExteriorSunLight.intensity < 1f)
			{
				_shipExteriorSunLight.intensity = Mathf.Clamp01(_shipExteriorSunLight.intensity + Time.deltaTime * 1f);
				if (_shipExteriorSunLight.GetComponentsInChildren<SunFlareEffect>().Length > 0)
				{
					_shipExteriorSunLight.GetComponentsInChildren<SunFlareEffect>()[0]
						.UpdateFlareBrightness(_shipExteriorSunLight.intensity);
				}
			}
			else if (!flag && _shipExteriorSunLight.intensity > 0f)
			{
				_shipExteriorSunLight.intensity = Mathf.Clamp01(_shipExteriorSunLight.intensity - Time.deltaTime * 1f);
				if (_shipExteriorSunLight.GetComponentsInChildren<SunFlareEffect>().Length > 0)
				{
					_shipExteriorSunLight.GetComponentsInChildren<SunFlareEffect>()[0]
						.UpdateFlareBrightness(_shipExteriorSunLight.intensity);
				}
			}
		}

		private void Update()
		{
			if (Keyboard.current.leftShiftKey.isPressed && Keyboard.current.iKey.isPressed)
			{
				IsAudioDebug = !IsAudioDebug;
			}

			TriggerRaycast();
			UpdateInput();
			UpdateShipInput();
			if (_cameraFovLerpValue.IsNotEpsilonZero())
			{
				LerpCameraFov();
			}

			if (Parent == null)
			{
				return;
			}

			// Smooth, stabilise, and regulate docking and ship movements.
			if ((ShipControlMode == ShipControlMode.Piloting || ShipControlMode == ShipControlMode.Docking ||
				 LockedToTrigger is SceneTriggerDockingPanel) && Parent is SpaceObjectVessel)
			{
				SpaceObjectVessel spaceObjectVessel =
					LockedToTrigger == null ? Parent as SpaceObjectVessel : LockedToTrigger.ParentShip;
				// Docking.
				if (ShipControlMode == ShipControlMode.Docking || LockedToTrigger is SceneTriggerDockingPanel)
				{
					if (LockedToTrigger is not SceneTriggerDockingPanel || (LockedToTrigger as SceneTriggerDockingPanel)
						.MyDockingPanel.IsDockingEnabled)
					{
						SceneDockingPort dockingPort = World.InWorldPanels.Docking.DockingPort;
						if (dockingPort != null)
						{
							Quaternion quaternion =
								Quaternion.LookRotation(Parent.transform.forward, Parent.transform.up).Inverse() *
								Quaternion.LookRotation(dockingPort.transform.forward, dockingPort.transform.up);
							if (spaceObjectVessel is Ship dockingShip)
							{
								dockingShip.SetPilotInput(_shipThrust * _shipThrustStrength,
									quaternion * _shipRotation * _shipRotationStrength);
							}
						}
					}
				}
				// Set ship rotation cursor and stabilise rotation.
				else if (ShipControlMode == ShipControlMode.Piloting && !FpsController.IsFreeLook &&
						 !World.InGameGUI.ConsoleIsUp)
				{
					Vector3 oldShipRotationCursor = ShipRotationCursor;

					// Must be the same as the other mouse delta.
					Vector2 mouseDelta = Mouse.current.delta.ReadValue() * 0.05f;
					ShipRotationCursor.x +=
						(Settings.SettingsData.ControlsSettings.InvertMouseWhileDriving ? 1 : -1) * mouseDelta.y;
					ShipRotationCursor.y += mouseDelta.x;

					// Stabilise rotation cursor.
					if (ControlsSubsystem.GetButton(ControlsSubsystem.ConfigAction.Sprint))
					{
						ShipRotationCursor *= spaceObjectVessel.RCS.RotationAcceleration /
											  spaceObjectVessel.RCS.RotationStabilization;
					}

					// Calculate velocity.
					Vector3 velocity = ShipRotationCursor - oldShipRotationCursor;
					if (velocity.magnitude > spaceObjectVessel.RCS.MaxOperationRate)
					{
						velocity = velocity.normalized * spaceObjectVessel.RCS.MaxOperationRate;
						ShipRotationCursor += velocity;
					}

					// Apply changes.
					if (velocity.IsNotEpsilonZero() && !ControlsSubsystem.GetButton(ControlsSubsystem.ConfigAction.Sprint))
					{
						_lastShipRotationCursorChangeTime = Time.realtimeSinceStartup;
						_shipRotation.x = velocity.x;
						_shipRotation.y = velocity.y;
						if (_shipRotation.IsNotEpsilonZero())
						{
							(spaceObjectVessel as Ship).IsRotationStabilized = false;
						}
						else
						{
							ShipRotationCursor = Vector3.zero;
						}
					}
					else
					{
						ShipRotationCursor = Vector3.Lerp(ShipRotationCursor,
							spaceObjectVessel.transform.rotation.Inverse() *
							spaceObjectVessel.MainVessel.AngularVelocity,
							(Time.realtimeSinceStartup - _lastShipRotationCursorChangeTime) / 5f);
						ShipRotationCursor.z = 0f;
					}

					// Regulate thrust.
					Vector3 value = RcsThrustModifier * _shipThrustStrength * _shipThrust;
					if (value.IsNotEpsilonZero())
					{
						if (ShipControlMode == ShipControlMode.Piloting &&
							World.InWorldPanels.Pilot.SelectedTarget != null &&
							Mathf.Abs(Vector3.Dot(_shipThrust.normalized, spaceObjectVessel.transform.forward)) > 0.9f &&
							spaceObjectVessel.OffSpeedHelper)
						{
							Vector3 positionChange =
								World.InWorldPanels.Pilot.SelectedTarget.ArtificialBody.transform.position -
								 spaceObjectVessel.transform.position;
							Vector3 velocityChange =
								World.InWorldPanels.Pilot.SelectedTarget.ArtificialBody.Velocity -
								 spaceObjectVessel.Velocity;
							Vector3 vector3 = Vector3.ProjectOnPlane(velocityChange, positionChange.normalized);
							float num = Vector3.Angle(positionChange, spaceObjectVessel.transform.forward);
							if (num <= 3f)
							{
								_shipThrust += 0.1f * MathHelper.Clamp(1f - num / 3f, 0f, 1f) * vector3;
								_shipThrust.x = MathHelper.Clamp(_shipThrust.x, -1f, 1f);
								_shipThrust.y = MathHelper.Clamp(_shipThrust.y, -1f, 1f);
								_shipThrust.z = MathHelper.Clamp(_shipThrust.z, -1f, 1f);
								World.InWorldPanels.Pilot.CancelInvoke(World.InWorldPanels.Pilot
									.OffSpeedHelperInactive);
								World.InWorldPanels.Pilot.OffSpeedHelperActive();
								World.InWorldPanels.Pilot.Invoke(
									World.InWorldPanels.Pilot.OffSpeedHelperInactive, 1f);
							}
						}

					}

					if (spaceObjectVessel is Ship pilotedShip)
					{
						pilotedShip.SetPilotInput(RcsThrustModifier * _shipThrustStrength * _shipThrust,
							_shipRotation * _shipRotationStrength);
					}
				}
			}

			// Camera controls.
			if (_cameraLerpHelper < 1f && _cameraLerpPosTo.HasValue && _cameraLerpPosFrom.HasValue &&
				_cameraLerpRotFrom.HasValue && _cameraLerpRotTo.HasValue)
			{
				_cameraLerpHelper += Mathf.Clamp01(Time.deltaTime * 2f);
				if (_cameraLerpLocal)
				{
					FpsController.MainCamera.transform.SetLocalPositionAndRotation(
						Vector3.Lerp(_cameraLerpPosFrom.Value, _cameraLerpPosTo.Value, _cameraLerpHelper),
						Quaternion.Lerp(_cameraLerpRotFrom.Value, _cameraLerpRotTo.Value, _cameraLerpHelper));
				}
				else
				{
					FpsController.MainCamera.transform.SetPositionAndRotation(
						Vector3.Lerp(_cameraLerpPosFrom.Value, _cameraLerpPosTo.Value, _cameraLerpHelper),
						Quaternion.Lerp(_cameraLerpRotFrom.Value, _cameraLerpRotTo.Value, _cameraLerpHelper));
				}

				if (_cameraLerpHelper >= 1f)
				{
					if (_cameraLerpLocal)
					{
						FpsController.MainCamera.transform.SetLocalPositionAndRotation(_cameraLerpPosTo.Value,
							_cameraLerpRotTo.Value);
					}
					else
					{
						FpsController.MainCamera.transform.SetPositionAndRotation(_cameraLerpPosTo.Value,
							_cameraLerpRotTo.Value);
					}

					_cameraLerpPosFrom = null;
					_cameraLerpRotFrom = null;
					_cameraLerpPosTo = null;
					_cameraLerpRotTo = null;
					_cameraLerpLocal = false;
					_cameraLerpHelper = 10f;
				}
			}

			if (CurrentRoomTrigger != null)
			{
				if (Pressure != CurrentRoomTrigger.InterpolatedAirPressure)
				{
					Pressure = CurrentRoomTrigger.InterpolatedAirPressure;
				}
			}
			else if (Pressure != 0f)
			{
				Pressure = 0f;
			}
		}

		private void TriggerRaycast()
		{
			LookingAtPlayer = null;
			if (Physics.Raycast(FpsController.CameraPosition, FpsController.CameraForward, out var hitInfo,
					FarRaycastDistance, _playerLookRaycastMask))
			{
				LookingAtPlayer = hitInfo.collider.GetComponent<Player>();
				LookingAtItem = hitInfo.collider.GetComponent<Item>();
				_lookingAtCorpseCollider = hitInfo.collider.GetComponent<RagdollCollider>();
				if (LookingAtItem == null)
				{
					LookingAtTrigger = hitInfo.collider.GetComponent<BaseSceneTrigger>();
					if (LookingAtTrigger != null && LookingAtTrigger is SceneTrigger)
					{
						SceneSpawnPoint componentInChildren = (LookingAtTrigger as SceneTrigger).transform.parent
							.GetComponentInChildren<SceneSpawnPoint>();
						if (componentInChildren != null && componentInChildren.PlayerGUID > 0 &&
							componentInChildren.PlayerGUID != Guid)
						{
							LookingAtTrigger = null;
						}
					}

					if (LookingAtTrigger != null && LockedToTrigger == null &&
						LookingAtTrigger.Glossary != null && Keyboard.current.f1Key.isPressed)
					{
						AbstractGlossaryElement glossary = LookingAtTrigger.Glossary;
						World.InGameGUI.PlayerOverview.Toggle(val: true, gloss: true);
						World.InGameGUI.PlayerOverview.Glossary.OpenElement(glossary);
					}

					if (LookingAtTrigger != null && LookingAtTrigger.IsNearTrigger &&
						hitInfo.distance > NearRaycastDistance)
					{
						LookingAtTrigger = null;
					}
				}
				else
				{
					LookingAtTrigger = null;
				}

				if (((LookingAtItem != null || LookingAtTrigger != null) &&
					 (LockedToTrigger == null || FpsController.IsFreeLook)) || (_lookingAtCorpseCollider != null &&
						_lookingAtCorpseCollider.CorpseObject != null))
				{
					if (_lookingAtCorpseCollider != null && _lookingAtCorpseCollider.CorpseObject != null)
					{
						World.InGameGUI.ToggleCroshair(show: true, canLoot: true);
					}
					else if (_prevLookingAtItem != LookingAtItem && LookingAtTrigger == null)
					{
						World.InGameGUI.SetItemName(LookingAtItem);
						if ((LookingAtItem is Outfit && (LookingAtItem as Outfit).AllItems() != null &&
							 (LookingAtItem as Outfit).AllItems().Count > 0) ||
							(LookingAtItem.IsSlotContainer && LookingAtItem.Slots.Count > 0))
						{
							World.InGameGUI.ToggleCroshair(show: true, canLoot: true);
						}
						else
						{
							World.InGameGUI.ToggleCroshair(show: true);
						}
					}
					else if (LookingAtTrigger != null)
					{
						World.InGameGUI.SetItemName(null);
						World.InGameGUI.ToggleCroshair(show: true, LookingAtTrigger);
					}
				}
				else if (World.InGameGUI.IsCroshairActive)
				{
					World.InGameGUI.ToggleCroshair(show: false);
				}
			}
			else
			{
				LookingAtTrigger = null;
				LookingAtItem = null;
				if (World.InGameGUI.IsCroshairActive)
				{
					World.InGameGUI.ToggleCroshair(show: false);
				}
			}
		}

		public void ReloadItem(Item newReloadingItem, Item currentReloadingItem, AnimatorHelper.ReloadType reloadType,
			ItemType itemType)
		{
			_newReloadingItem = newReloadingItem;
			_currentReloadingItem = currentReloadingItem;
			AnimatorHelper animatorHelper = animHelper;
			AnimatorHelper.ReloadType? reloadType2 = reloadType;
			animatorHelper.SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null,
				null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, reloadType2);
			AnimatorHelper animatorHelper2 = animHelper;
			ItemType? itemType2 = itemType;
			animatorHelper2.SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null,
				null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
				null, null, null, null, null, null, null, null, null, null, null, null, null, itemType2);
			animHelper.SetParameter(null, null, null, null, null, null, null, null, null, true);
		}

		public void ReloadStepComplete(AnimatorHelper.ReloadStepType type)
		{
			switch (type)
			{
				case AnimatorHelper.ReloadStepType.ReloadStart:
					CurrentActiveItem.ReloadStepComplete(this, type, ref _currentReloadingItem,
						ref _newReloadingItem);
					break;
				case AnimatorHelper.ReloadStepType.ItemSwitch:
					CurrentActiveItem.ReloadStepComplete(this, type, ref _currentReloadingItem,
						ref _newReloadingItem);
					break;
				case AnimatorHelper.ReloadStepType.ReloadEnd:
					CurrentActiveItem.ReloadStepComplete(this, type, ref _currentReloadingItem,
						ref _newReloadingItem);
					_newReloadingItem = null;
					_currentReloadingItem = null;
					break;
				case AnimatorHelper.ReloadStepType.UnloadEnd:
					CurrentActiveItem.ReloadStepComplete(this, type, ref _currentReloadingItem,
						ref _newReloadingItem);
					_newReloadingItem = null;
					_currentReloadingItem = null;
					break;
			}
		}

		private void UpdateInput()
		{
			if (World.InGameGUI.DeadScreen.activeInHierarchy || World.IsChatOpened ||
				World.InGameGUI.IsInputFieldIsActive || World.InGameGUI.ConsoleIsUp)
			{
				return;
			}

			if (ControlsSubsystem.GetButtonDown(ControlsSubsystem.ConfigAction.ToggleJetpack))
			{
				FpsController.ToggleJetPack();
			}

			if (ControlsSubsystem.GetButtonDown(ControlsSubsystem.ConfigAction.FreeLook))
			{
				if (Inventory.ItemInHands == null)
				{
					HighlightAttachPoints();
				}
				else
				{
					Item itemInHands = Inventory.ItemInHands;
					GenericItemSubType? generic = null;
					MachineryPartType? part = null;
					int? partTier = null;
					if (itemInHands is GenericItem)
					{
						generic = (itemInHands as GenericItem).SubType;
					}

					if (itemInHands is MachineryPart)
					{
						part = (itemInHands as MachineryPart).PartType;
						partTier = (itemInHands as MachineryPart).Tier;
					}

					HighlightAttachPoints(itemInHands.Type, generic, part, partTier);
				}
			}
			else if (ControlsSubsystem.GetButtonUp(ControlsSubsystem.ConfigAction.FreeLook))
			{
				Invoke(nameof(HideHighlightedAttachPoints), 5f);
			}

			if (ControlsSubsystem.GetButtonDown(ControlsSubsystem.ConfigAction.Melee) && animHelper.CanMelee &&
				(CurrentActiveItem == null || CurrentActiveItem.HasMelee))
			{
				animHelper.SetParameterTrigger(AnimatorHelper.Triggers.Melee);
			}

			// Open the in game menu when pressing escape.
			if (Keyboard.current.escapeKey.wasPressedThisFrame && LockedToTrigger == null &&
				!World.InGameGUI.IsGameMenuOpen && !World.InGameGUI.ConsoleIsUp &&
				!World.InGameGUI.ScreenShootMod.activeInHierarchy)
			{
				if (World.InGameGUI.IsPlayerOverviewOpen)
				{
					World.InGameGUI.PlayerOverview.Toggle(val: false);
				}
				else
				{
					World.InGameGUI.Open();
				}
			}

			if (Mouse.current.middleButton.wasPressedThisFrame && !FpsController.IsZeroG)
			{
				Vector3 position = transform.position + Gravity.normalized + transform.forward * 0.7f;
				Collider[] array = Physics.OverlapSphere(position, 0.7f);
				foreach (Collider collider in array)
				{
					GenericItem componentInParent = collider.GetComponentInParent<GenericItem>();
					if (componentInParent != null && componentInParent.AttachPoint == null &&
						componentInParent.SubType == GenericItemSubType.BasketBall)
					{
						componentInParent.DynamicObj.ToggleKinematic(value: false);
						componentInParent.DynamicObj.RigidBody.AddForce(transform.forward * 4f + transform.up * 2f,
							ForceMode.VelocityChange);
						ImpactDetector componentInChildren =
							componentInParent.DynamicObj.GetComponentInChildren<ImpactDetector>(includeInactive: true);
						if (componentInChildren != null)
						{
							componentInChildren.PlayImpactSound(4f);
						}

						break;
					}
				}
			}

			// Open player overview/inventory.
			if ((Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame) &&
				(LockedToTrigger != null || CancelInteractExecutor != null))
			{
				CancelInteract();
			}
			else if (LockedToTrigger == null && !InIteractLayer && !InLerpingState &&
					 !Instance.FpsController.IsOnLadder)
			{
				if (ControlsSubsystem.GetButtonDown(ControlsSubsystem.ConfigAction.Journal))
				{
					World.InGameGUI.PlayerOverview.Toggle(!World.InGameGUI.IsPlayerOverviewOpen, 1);
				}

				if (ControlsSubsystem.GetButtonDown(ControlsSubsystem.ConfigAction.Inventory) &&
					!World.InGameGUI.IsGameMenuOpen)
				{
					_inventoryQuickTime = Time.time;
					World.InGameGUI.PlayerOverview.Toggle(!World.InGameGUI.IsPlayerOverviewOpen);
				}
				else if (ControlsSubsystem.GetButtonUp(ControlsSubsystem.ConfigAction.Inventory) &&
						 !World.InGameGUI.IsGameMenuOpen && _inventoryQuickTime > 0f)
				{
					if (Time.time - _inventoryQuickTime > InventoryThreshold && World.InGameGUI.IsPlayerOverviewOpen)
					{
						World.InGameGUI.PlayerOverview.Toggle(val: false);
					}

					_inventoryQuickTime = -1f;
				}
			}

			// Prevent us from interacting with the environment when we're in a menu.
			if (World.InGameGUI.IsGameMenuOpen || World.InGameGUI.IsPlayerOverviewOpen)
			{
				return;
			}

			// Helmet visor and lights.
			if (CurrentHelmet != null)
			{
				if (ControlsSubsystem.GetButtonDown(ControlsSubsystem.ConfigAction.ToggleVisor))
				{
					CurrentHelmet.ToggleVisor();
				}

				if (ControlsSubsystem.GetButtonDown(ControlsSubsystem.ConfigAction.ToggleLights) && !IsDrivingShip)
				{
					CurrentHelmet.ToggleLight(!CurrentHelmet.LightOn);
				}
			}

			// Drop action.
			if (ControlsSubsystem.GetButtonDown(ControlsSubsystem.ConfigAction.Drop) && Inventory.ItemInHands != null &&
				animHelper.CanDrop)
			{
				_dropThrowStartTime = Time.time;
			}
			else if (ControlsSubsystem.GetButton(ControlsSubsystem.ConfigAction.Drop) && Inventory.ItemInHands != null &&
					 animHelper.CanDrop)
			{
				if (!World.InGameGUI.ThrowingItem.activeInHierarchy &&
					Time.time - _dropThrowStartTime >= World.DROP_THRESHOLD)
				{
					World.InGameGUI.ThrowingItemToggle(val: true);
				}
			}
			else if (ControlsSubsystem.GetButtonUp(ControlsSubsystem.ConfigAction.Drop))
			{
				World.InGameGUI.ThrowingItemToggle(val: false);
				if (Inventory.ItemInHands != null)
				{
					Inventory.ItemInHands.RequestDrop(Mathf.Clamp(
						Time.time - _dropThrowStartTime - World.DROP_THRESHOLD, 0f, World.DROP_MAX_TIME));
				}

				_dropThrowStartTime = -1f;
			}

			// Ladder controls.
			if (InLadderTrigger && !FpsController.IsZeroG)
			{
				if (ControlsSubsystem.GetButtonDown(ControlsSubsystem.ConfigAction.Interact) &&
					FpsController.Velocity.magnitude < LadderTrigger.MaxAttachVelocity)
				{
					LockedToTrigger = LookingAtTrigger;
					LadderTrigger.LadderAttach(this);
				}

				if ((ControlsSubsystem.GetButtonDown(ControlsSubsystem.ConfigAction.Jump) ||
					 Keyboard.current.tabKey.wasPressedThisFrame) && FpsController.IsOnLadder)
				{
					LadderTrigger.LadderDetach(Instance);
				}
			}

			// Custom interactions, and picking up items.
			if (ControlsSubsystem.GetButtonDown(ControlsSubsystem.ConfigAction.Interact) && LookingAtItem != null &&
				animHelper.CanPickUp)
			{
				_pickUpWhenFullCounter = 0f;
				_switchItemTime = Time.time;
			}
			else if (ControlsSubsystem.GetButton(ControlsSubsystem.ConfigAction.Interact) &&
					 (LookingAtItem != null || _lookingAtCorpseCollider != null) && animHelper.CanPickUp)
			{
				if (LookingAtItem != null || _lookingAtCorpseCollider != null)
				{
					_pickUpWhenFullCounter += Time.deltaTime;
					if (_pickUpWhenFullCounter > pickUpWhenFullTreshold)
					{
						if ((LookingAtItem != null && LookingAtItem is Outfit &&
							 (LookingAtItem as Outfit).AllItems() != null) ||
							(_lookingAtCorpseCollider != null && _lookingAtCorpseCollider.CorpseObject != null) ||
							(LookingAtItem.IsSlotContainer && LookingAtItem.Slots.Count > 0))
						{
							if (LookingAtItem != null)
							{
								if (LookingAtItem is Outfit)
								{
									World.InGameGUI.PlayerOverview.Inventory.LootingTarget =
										LookingAtItem as Outfit;
									World.InGameGUI.PlayerOverview.Toggle(val: true);
								}
								else if (LookingAtItem.IsSlotContainer && LookingAtItem.Slots.Count > 0)
								{
									World.InGameGUI.PlayerOverview.Inventory.LootingTarget = LookingAtItem;
									World.InGameGUI.PlayerOverview.Toggle(val: true);
								}
							}
							else if (Vector3.Distance(transform.position,
										 _lookingAtCorpseCollider.CorpseObject.transform.position) < FarRaycastDistance)
							{
								World.InGameGUI.PlayerOverview.Inventory.LootingTarget = _lookingAtCorpseCollider
									.CorpseObject.GetComponentInParent<Corpse>().Inventory;
								World.InGameGUI.PlayerOverview.Toggle(val: true);
							}

							_pickUpWhenFullCounter = 0f;
						}
						else if (LookingAtItem.CanPlayerPickUp(this))
						{
							LookingAtItem.RequestPickUp(handsFirst: true);
							LookingAtItem = null;
							_pickUpWhenFullCounter = 0f;
						}

						_pickingUpItem = true;
					}
				}
				else
				{
					_pickUpWhenFullCounter = 0f;
				}
			}
			else if (ControlsSubsystem.GetButtonUp(ControlsSubsystem.ConfigAction.Interact) && LookingAtItem != null &&
					 animHelper.CanPickUp && Time.time - _switchItemTime < pickUpWhenFullTreshold &&
					 LookingAtItem.CanPlayerPickUp(this))
			{
				LookingAtItem.RequestPickUp();
			}

			// Cancel picking up an item.
			if (ControlsSubsystem.GetButtonUp(ControlsSubsystem.ConfigAction.Interact) && _pickingUpItem)
			{
				_pickingUpItem = false;
				return;
			}

			// Interact with trigger.
			if (ControlsSubsystem.GetButtonUp(ControlsSubsystem.ConfigAction.Interact) && LookingAtTrigger != null &&
				!_pickingUpItem && LockedToTrigger == null &&
				!LookingAtTrigger.OtherPlayerLockedToTrigger(World) && LookingAtTrigger.IsInteractable &&
				(FpsController.IsZeroG || FpsController.IsGrounded) &&
				(LookingAtTrigger.PlayerHandsCheck == PlayerHandsCheckType.DontCheck ||
				 (LookingAtTrigger.PlayerHandsCheck == PlayerHandsCheckType.HandsMustBeEmpty &&
				  (Inventory == null || Inventory.ItemInHands == null)) ||
				 (LookingAtTrigger.PlayerHandsCheck == PlayerHandsCheckType.StoreItemInHands &&
				  (Inventory == null || Inventory.StoreItemInHands())) ||
				 (LookingAtTrigger.PlayerHandsCheck == PlayerHandsCheckType.MustHaveItemInHands &&
				  LookingAtTrigger.PlayerHandsItemType != null && Inventory != null &&
				  Inventory.ItemInHands != null &&
				  LookingAtTrigger.PlayerHandsItemType.Contains(Inventory.ItemInHands.Type))))
			{
				if (LookingAtTrigger.ExclusivePlayerLocking)
				{
					NetworkController.SendAndForget(new LockToTriggerMessage
					{
						TriggerID = LookingAtTrigger.GetID(),
						IsPilotingVessel = LookingAtTrigger.TriggerType == SceneTriggerType.ShipControl ||
										   LookingAtTrigger.TriggerType == SceneTriggerType.DockingPanel
					});
				}
				else
				{
					if (!LookingAtTrigger.Interact(this))
					{
						World.InGameGUI.Alert(Localization.UnauthorizedAccess.ToUpper());
					}
				}
			}

			if (ControlsSubsystem.GetButtonDown(ControlsSubsystem.ConfigAction.Equip))
			{
				_reloadCalled = false;
				_reloadButtonPressedTime = 0f;
			}

			if (ControlsSubsystem.GetButton(ControlsSubsystem.ConfigAction.Equip) && !_reloadCalled)
			{
				_reloadButtonPressedTime += Time.deltaTime;
				if (_reloadButtonPressedTime > SpecialMenuThreshold && Inventory.ItemInHands != null &&
					animHelper.CanSpecial)
				{
					_reloadCalled = true;
					_reloadButtonPressedTime = 0f;
					Inventory.ItemInHands.Special();
				}
			}

			if ((ControlsSubsystem.GetButtonUp(ControlsSubsystem.ConfigAction.Equip) || (LookingAtItem != null &&
																	   ControlsSubsystem.GetButtonDown(ControlsSubsystem.ConfigAction.Interact))) &&
				!animHelper.GetParameterBool(AnimatorHelper.Parameter.Reloading))
			{
				if (Inventory.ItemInHands != null && (ControlsSubsystem.GetButtonUp(ControlsSubsystem.ConfigAction.Equip) ||
													  (ControlsSubsystem.GetButtonDown(ControlsSubsystem.ConfigAction.Interact) &&
													   Inventory.ItemInHands.CanReloadOnInteract(LookingAtItem))))
				{
					if (LookingAtItem != null)
					{
						Inventory.ItemInHands.Reload(LookingAtItem);
					}
					else if (Inventory.ItemInHands is Weapon)
					{
						(Inventory.ItemInHands as Weapon).ReloadFromInventory();
					}

					LookingAtItem = null;
				}
			}

			if (Mouse.current.leftButton.isPressed && animHelper.CanSwitchState)
			{
				if (LockedToTrigger == null)
				{
					if (Inventory.ItemInHands != null)
					{
						if (Inventory.ItemInHands.HasActiveStance && _currentStance == PlayerStance.Passive &&
							animHelper.doneSwitchingState)
						{
							_currentStance = PlayerStance.Active;
							AnimatorHelper animatorHelper = animHelper;
							PlayerStance? playerStance = _currentStance;
							animatorHelper.SetParameter(null, null, null, null, null, null, null, null, null, null,
								null, null, null, null, null, null, null, playerStance);
							FpsController.SetStateSpeedMultiplier(Inventory.ItemInHands.ActiveSpeedMultiplier);
						}

						if (ItemTypeRange.IsWeapon(Inventory.ItemInHands.Type))
						{
							if (animHelper.CanShoot && _currentStance != PlayerStance.Passive)
							{
								Inventory.ItemInHands.PrimaryFunction();
							}
						}
						else
						{
							Inventory.ItemInHands.PrimaryFunction();
						}
					}
				}
				else if (LockedToTrigger.TriggerType == SceneTriggerType.Turret)
				{
					(LockedToTrigger as SceneTriggerTurret).GetComponent<Turret>().Shoot();
				}
			}

			if (Mouse.current.leftButton.wasReleasedThisFrame)
			{
				if (Inventory.ItemInHands != null)
				{
					Inventory.ItemInHands.PrimaryReleased();
				}
			}

			if (ControlsSubsystem.GetButtonUp(ControlsSubsystem.ConfigAction.WeaponMod))
			{
				if (Inventory.ItemInHands != null && Inventory.ItemInHands is Weapon)
				{
					(Inventory.ItemInHands as Weapon).IncrementMod();
				}
			}

			if (!animHelper.GetParameterBool(AnimatorHelper.Parameter.Reloading) && animHelper.CanSwitchState &&
				ControlsSubsystem.GetButtonDown(ControlsSubsystem.ConfigAction.ChangeStance))
			{
				if (Inventory.ItemInHands != null && Inventory.ItemInHands.HasActiveStance)
				{
					_currentStance = _currentStance != PlayerStance.Passive ? PlayerStance.Passive : PlayerStance.Active;
					FpsController.SetStateSpeedMultiplier(_currentStance != PlayerStance.Passive
						? Inventory.ItemInHands.ActiveSpeedMultiplier
						: Inventory.ItemInHands.PassiveSpeedMultiplier);
					AnimatorHelper animatorHelper2 = animHelper;
					PlayerStance? playerStance = _currentStance;
					animatorHelper2.SetParameter(null, null, null, null, null, null, null, null, null, null, null, null,
						null, null, null, null, null, playerStance);
					if (!FpsController.MainCamera.fieldOfView.IsEpsilonEqual(Globals.Instance.DefaultCameraFov))
					{
						ChangeCamerasFov(Globals.Instance.DefaultCameraFov);
					}

					if (Inventory.ItemInHands is Weapon)
					{
						(Inventory.ItemInHands as Weapon).IsSpecialStance = false;
					}
				}
			}

			if (ControlsSubsystem.GetButton(ControlsSubsystem.ConfigAction.Sprint) && characterController.CanLockToPoint &&
				characterController.IsZeroG)
			{
				if (!characterController.IsLockedToPoint)
				{
					characterController.GrabSlowEnabled = true;
				}
			}
			else if (ControlsSubsystem.GetButtonUp(ControlsSubsystem.ConfigAction.Sprint) && characterController.IsLockedToPoint)
			{
				characterController.ResetPlayerLock();
			}

			// Handle right-click zoom.
			if (IsLockedToTrigger)
			{
				if (ControlsSubsystem.GetButtonDown(ControlsSubsystem.ConfigAction.FreeLook) &&
					FpsController.MainCamera.fieldOfView != Globals.Instance.DefaultCameraFov)
				{
					ChangeCamerasFov(Globals.Instance.DefaultCameraFov);
				}
				else if (ControlsSubsystem.GetButtonUp(ControlsSubsystem.ConfigAction.FreeLook) &&
						 FpsController.MainCamera.fieldOfView != CurrentPanelFov && CurrentPanelFov > 10f)
				{
					ChangeCamerasFov(CurrentPanelFov);
				}
				else if (Mouse.current.rightButton.wasPressedThisFrame &&
						 ControlsSubsystem.GetButton(ControlsSubsystem.ConfigAction.FreeLook))
				{
					ChangeCamerasFov(CameraFovZoomMinValue);
				}
				else if (Mouse.current.rightButton.wasReleasedThisFrame &&
						 ControlsSubsystem.GetButton(ControlsSubsystem.ConfigAction.FreeLook))
				{
					ChangeCamerasFov(Globals.Instance.DefaultCameraFov);
				}
			}
			else if (Mouse.current.rightButton.wasPressedThisFrame && CurrentStance != PlayerStance.Special)
			{
				_cameraFovZoomCounter = Time.time;
			}
			else if (Mouse.current.rightButton.isPressed && CurrentStance != PlayerStance.Special)
			{
				if (Time.time - _cameraFovZoomCounter > CameraFovZoomThreshold)
				{
					ChangeCamerasFov(CameraFovZoomMinValue);
				}
			}
			else if (Mouse.current.rightButton.wasReleasedThisFrame && !_cameraFovLerpValue.IsNotEpsilonZero())
			{
				if (Inventory.ItemInHands != null)
				{
					Inventory.ItemInHands.SecondaryFunction();
				}
			}
			else if (Mouse.current.rightButton.wasReleasedThisFrame && _cameraFovLerpValue.IsNotEpsilonZero() &&
					 _currentStance != PlayerStance.Special)
			{
				ChangeCamerasFov(Globals.Instance.DefaultCameraFov);
			}

			if (_inventoryUI != null && Inventory?.Outfit is not null && animHelper.CanDrop)
			{
				if (ControlsSubsystem.GetButtonDown(ControlsSubsystem.ConfigAction.Quick1))
				{
					StartCoroutine(QuickSwitchItem(InventorySlot.Group.Primary));
				}
				else if (ControlsSubsystem.GetButtonDown(ControlsSubsystem.ConfigAction.Quick2))
				{
					StartCoroutine(QuickSwitchItem(InventorySlot.Group.Secondary));
				}
				else if (ControlsSubsystem.GetButtonDown(ControlsSubsystem.ConfigAction.Quick3))
				{
					StartCoroutine(QuickSwitchItem(null, new List<ItemType>
					{
						ItemType.APGrenade,
						ItemType.EMPGrenade
					}));
				}
				else if (ControlsSubsystem.GetButtonDown(ControlsSubsystem.ConfigAction.Quick4))
				{
					StartCoroutine(QuickSwitchItem(null, new List<ItemType>
					{
						ItemType.AltairMedpackBig,
						ItemType.AltairMedpackSmall,
						ItemType.AltairResourceContainer
					}));
				}
			}
		}

		private void UpdateShipInput()
		{
			if (World.InGameGUI.DeadScreen.activeInHierarchy || World.IsChatOpened ||
				World.InGameGUI.IsInputFieldIsActive || World.InGameGUI.ConsoleIsUp)
			{
				return;
			}

			_shipThrust = Vector3.zero;
			_shipRotation = Vector3.zero;

			// If parent object isn't a ship, or we aren't locked to any trigger.
			if (Parent is not Ship || LockedToTrigger == null ||
				(LockedToTrigger.TriggerType != SceneTriggerType.ShipControl &&
				 LockedToTrigger.TriggerType != SceneTriggerType.Turret &&
				 LockedToTrigger.TriggerType != SceneTriggerType.DockingPanel))
			{
				return;
			}

			// Ship controls.
			Ship ship = Parent as Ship;

			// Turn engine on/off.
			if (ship.Engine != null && ControlsSubsystem.GetButtonDown(ControlsSubsystem.ConfigAction.EngineToggle))
			{
				if (ship.Engine.IsSwitchedOn())
				{
					ship.Engine.SwitchOff();
				}
				else
				{
					ship.Engine.SwitchOn();
				}
			}

			// Ship movement.
			float forwardAxis = ControlsSubsystem.GetAxisRaw(ControlsSubsystem.ConfigAction.Forward);
			float rightAxis = ControlsSubsystem.GetAxisRaw(ControlsSubsystem.ConfigAction.Right);
			float roll = ControlsSubsystem.GetAxis(ControlsSubsystem.ConfigAction.Lean);
			float pitch = 0f;
			float yaw = 0f;
			if (!FpsController.IsFreeLook)
			{
				// Must be the same as the other mouse delta.
				Vector2 mouseDelta = Mouse.current.delta.ReadValue() * 0.05f;
				if (FpsController.MouseUpAxis.IsNotEpsilonZero())
				{
					pitch = (Settings.SettingsData.ControlsSettings.InvertMouseWhileDriving ? 1 : -1) * mouseDelta.y;
				}

				if (FpsController.MouseRightAxis.IsNotEpsilonZero())
				{
					yaw = mouseDelta.x;
				}
			}

			if (LockedToTrigger.TriggerType == SceneTriggerType.Turret)
			{
				(LockedToTrigger as SceneTriggerTurret).GetComponent<Turret>()
					.SetNewRotation(pitch, yaw, Mouse.current.rightButton.isPressed);
				return;
			}

			bool thrustChanged = false;
			if (ship.Engine != null && ship.Engine.Status == SystemStatus.Online && (LockedToTrigger == null ||
					LockedToTrigger.TriggerType != SceneTriggerType.DockingPanel))
			{
				bool isEngineActive = false;
				bool thrustUpButton = ControlsSubsystem.GetButton(ControlsSubsystem.ConfigAction.ThrustUp);
				bool thrustDownButton = ControlsSubsystem.GetButton(ControlsSubsystem.ConfigAction.ThrustDown);
				if (thrustUpButton || thrustDownButton || forwardAxis.IsNotEpsilonZero())
				{
					float thrust;
					if (thrustUpButton)
					{
						thrust = 1f;
						_cruiseControl = true;
					}
					else if (thrustDownButton)
					{
						thrust = -1f;
						_cruiseControl = true;
					}
					else
					{
						thrust = Mathf.Sign(forwardAxis);
						_cruiseControl = false;
					}

					if (Mathf.Sign(ship.EngineThrustPercentage) != thrust && !_cruiseControl)
					{
						ship.EngineThrustPercentage = 0f;
					}

					isEngineActive = !ship.EngineOnLine;
					_changeEngineThrustTime += Time.deltaTime * 10f;
					if (_changeEngineThrustTime > 0.1f)
					{
						float num4 = Mathf.Clamp(ship.EngineThrustPercentage + 0.01f * thrust, -1f, 1f);
						_changeEngineThrustTime -= 0.1f;
						if (Mathf.Sign(num4) != _prevEngineThrustDirection && _prevEngineThrustDirection != 0f &&
							_cruiseControl)
						{
							num4 = 0f;
							_changeEngineThrustTime = -10f;
						}

						isEngineActive |= ship.EngineThrustPercentage != num4;
						ship.EngineThrustPercentage = num4;
						if (num4 != float.Epsilon)
						{
							_prevEngineThrustDirection = Mathf.Sign(num4);
						}
					}
				}
				else if (!_cruiseControl)
				{
					_changeEngineThrustTime = 0f;
					isEngineActive = ship.EngineOnLine || ship.EngineThrustPercentage != forwardAxis;
					ship.EngineThrustPercentage = 0f;
				}

				if (isEngineActive)
				{
					float? engineThrustPercentage = ship.EngineThrustPercentage;
					ship.ChangeStats(null, null, null, engineThrustPercentage);
				}
			}

			// Thrust forward/back.
			else if (forwardAxis.IsNotEpsilonZero())
			{
				_shipThrust = Mathf.Sign(forwardAxis) * ThrustForward;
				thrustChanged = true;
			}

			// Thrust right/left.
			if (rightAxis.IsNotEpsilonZero())
			{
				_shipThrust += Mathf.Sign(rightAxis) * ThrustRight;
				thrustChanged = true;
			}

			// Thrust up.
			if (ControlsSubsystem.GetButton(ControlsSubsystem.ConfigAction.Jump) &&
				!ControlsSubsystem.GetButton(ControlsSubsystem.ConfigAction.Crouch))
			{
				_shipThrust += ThrustUp;
				thrustChanged = true;
			}

			// Thrust down.
			if (ControlsSubsystem.GetButton(ControlsSubsystem.ConfigAction.Crouch) &&
				!ControlsSubsystem.GetButton(ControlsSubsystem.ConfigAction.Jump))
			{
				_shipThrust += -ThrustUp;
				thrustChanged = true;
			}

			if (thrustChanged && Parent is Ship)
			{
				if (ship.RCS.CanThrust())
				{
					_shipThrustStrength =
						Mathf.Clamp01(_shipThrustStrength + Time.smoothDeltaTime * World.RCS_THRUST_SENSITIVITY);
				}
				else
				{
					_shipThrustStrength = 0f;
				}
			}
			else
			{
				_shipThrustStrength = 0f;
			}

			if (ControlsSubsystem.GetButton(ControlsSubsystem.ConfigAction.Sprint))
			{
				Vector3? autoStabilize = Vector3.one;
				ship.ChangeStats(null, null, autoStabilize);
			}
			else if (ControlsSubsystem.GetButtonUp(ControlsSubsystem.ConfigAction.Sprint))
			{
				Vector3? autoStabilize = Vector3.zero;
				ship.ChangeStats(null, null, autoStabilize);
			}

			if (ControlsSubsystem.GetButtonDown(ControlsSubsystem.ConfigAction.MatchVelocity))
			{
				World.InWorldPanels.Pilot.StartTargetStabilization();
			}

			// Set if we are going to change our thrust.
			if (ControlsSubsystem.GetButtonDown(ControlsSubsystem.ConfigAction.ThrustDown) ||
				ControlsSubsystem.GetButtonDown(ControlsSubsystem.ConfigAction.ThrustUp))
			{
				_changeEngineThrust = true;
			}
			else if (ControlsSubsystem.GetButtonUp(ControlsSubsystem.ConfigAction.ThrustDown) &&
					 ControlsSubsystem.GetButtonUp(ControlsSubsystem.ConfigAction.ThrustUp))
			{
				_changeEngineThrust = false;
				_changeEngineThrustTime = 0f;
			}

			// If any one button changing thrust is pressed.
			if (_changeEngineThrust)
			{
				if (ControlsSubsystem.GetButton(ControlsSubsystem.ConfigAction.ThrustDown))
				{
					_changeEngineThrustTime += Time.deltaTime * 5f;
					if (_changeEngineThrustTime > 0.1f)
					{
						bool flag3 = ship.EngineThrustPercentage > float.Epsilon;
						ship.EngineThrustPercentage = Mathf.Clamp(ship.EngineThrustPercentage - 0.01f, -1f, 1f);
						if (flag3 && ship.EngineThrustPercentage <= 0f)
						{
							ship.EngineThrustPercentage = 0f;
							_changeEngineThrust = false;
						}

						_changeEngineThrustTime -= 0.1f;
						float? engineThrustPercentage = ship.EngineThrustPercentage;
						ship.ChangeStats(null, null, null, engineThrustPercentage);
					}
				}
				else if (ControlsSubsystem.GetButton(ControlsSubsystem.ConfigAction.ThrustUp))
				{
					_changeEngineThrustTime += Time.deltaTime * 5f;
					if (_changeEngineThrustTime > 0.1f)
					{
						bool flag4 = ship.EngineThrustPercentage < -1.401298E-45f;
						ship.EngineThrustPercentage = Mathf.Clamp(ship.EngineThrustPercentage + 0.01f, -1f, 1f);
						if (flag4 && ship.EngineThrustPercentage >= 0f)
						{
							ship.EngineThrustPercentage = 0f;
							_changeEngineThrust = false;
						}

						_changeEngineThrustTime -= 0.1f;
						float? engineThrustPercentage = ship.EngineThrustPercentage;
						ship.ChangeStats(null, null, null, engineThrustPercentage);
					}
				}
			}

			// Don't apply changes in free look mode.
			if (FpsController.IsFreeLook)
			{
				return;
			}

			bool appliedRotation = false;

			// Lean ship. (roll)
			if (roll.IsNotEpsilonZero())
			{
				_shipRotation.z = 0f - Mathf.Clamp(roll, -1f, 1f);
				appliedRotation = true;
			}

			// Roatate ship forward/back. (pitch)
			if (pitch.IsNotEpsilonZero())
			{
				_shipRotation.x = Mathf.Clamp(pitch, -1f, 1f);
				appliedRotation = true;
			}

			// Rotate ship left/right. (yaw)
			if (yaw.IsNotEpsilonZero())
			{
				_shipRotation.y = Mathf.Clamp(yaw, -1f, 1f);
				appliedRotation = true;
			}

			// Check if we can rotate and set rotation strength.
			if (appliedRotation && Parent is Ship)
			{
				if (ship.RCS.CanRotate())
				{
					_shipRotationStrength = Mathf.Clamp01(_shipRotationStrength +
														 Time.smoothDeltaTime * World.RCS_ROTATION_SENSITIVITY);
				}
				else
				{
					_shipRotationStrength = 0f;
				}
			}
			else
			{
				_shipRotationStrength = 0f;
			}
		}

		private void CancelInteract()
		{
			if (LockedToTrigger != null)
			{
				BaseSceneTrigger lockedToTrigger = LockedToTrigger;
				bool isDrivingShip = IsDrivingShip;
				LockedToTrigger.CancelInteract(this);
				if (lockedToTrigger != null && CancelInteractExecutor != null && !isDrivingShip)
				{
					SceneTriggerExecutor cancelInteractExecutor = CancelInteractExecutor;
					CancelInteractExecutor = null;
					cancelInteractExecutor.CancelInteract();
				}
			}
			else if (CancelInteractExecutor != null)
			{
				SceneTriggerExecutor cancelInteractExecuter2 = CancelInteractExecutor;
				CancelInteractExecutor = null;
				cancelInteractExecuter2.CancelInteract();
			}
		}

		private IEnumerator QuickSwitchItem(InventorySlot.Group? group, List<ItemType> types = null)
		{
			if (_quickSwitchItemRunning)
			{
				yield break;
			}

			_quickSwitchItemRunning = true;
			Item handsItem = Inventory.HandsSlot.Item;
			float t;
			if (group.HasValue)
			{
				InventorySlot slot = Inventory.Outfit.InventorySlots
					.FirstOrDefault((KeyValuePair<short, InventorySlot> m) => m.Value.SlotGroup == group.Value).Value;
				if (slot != null)
				{
					if (slot.Item != null)
					{
						if (handsItem != null)
						{
							InventorySlot freeSlot2 = Inventory.FindEmptyOutfitSlot(handsItem);
							if (freeSlot2 == null)
							{
								_quickSwitchItemRunning = false;
								yield break;
							}

							handsItem.RequestAttach(freeSlot2);
							t = Time.realtimeSinceStartup;
							yield return new WaitUntil(() =>
								Inventory.HandsSlot.Item == null &&
								Time.realtimeSinceStartup > t + 0.3);
							if (Inventory.HandsSlot.Item != null)
							{
								_quickSwitchItemRunning = false;
								yield break;
							}
						}

						slot.Item.RequestAttach(Inventory.HandsSlot);
						t = Time.realtimeSinceStartup;
						yield return new WaitUntil(() =>
							Inventory.HandsSlot.Item != null && Time.realtimeSinceStartup > t + 0.3);
						if (Inventory.HandsSlot.Item is Weapon && CurrentStance == PlayerStance.Passive)
						{
							ChangeStance(PlayerStance.Active, 1f);
						}
						else if (CurrentStance != PlayerStance.Passive)
						{
							ChangeStance(PlayerStance.Passive, 1f);
						}
					}
					else if (handsItem != null && slot.CanFitItem(handsItem))
					{
						handsItem.RequestAttach(slot);
					}
				}
			}
			else if (types != null)
			{
				List<Item> items = (from m in Inventory.GetAllSlots().Values
					where m.Item != null && types.Contains(m.Item.Type) && (!(m.Item is ICargo) ||
																			(m.Item as ICargo).Compartments.Sum(
																				(ICargoCompartment n) =>
																					n.Capacity - n.AvailableCapacity) >
																			0f)
					select m.Item).ToList();
				List<ItemType> itemTypes = (from m in items.Select((Item m) => m.Type).Distinct()
					orderby types.IndexOf(m)
					select m).ToList();
				if (items.Count > 0)
				{
					Item nextItem2 = null;
					if (handsItem != null)
					{
						int idx = itemTypes.IndexOf(handsItem.Type) + 1;
						if (idx >= itemTypes.Count)
						{
							idx = 0;
						}

						nextItem2 = items.FirstOrDefault((Item m) => m.Type == itemTypes[idx]);
					}
					else
					{
						nextItem2 = items.FirstOrDefault((Item m) => m.Type == itemTypes[0]);
					}

					if (handsItem != null)
					{
						InventorySlot freeSlot = Inventory.FindEmptyOutfitSlot(handsItem);
						if (freeSlot == null)
						{
							_quickSwitchItemRunning = false;
							yield break;
						}

						if (CurrentStance != PlayerStance.Passive)
						{
							ChangeStance(PlayerStance.Passive, 1f);
						}

						ChangeStance(PlayerStance.Passive, 10f);
						handsItem.RequestAttach(freeSlot);
						t = Time.realtimeSinceStartup;
						yield return new WaitUntil(() =>
							Inventory.HandsSlot.Item == null && Time.realtimeSinceStartup > t + 0.3);
						if (Inventory.HandsSlot.Item != null)
						{
							_quickSwitchItemRunning = false;
							yield break;
						}
					}

					if (nextItem2 != null && (handsItem == null || nextItem2.Type != handsItem.Type))
					{
						nextItem2.RequestAttach(Inventory.HandsSlot);
					}
				}
			}

			_quickSwitchItemRunning = false;
		}

		public bool ActivatePlayer(PlayerSpawnResponse spawnResponse)
		{
			transform.SetLocalPositionAndRotation(spawnResponse.Position.ToVector3(), spawnResponse.Rotation.ToQuaternion());

			SceneSpawnPoint sceneSpawnPoint = null;
			if (spawnResponse.SpawnPointId > 0)
			{
				if (Parent is Ship)
				{
					sceneSpawnPoint = (Parent as Ship).GetStructureObject<SceneSpawnPoint>(spawnResponse.SpawnPointId);
				}

				if (sceneSpawnPoint == null)
				{
					Debug.LogErrorFormat("Cannot find spawn point in scene ({0}).", spawnResponse.SpawnPointId);
				}
			}

			Health = spawnResponse.Health;
			IsAdmin = spawnResponse.IsAdmin;
			healthEffect.Health = Health;
			AkSoundEngine.SetRTPCValue(SoundManager.Health, Health);
			gameObject.SetActive(value: true);
			rigidBody.isKinematic = false;
			FpsController.ResetVelocity();
			FpsController.ToggleMovement(true);
			AnimatorHelper animatorHelper = animHelper;
			bool? isZeroG = FpsController.IsZeroG;
			animatorHelper.SetParameter(null, null, isZeroG);
			if (sceneSpawnPoint != null && sceneSpawnPoint.Executor != null)
			{
				InIteractLayer = true;
				sceneSpawnPoint.Executor.SetExecutorDetails(new SceneTriggerExecutorDetails
				{
					PlayerThatActivated = Instance.Guid,
					InSceneID = sceneSpawnPoint.Executor.InSceneID,
					IsImmediate = true,
					IsFail = false,
					CurrentStateID = sceneSpawnPoint.Executor.CurrentStateID,
					NewStateID = sceneSpawnPoint.Executor.GetStateID(sceneSpawnPoint.ExecutorState)
				}, isInstant: false, checkCurrentState: false);
			}

			return true;
		}

		/// <summary>
		/// 	Spawns a player owned by the local machine into the world.
		/// </summary>
		public static async UniTask SpawnMyPlayer(World world, LogInResponse res)
		{
			GameObject characterObject = Instantiate(Resources.Load("Models/Units/Characters/FirstPersonCharacter")) as GameObject;
			characterObject.SetActive(false);

			GenderSettings genderSettings = characterObject.GetComponent<GenderSettings>();
			GenderSettings.GenderItem genderItem = null;
			foreach (GenderSettings.GenderItem setting in genderSettings.settings)
			{
				if (setting.Gender != res.Data.Gender)
				{
					Destroy(setting.Outfit.gameObject);
				}
				else
				{
					genderItem = setting;
				}
			}

			if (genderItem == null)
			{
				throw new ArgumentNullException();
			}

			InventoryCharacterPreview.Instance.ChangeGender(res.Data.Gender);
			MyPlayer myPlayer = characterObject.GetComponent<MyPlayer>();
			myPlayer.World = world;
			characterObject.name = "MyCharacter";

			GameObject headObject =
				Instantiate(Resources.Load("Models/Units/Characters/Heads/" + res.Data.Gender + "/Head" +
										   res.Data.HeadType), characterObject.transform, true) as GameObject;
			headObject.transform.localPosition = new Vector3(0f, -1.34f, 0f);
			headObject.transform.localRotation = Quaternion.identity;
			headObject.transform.localScale = Vector3.one;

			AnimatorHelper animatorHelper = genderItem.Outfit.GetComponent<AnimatorHelper>();
			myPlayer.FpsController.AnimatorHelper = animatorHelper;
			myPlayer.animHelper = animatorHelper;
			myPlayer.FpsController.CameraController.animatorHelper = animatorHelper;
			myPlayer.Inventory = new Inventory(myPlayer, animatorHelper);
			myPlayer._inventoryUI = world.InGameGUI.InventoryUI;

			myPlayer.HeadSkin = headObject.GetComponent<SkinnedMeshRenderer>();
			myPlayer.FpsController.HeadCameraParent = genderItem.HeadCameraParent;
			myPlayer.FpsController.RagdollChestRigidbody = myPlayer.FpsController.AnimatorHelper
				.GetBone(AnimatorHelper.HumanBones.Spine2).GetComponent<Rigidbody>();
			myPlayer.ragdollComponent = genderItem.Outfit.GetComponent<RagdollHelper>();
			myPlayer.Outfit = genderItem.Outfit;
			myPlayer.HeadSkin.rootBone =
				myPlayer.FpsController.AnimatorHelper.GetBone(AnimatorHelper.HumanBones.Spine2);
			myPlayer.ReferenceHead.rootBone =
				myPlayer.FpsController.AnimatorHelper.GetBone(AnimatorHelper.HumanBones.Spine2);
			myPlayer.ArmSkins.Clear();
			myPlayer.ArmSkins = genderItem.ArmSkins;
			myPlayer.UpdateReferenceHead();
			myPlayer.FpsController.CameraController.spineTransform =
				myPlayer.FpsController.AnimatorHelper.GetBone(AnimatorHelper.HumanBones.Spine2);
			myPlayer.RefreshOutfitData();
			myPlayer.Guid = res.GUID;
			myPlayer.PlayerName = res.Data.Name;
			myPlayer.PlayerId = Profile.PlayerId;
			myPlayer.SunCameraRoot = world.SunCameraRootTransform;
			myPlayer._sunCamera = world.SunCameraTransform;
			myPlayer.PlanetsCameraRoot = world.PlanetsCameraRootTransform;
			myPlayer._planetsCamera = world.PlanetsCameraTransform;
			myPlayer._shipExteriorSunLight = world.ShipSunLightTransform.GetComponent<Light>();
			myPlayer.rigidBody.isKinematic = true;
			AkSoundEngine.SetRTPCValue(SoundManager.InGameVolume, 0f);
			world.InGameGUI.Console.CheckIfGodAndUpdate();

			Debug.Log("Successfully spawned player.");
		}

		public void SendPlayerRoomMessage()
		{
			PlayerRoomMessage playerRoomMessage = new PlayerRoomMessage();
			if (CurrentRoomTrigger != null)
			{
				playerRoomMessage.ID = new VesselObjectID(CurrentRoomTrigger.ParentVessel.Guid,
					CurrentRoomTrigger.InSceneID);
				playerRoomMessage.IsOutsideRoom = CurrentRoomTrigger.DisablePlayerInsideOccluder;
			}
			else
			{
				playerRoomMessage.ID = null;
			}

			NetworkController.SendAndForget(playerRoomMessage);
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			_instance = null;
			EventSystem.RemoveListener(typeof(PlayerShootingMessage), PlayerShootingMessageListener);
			EventSystem.RemoveListener(typeof(PlayerStatsMessage), PlayerStatMessageListener);
			EventSystem.RemoveListener(typeof(TextChatMessage), TextChatMessageListener);
			EventSystem.RemoveListener(typeof(LockToTriggerMessage), LockToTriggerMessageListener);
			EventSystem.RemoveListener(typeof(QuestStatsMessage), QuestStatsMessageListener);
			EventSystem.RemoveListener(typeof(UpdateBlueprintsMessage), UpdateBlueprintsMessageListener);
		}

		private void OnGUI()
		{
			string text = "";
			if (Parent == null)
			{
				text = "null";
			}
			else if (Parent is SpaceObjectVessel)
			{
				text = (Parent as SpaceObjectVessel).CustomName;
			}
			else
			{
				text = Parent.gameObject.name;
			}

			if (!ShowGUIElements)
			{
				return;
			}

			if (FpsController.IsJetpackOn)
			{
				GUI.Label(new Rect(15f, 75f, 120f, 30f), "Fuel: ");
				GUI.HorizontalSlider(new Rect(45f, 81f, 350f, 30f), FpsController.JetpackFuel, 0f, 10f);
			}

			GUI.Label(new Rect(15f, 225f, 320f, 30f),
				"Occlusion status: " + (!ZeroOcclusion.UseOcclusion ? "Inactive" : "Active"));
			GUI.Label(new Rect(Screen.width - 250, 250f, 320f, 30f), "Latency: " + World.LatencyMs + " ms");
			if (_shotDebugList.Count > 0)
			{
				GUI.Label(new Rect(Screen.width / 2, Screen.height / 2, 120f, 30f), "Shotz fired: ");
				int num = 1;
				foreach (string shotDebug in _shotDebugList)
				{
					GUI.Label(new Rect(Screen.width / 2, Screen.height / 2 + (120 - 30 * num), 350f, 30f), shotDebug);
					num++;
				}
			}

			Item itemInHands = Inventory.ItemInHands;
			if (itemInHands is HandDrill)
			{
				HandDrill handDrill = itemInHands as HandDrill;
				if (handDrill.Canister != null)
				{
					GUI.Label(new Rect(250f, 220f, 250f, 30f), "Canister resources:");
					foreach (CargoResourceData resource in handDrill.Canister.GetCompartment().Resources)
					{
						GUI.Label(new Rect(250f, 250 + 30 * (int)resource.ResourceType, 250f, 30f),
							string.Concat("Resource", resource.ResourceType, " :", resource.Quantity));
					}
				}

				if (handDrill.Battery != null)
				{
					GUI.Label(new Rect(250f, 175f, 250f, 30f), "Battery:" + handDrill.Battery.CurrentPower);
				}
			}

			GUI.color = Color.red;
			GUI.Label(new Rect(15f, 15f, 250f, 30f), "Health: " + Health);
			if (Parent != null && Parent is SpaceObjectVessel)
			{
				GUI.Label(new Rect(250f, 15f, 200f, 40f),
					"Parent GUID: " + Parent.Guid + ", Health: " + (Parent as SpaceObjectVessel).Health + " (" +
					FormatHelper.Percentage((Parent as SpaceObjectVessel).Health /
											(Parent as SpaceObjectVessel).MaxHealth) + ")");
			}
			else
			{
				GUI.Label(new Rect(250f, 15f, 200f, 30f), "NO PARENT");
			}

			if (Parent is SpaceObjectVessel)
			{
				GUI.Label(new Rect(750f, 15f, 1000f, 30f),
					"Rad.sig.: " + (Parent as SpaceObjectVessel).MainVessel.RadarSignature);
			}

			GUI.color = Color.green;
			if (LockedToTrigger != null)
			{
				if (LockedToTrigger.TriggerType == SceneTriggerType.ShipControl)
				{
					GUI.Label(new Rect(15f, 55f, 250f, 30f),
						"Press '" + ControlsSubsystem.GetAxisKeyName(ControlsSubsystem.ConfigAction.Inventory) +
						"' to stop driving ship");
				}
				else if (LockedToTrigger.TriggerType == SceneTriggerType.NavigationPanel)
				{
					GUI.Label(new Rect(15f, 55f, 250f, 30f),
						"Press '" + ControlsSubsystem.GetAxisKeyName(ControlsSubsystem.ConfigAction.Inventory) +
						"' to stop interactin with panel");
				}
			}
			else if (LookingAtTrigger != null)
			{
				if (LookingAtTrigger.TriggerType == SceneTriggerType.ShipControl)
				{
					GUI.Label(new Rect(15f, 55f, 250f, 30f),
						"Press '" + ControlsSubsystem.GetAxisKeyName(ControlsSubsystem.ConfigAction.Interact) +
						"' to drive ship");
				}
				else if (LookingAtTrigger.TriggerType == SceneTriggerType.LightSwitch)
				{
					GUI.Label(new Rect(15f, 55f, 250f, 30f),
						"Press '" + ControlsSubsystem.GetAxisKeyName(ControlsSubsystem.ConfigAction.Interact) +
						"' to switch light");
				}
				else if (LookingAtTrigger.TriggerType == SceneTriggerType.NavigationPanel)
				{
					GUI.Label(new Rect(15f, 55f, 250f, 30f),
						"Press '" + ControlsSubsystem.GetAxisKeyName(ControlsSubsystem.ConfigAction.Interact) +
						"' interact with panel");
				}
				else if (LookingAtTrigger.TriggerType == SceneTriggerType.Door)
				{
					GUI.Label(new Rect(15f, 55f, 250f, 30f),
						"Press '" + ControlsSubsystem.GetAxisKeyName(ControlsSubsystem.ConfigAction.Interact) + "' interact");
				}
				else if (LookingAtTrigger.TriggerType == SceneTriggerType.Turret)
				{
					GUI.Label(new Rect(15f, 55f, 250f, 30f),
						"Press '" + ControlsSubsystem.GetAxisKeyName(ControlsSubsystem.ConfigAction.Interact) +
						"' to use turret");
				}
			}
			else if (LookingAtItem != null)
			{
				GUI.Label(new Rect(15f, 55f, 250f, 30f),
					"Press '" + ControlsSubsystem.GetAxisKeyName(ControlsSubsystem.ConfigAction.Interact) + "' to pick up item");
			}

			if (CurrentRoomTrigger != null)
			{
				GUI.color = Color.magenta;
				GUI.Label(new Rect(15f, 95f, 250f, 30f), "Press J to toggle gravity");
			}

			GUI.color = Color.green;
			if (CurrentRoomTrigger != null)
			{
				GUI.Label(new Rect(Screen.width - 200, 15f, 200f, 30f),
					$"Air quality: {CurrentRoomTrigger.AirQuality * 100f:0.##}%");
				GUI.Label(new Rect(Screen.width - 200, 30f, 200f, 30f),
					$"Air pressure: {CurrentRoomTrigger.AirPressure:0.##} bar");
			}

			if (Parent != null && Parent is Ship)
			{
				Ship ship = Parent as Ship;
				GUILayout.BeginArea(new Rect(Screen.width - 200, 55f, 450f, 300f));
				GUILayout.Label($"Velocity: {ship.Velocity.magnitude:0.00}");
				GUILayout.Label(
					$"Rotation: {ship.AngularVelocity.x:0.00}, {ship.AngularVelocity.y:0.00}, {ship.AngularVelocity.z:0.00}");
				GUILayout.Label(string.Format("Engine: {0}, Perc: {1}", !ship.EngineOnLine ? "Off" : "On",
					(int)(ship.EngineThrustPercentage * 100f) + "%"));
				GUILayout.EndArea();
				if (ship.ResDebug != null)
				{
					GUI.Label(default(Rect), "Debug resources: ");
					for (int i = 0; i < ship.ResDebug.Length; i++)
					{
						GUI.Label(new Rect(650 + 120 * i, 80f, 120f, 30f), i + ": " + ship.ResDebug[i]);
					}
				}

				if (_showSystemsDetails == 1)
				{
					GUI.color = Color.cyan;
					GUIStyle gUIStyle = new GUIStyle
					{
						alignment = TextAnchor.MiddleRight
					};
					gUIStyle.normal.textColor = Color.cyan;
					GUI.Label(new Rect(Screen.width - 450, 70f, 385f, 30f), "SUBSYSTEMS", gUIStyle);
					int num2 = 0;
					SubSystem[] componentsInChildren = ship.GeometryRoot.GetComponentsInChildren<SubSystem>();
					foreach (SubSystem subSystem in componentsInChildren)
					{
						GUI.Label(new Rect(Screen.width - 450, 95 + 20 * num2++, 385f, 30f),
							subSystem.name + " (" + subSystem.Status + ")", gUIStyle);
					}
				}
				else if (_showSystemsDetails == 2)
				{
					GUI.color = Color.cyan;
					GUIStyle gUIStyle2 = new GUIStyle
					{
						alignment = TextAnchor.MiddleRight
					};
					gUIStyle2.normal.textColor = Color.cyan;
					GUI.Label(new Rect(Screen.width - 350, 70f, 385f, 30f), "GENERATORS");
					int num3 = 0;
					Generator[] componentsInChildren2 = ship.GeometryRoot.GetComponentsInChildren<Generator>();
					foreach (Generator generator in componentsInChildren2)
					{
						GUI.Label(new Rect(Screen.width - 450, 95 + 20 * num3++, 385f, 30f),
							$"{generator.name}: {generator.Status}, {generator.SecondaryStatus}, {generator.Output}/{generator.NominalOutput}, {(!(generator.NominalOutput > 0f) ? 0f : generator.Output / generator.NominalOutput * 100f):0.#}%",
							gUIStyle2);
					}
				}
				else if (_showSystemsDetails == 3)
				{
					GUI.color = Color.cyan;
					GUI.Label(new Rect(Screen.width - 350, 70f, 385f, 30f), "RESOURCE CONTAINERS");
					int num4 = 0;
					ResourceContainer[] componentsInChildren3 =
						ship.GeometryRoot.GetComponentsInChildren<ResourceContainer>();
					foreach (ResourceContainer resourceContainer in componentsInChildren3)
					{
						GUI.Label(new Rect(Screen.width - 350, 95 + 20 * num4++, 385f, 30f),
							resourceContainer.name +
							$" ({resourceContainer.DistributionSystemType.ToString()}): {resourceContainer.Quantity:0.####} / {resourceContainer.Capacity:0.####}" +
							", " + (!resourceContainer.IsInUse ? "NOT IN USE" : "IN USE"));
					}
				}
				else if (_showSystemsDetails == 4)
				{
					GUI.color = Color.cyan;
					GUI.Label(new Rect(Screen.width - 350, 70f, 385f, 30f), "AIR VOLUMES");
					int num5 = 0;
					SceneTriggerRoom[] componentsInChildren4 =
						ship.GeometryRoot.GetComponentsInChildren<SceneTriggerRoom>();
					foreach (SceneTriggerRoom sceneTriggerRoom in componentsInChildren4)
					{
						GUI.Label(new Rect(Screen.width - 350, 95 + 20 * num5++, 385f, 30f),
							sceneTriggerRoom.name +
							$" - quality: {sceneTriggerRoom.InterpolatedAirQuality * 100f:0.##}%, pressure: {sceneTriggerRoom.InterpolatedAirPressure:0.##} bar");
					}
				}
			}

			if (InLadderTrigger && !FpsController.IsOnLadder && !FpsController.IsZeroG)
			{
				GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height / 2 - 15, 300f, 30f),
					"Press '" + ControlsSubsystem.GetAxisKeyName(ControlsSubsystem.ConfigAction.Interact) + "' to use");
			}
		}

		public override void EnterVessel(SpaceObjectVessel vessel)
		{
			if (vessel == null)
			{
				return;
			}

			SpaceObjectVessel spaceObjectVessel =
				Parent is not SpaceObjectVessel || !(Parent as SpaceObjectVessel).IsDocked
					? Parent as SpaceObjectVessel
					: (Parent as SpaceObjectVessel).DockedToMainVessel;
			SpaceObjectVessel spaceObjectVessel2 = !vessel.IsDocked ? vessel : vessel.DockedToMainVessel;
			if (Parent is Pivot)
			{
				World.RemoveArtificialBody(Parent as ArtificialBody);
				Destroy(Parent.gameObject);
				SceneQuestTrigger.OnTriggerInChildren(vessel.MainVessel.gameObject,
					SceneQuestTriggerEvent.EnterStation);
			}

			SceneQuestTrigger.OnTriggerInChildren(vessel.GeometryRoot, SceneQuestTriggerEvent.EnterVessel);
			Parent = vessel;
			if (CurrentHelmet != null && Parent is Ship)
			{
				CurrentHelmet.HudUI.Radar.CanRadarWork = false;
				CurrentHelmet.HudUI.Radar.ToggleTargeting(val: false);
			}

			if (spaceObjectVessel != null && spaceObjectVessel != spaceObjectVessel2)
			{
				spaceObjectVessel.ToggleOptimization(optimizationEnabled: true);
				foreach (SpaceObjectVessel allDockedVessel in spaceObjectVessel.AllDockedVessels)
				{
					allDockedVessel.ToggleOptimization(optimizationEnabled: true);
				}
			}

			spaceObjectVessel2.ToggleOptimization(optimizationEnabled: false);
			foreach (SpaceObjectVessel allDockedVessel2 in spaceObjectVessel2.AllDockedVessels)
			{
				allDockedVessel2.ToggleOptimization(optimizationEnabled: false);
			}

			World.SolarSystem.CenterPlanets();
			UpdateCameraPositions();
			World.CubemapRenderer.RenderCubemapForReflectionProbe();
			World.CubemapRenderer.RenderCubemapForDockingPort();
			if (spaceObjectVessel != null)
			{
				ZeroOcclusion.CheckOcclusionFor(spaceObjectVessel, onlyCheckDistance: false);
				foreach (SpaceObjectVessel allDockedVessel3 in spaceObjectVessel.AllDockedVessels)
				{
					ZeroOcclusion.CheckOcclusionFor(allDockedVessel3, onlyCheckDistance: false);
				}
			}

			if (spaceObjectVessel2 != null)
			{
				ZeroOcclusion.CheckOcclusionFor(spaceObjectVessel2, onlyCheckDistance: false);
				foreach (SpaceObjectVessel allDockedVessel4 in spaceObjectVessel2.AllDockedVessels)
				{
					ZeroOcclusion.CheckOcclusionFor(allDockedVessel4, onlyCheckDistance: false);
				}
			}

		}

		/// <summary>
		/// 	Leaves the current vessel onto a freshly created pivot.
		/// </summary>
		public override void ExitVessel(bool forceExit)
		{
			if (Parent is not SpaceObjectVessel vesselWeAreExiting)
			{
				return;
			}

			SceneQuestTrigger.OnTriggerInChildren(vesselWeAreExiting.GeometryRoot,
				SceneQuestTriggerEvent.ExitVessel);
			SceneQuestTrigger.OnTriggerInChildren(vesselWeAreExiting.MainVessel.gameObject,
				SceneQuestTriggerEvent.ExitStation);

			if (vesselWeAreExiting is Ship)
			{
				if (CurrentHelmet != null)
				{
					CurrentHelmet.HudUI.Radar.CanRadarWork = true;
					CurrentHelmet.HudUI.Radar.ToggleTargeting(val: true);
				}

				Instance.FpsController.CameraController.cameraShakeController.Stop();
			}

			SpaceObjectVessel mainVessel = vesselWeAreExiting.MainVessel;
			Pivot pivot = Pivot.Create(SpaceObjectType.PlayerPivot, Guid, mainVessel, isMainObject: true);

			Parent = pivot;

			World.SolarSystem.CenterPlanets();
			UpdateCameraPositions();
			World.CubemapRenderer.RenderCubemapForReflectionProbe();
			World.CubemapRenderer.RenderCubemapForDockingPort();

			foreach (SpaceObjectVessel vessel in World.AllArtificialBodies.OfType<SpaceObjectVessel>())
			{
				vessel.ToggleOptimization(optimizationEnabled: true);
				vessel.UpdateArtificialBodyPosition(updateChildren: false);
			}

			if (_isRagdolled)
			{
				ToggleRagdoll(false);
			}

			ZeroOcclusion.CheckOcclusionFor(mainVessel, onlyCheckDistance: false);
			foreach (SpaceObjectVessel dockedVessel in mainVessel.AllDockedVessels)
			{
				ZeroOcclusion.CheckOcclusionFor(dockedVessel, onlyCheckDistance: false);
			}
		}

		public void ProcessMovementMessage(long parentGuid, Vector3? position, Quaternion? rotation, Vector3? linearVelocity,
			CharacterAnimationData? animationData)
		{
			if (animationData.HasValue)
			{
				animHelper.ParseData(animationData.Value);
			}

			if (parentGuid != Parent.Guid)
			{
				return;
			}

			Transform frame = Parent.TransferableObjectsRoot.transform;
			if (position.HasValue)
			{
				transform.position = frame.TransformPoint(position.Value);
			}

			if (rotation.HasValue)
			{
				transform.rotation = frame.rotation * rotation.Value;
			}

			if (rigidBody.isKinematic)
			{
				return;
			}

			if (position.HasValue)
			{
				rigidBody.position = transform.position;
			}

			if (rotation.HasValue)
			{
				rigidBody.rotation = transform.rotation;
			}

			if (linearVelocity.HasValue)
			{
				rigidBody.linearVelocity = frame.TransformDirection(linearVelocity.Value);
			}
		}

		private void LockToTrigger(BaseSceneTrigger trigger)
		{
			LockedToTrigger = trigger;
			if (FpsController.CurrentJetpack != null)
			{
				_wasJetpackOn = FpsController.CurrentJetpack.IsActive;
				FpsController.CurrentJetpack.IsActive = false;
			}
		}

		public void DetachFromPanel()
		{
			World.InWorldPanels.Detach();
			FpsController.ResetVelocity();
			FpsController.ToggleAttached(false);
			FpsController.ToggleCameraMovement(true);
			ChangeCamerasFov(Globals.Instance.DefaultCameraFov);
			FpsController.ToggleMovement(!SittingOnPilotSeat);
			if (SittingOnPilotSeat)
			{
				FpsController.ToggleCameraAttachToHeadBone(true);
			}

			FpsController.ToggleAutoFreeLook(SittingOnPilotSeat);
			LockedToTrigger = null;
			Globals.ToggleCursor(false);
			World.InGameGUI.OverlayCanvasIsOn = false;
			ResetMyRoomTrigger();
			if (FpsController.CurrentJetpack != null)
			{
				FpsController.CurrentJetpack.IsActive = _wasJetpackOn;
			}
		}

		public void ResetMyRoomTrigger()
		{
			if (MyRoomTrigger.gameObject.activeInHierarchy)
			{
				MyRoomTrigger.transform.localPosition = new Vector3(0f, -0.3f, 0f);
				EnableTransitionTrigger();
			}
		}

		public void AttachToPanel(BaseSceneTrigger trigger, bool useCursor = true)
		{
			World.InGameGUI.HelmetOverlayModel.SetAxis(0f, 0f, 0f);
			World.InGameGUI.HelmetOverlayModel.SetMovement(0f, 0f);
			World.InWorldPanels.Interact();
			FpsController.ResetVelocity();
			FpsController.ToggleAttached(true);
			FpsController.ToggleMovement(false);
			FpsController.ToggleCameraMovement(false);
			LockToTrigger(trigger);
			Globals.ToggleCursor(useCursor);
			World.InGameGUI.OverlayCanvasIsOn = true;
			if (!(Parent is SpaceObjectVessel))
			{
				SpaceObjectVessel spaceObjectVessel =
					trigger.GetComponentInParent<GeometryRoot>().MainObject as SpaceObjectVessel;
				if (spaceObjectVessel != null)
				{
					SetMyRoomTrigger(spaceObjectVessel);
				}
			}
		}

		public void SetMyRoomTrigger(SpaceObjectVessel vessel)
		{
			MyRoomTrigger.ParentVessel = vessel;
			MyRoomTrigger.transform.localPosition = Vector3.zero;
		}

		public void ToggleRagdoll(bool? enabled = null)
		{
			if (enabled.HasValue)
			{
				_isRagdolled = enabled.Value;
			}
			else
			{
				_isRagdolled = !_isRagdolled;
			}

			if (_isRagdolled)
			{
				_wasInStance = animHelper.GetParameterBool(AnimatorHelper.Parameter.InStance);
				animHelper.SetParameter(null, null, null, null, null, null, null, null, false);
				_isRagdollFinished = false;
				FpsController.ToggleMovement(false);
				Vector3 velocity = rigidBody.linearVelocity;
				FpsController.ToggleColliders(isEnabled: false);
				rigidBody.isKinematic = true;
				ragdollComponent.ToggleRagdoll(enabled: true, this, velocity);
				FpsController.ResetVelocity();
				FpsController.ToggleCameraAttachToHeadBone(true);
				return;
			}

			FpsController.ReparentCenterOfMass(isInChest: false);
			ragdollComponent.ToggleRagdoll(enabled: false, null);
			Transform bone = animHelper.GetBone(AnimatorHelper.HumanBones.Hips);
			RaycastHit hitInfo;
			if (FpsController.IsZeroG)
			{
				FpsController.transform.position = bone.position;
			}
			else if (Physics.Raycast(bone.position, GravityDirection, out hitInfo, 2f,
						 World.DefaultLayerMask))
			{
				FpsController.transform.position = hitInfo.point - GravityDirection * 1.34f;
			}

			Quaternion rotation = FpsController.transform.rotation;
			if (Vector3.Dot(bone.up, GravityDirection) >= 0f)
			{
				FpsController.transform.rotation =
					Quaternion.LookRotation(Vector3.ProjectOnPlane(bone.right, -GravityDirection),
						-GravityDirection);
			}
			else
			{
				FpsController.transform.rotation =
					Quaternion.LookRotation(Vector3.ProjectOnPlane(-bone.right, -GravityDirection),
						-GravityDirection);
			}

			ragdollComponent.AddRootRotation(rotation * FpsController.transform.rotation.Inverse());
			ragdollComponent.RestorePositions();
			AnimatorHelper animatorHelper = animHelper;
			float? getUpType = FpsController.CheckGetUpRoom();
			animatorHelper.SetParameter(null, null, null, null, null, null, null, null, null, null, null, null, null,
				null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
				null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
				null, getUpType);
			FpsController.ToggleColliders(isEnabled: true);
			rigidBody.isKinematic = false;
			FpsController.ReparentCenterOfMass(isInChest: true);
		}

		public void RagdollFinished()
		{
			_isRagdollFinished = true;
			FpsController.ToggleCameraAttachToHeadBone(false);
			FpsController.LerpCameraBack(3f);
			FpsController.ToggleMovement(true);
			FpsController.ToggleCameraMovement(true);
			AnimatorHelper animatorHelper = animHelper;
			bool? inStance = _wasInStance;
			animatorHelper.SetParameter(null, null, null, null, null, null, null, null, inStance);
			_wasInStance = false;
		}

		public void ChangeCamerasFov(float fovVal)
		{
			if (fovVal > 0f)
			{
				_cameraFovLerpFrom = fovVal;
				_cameraFovLerpTo = FpsController.MainCamera.fieldOfView;
				_cameraFovLerpValue = 1f;
				World.InGameGUI.HelmetHud.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);
				if (fovVal == Globals.Instance.DefaultCameraFov)
				{
					World.InGameGUI.HelmetHud.transform.localScale = new Vector3(1f, 1f, 1f);
					ControlsSubsystem.RealSensitivity = Settings.SettingsData.ControlsSettings.MouseSensitivity;
				}
				else
				{
					// Reduce the sensitivity if the fov is reduced.
					ControlsSubsystem.RealSensitivity = Settings.SettingsData.ControlsSettings.MouseSensitivity *
												   (fovVal / 100f);
				}
			}
		}

		public void ResetCameraFov()
		{
			if (!FpsController.MainCamera.fieldOfView.IsEpsilonEqual(Globals.Instance.DefaultCameraFov))
			{
				ChangeCamerasFov(Globals.Instance.DefaultCameraFov);
			}
		}

		private void LerpCameraFov()
		{
			_cameraFovLerpValue -= Time.deltaTime * CameraFovLerpStrength;
			if (_cameraFovLerpValue <= 0.001f)
			{
				_cameraFovLerpValue = 0f;
			}

			float fieldOfView = Mathf.Lerp(_cameraFovLerpFrom, _cameraFovLerpTo, _cameraFovLerpValue);
			World.SunCameraTransform.GetComponent<Camera>().fieldOfView = fieldOfView;
			World.PlanetsCameraTransform.GetComponent<Camera>().fieldOfView = fieldOfView;
			FpsController.MainCamera.fieldOfView = fieldOfView;
			FpsController.FarCamera.fieldOfView = fieldOfView;
			FpsController.NearCamera.fieldOfView = fieldOfView;
		}

		public void ChangeLockedToTrigger(BaseSceneTrigger trigger)
		{
			LockedToTrigger = trigger;
		}

		private void UpdateReferenceHead()
		{
			Transform[] array = new Transform[ReferenceHead.bones.Length];
			Transform bone = animHelper.GetBone(AnimatorHelper.HumanBones.Spine2);
			for (int i = 0; i < 6; i++)
			{
				array[i] = bone.FindChildByName(ReferenceHead.bones[i].name);
			}

			ReferenceHead.bones = array;
		}

		private void RefreshHeadBones()
		{
			Transform[] array = new Transform[ReferenceHead.bones.Length];
			Transform bone = animHelper.GetBone(AnimatorHelper.HumanBones.Spine2);
			for (int i = 0; i < HeadSkin.bones.Length; i++)
			{
				array[i] = bone.FindChildByName(ReferenceHead.bones[i].name);
			}

			HeadSkin.bones = array;
		}

		public void RefreshOutfitData()
		{
			animHelper.RebindAndReload();
			ragdollComponent.RefreshRagdollVariables();
			RefreshHeadBones();
			FpsController.RefreshOutfitData(Outfit);
		}

		private Dictionary<byte, RagdollItemData> GetRagdollData()
		{
			Dictionary<byte, RagdollItemData> dictionary = new Dictionary<byte, RagdollItemData>();
			foreach (KeyValuePair<AnimatorHelper.HumanBones, Transform> bone in animHelper.GetBones())
			{
				dictionary.Add((byte)bone.Key, new RagdollItemData
				{
					Position = bone.Value.localPosition.ToArray(),
					LocalRotation = bone.Value.localRotation.ToArray()
				});
			}

			return dictionary;
		}

		public void OnTriggerEnter(Collider coli)
		{
			OcSector component = coli.GetComponent<OcSector>();
			if (component != null)
			{
				if (_currOcSector == null)
				{
					component.ToggleVisibility(true);
				}

				component.ToggleNeighbours(isVisible: true);
				_currOcSector = component;
			}
		}

		public void OnTriggerExit(Collider coli)
		{
			OcSector component = coli.GetComponent<OcSector>();
			if (component != null)
			{
				if (component == _currOcSector)
				{
					_currOcSector = null;
				}
				else
				{
					component.ToggleNeighbours(isVisible: false, _currOcSector);
				}
			}
		}

		public void AnimInteraction_LockEnter()
		{
			InIteractLayer = true;
			InLockState = true;
			if (World.InGameGUI.IsPlayerOverviewOpen)
			{
				World.InGameGUI.PlayerOverview.Toggle(val: false);
			}

			if (OnLockStart != null)
			{
				OnLockStart();
			}
		}

		public void AnimInteraction_LockExit()
		{
			InLockState = false;
			if (OnLockComplete != null)
			{
				OnLockComplete();
			}
		}

		public void AnimInteraction_InteractEnter()
		{
			InIteractLayer = true;
			InInteractState = true;
			if (World.InGameGUI.IsPlayerOverviewOpen)
			{
				World.InGameGUI.PlayerOverview.Toggle(val: false);
			}

			if (OnIteractStart != null)
			{
				OnIteractStart();
			}
		}

		public void AnimInteraction_InteractExit()
		{
			InInteractState = false;
			if (OnIteractComplete != null)
			{
				OnIteractComplete();
			}
		}

		public void AnimInteraction_NoneEnter()
		{
			InIteractLayer = false;
		}

		public void AnimInteraction_NoneExit()
		{
		}

		public void ResetLookingAtItem()
		{
			LookingAtItem = null;
		}

		public void ToggleMeshRendereres(bool enableMesh)
		{
			MeshRenderersEnabled = enableMesh;
			SkinnedMeshRenderer[] componentsInChildren = Outfit.GetComponentsInChildren<SkinnedMeshRenderer>();
			foreach (SkinnedMeshRenderer skinnedMeshRenderer in componentsInChildren)
			{
				if (!ArmSkins.Contains(skinnedMeshRenderer) &&
					(CurrentOutfit == null || !CurrentOutfit.ArmSkins.Contains(skinnedMeshRenderer)))
				{
					skinnedMeshRenderer.shadowCastingMode =
						enableMesh ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly;
				}
			}
		}

		public override void DockedVesselParentChanged(SpaceObjectVessel vessel)
		{
			SpaceObjectVessel spaceObjectVessel = Parent as SpaceObjectVessel;
			Parent = vessel;
			SceneQuestTrigger.OnTriggerInChildren(vessel.GeometryRoot, SceneQuestTriggerEvent.EnterVessel);
			if (spaceObjectVessel != null)
			{
				ZeroOcclusion.CheckOcclusionFor(spaceObjectVessel, onlyCheckDistance: false);
				spaceObjectVessel.ToggleOptimization(optimizationEnabled: true);
			}

			if (vessel != null)
			{
				ZeroOcclusion.CheckOcclusionFor(vessel, onlyCheckDistance: false);
				vessel.ToggleOptimization(optimizationEnabled: false);
			}
		}

		public override void OnGravityChanged(Vector3 oldGravity)
		{
			FpsController.SetGravity(Gravity);
			InventoryCharacterPreview.Instance.ChangeGravity(FpsController.IsZeroG);
		}

		public override void RoomChanged(SceneTriggerRoom prevRoomTrigger)
		{
			base.RoomChanged(prevRoomTrigger);

			SendPlayerRoomMessage();

			if (CurrentRoomTrigger != null)
			{
				CurrentRoomTrigger.ExecuteBehaviourScripts();
			}
		}

		public void Suicide()
		{
			NetworkController.SendAndForget(new SuicideRequest());
		}

		public void CheckCameraShake()
		{
			if (!(Parent is Ship))
			{
				FpsController.CameraController.cameraShakeController.Stop();
				return;
			}

			Ship ship = Parent as Ship;
			if (ship.IsWarpOnline ||
				ship.AllDockedVessels.Find((SpaceObjectVessel m) => m as Ship != null && (m as Ship).IsWarpOnline) !=
				null)
			{
				FpsController.CameraController.cameraShakeController.ShakeCamera(CameraShake.ShakeType.Warp,
					infiniteShake: true);
			}
			else if ((ship.Engine != null && ship.Engine.Status == SystemStatus.Online) ||
					 ship.AllDockedVessels.Find((SpaceObjectVessel m) =>
						 m as Ship != null && (m as Ship).Engine != null &&
						 (m as Ship).Engine.Status == SystemStatus.Online) != null)
			{
				FpsController.CameraController.cameraShakeController.TargetMainMultiplier = ship.Engine.OperationRate;
				FpsController.CameraController.cameraShakeController.ShakeCamera(CameraShake.ShakeType.Engine,
					infiniteShake: true);
			}
			else
			{
				FpsController.CameraController.cameraShakeController.Stop();
			}
		}

		public void StartExitCryoChamberCountdown(SceneTriggerExecutor cryoExecutor, string stateName)
		{
			StartCoroutine(WaitFromCryoChamberExit(cryoExecutor, stateName));
		}

		private IEnumerator WaitFromCryoChamberExit(SceneTriggerExecutor cryoExecutor, string stateName)
		{
			yield return new WaitForSeconds(12.12f);
			if (InIteractLayer)
			{
				cryoExecutor.ChangeStateImmediate(stateName);
				cryoExecutor.SetExecutorDetails(new SceneTriggerExecutorDetails
				{
					PlayerThatActivated = Instance.Guid,
					InSceneID = cryoExecutor.InSceneID,
					IsImmediate = true,
					IsFail = false,
					CurrentStateID = cryoExecutor.CurrentStateID,
					NewStateID = cryoExecutor.GetStateID(stateName)
				}, isInstant: false, checkCurrentState: false);
				cryoExecutor.CharacterUnlock();
			}
		}

		public float? GetDistance(ISoundOccludable otherObject, out bool throughBulkhead)
		{
			throughBulkhead = false;
			if (otherObject is SpaceObjectTransferable)
			{
				return GetDistance(otherObject as SpaceObjectTransferable, out throughBulkhead);
			}

			if (otherObject is VesselSystem)
			{
				return GetDistance(otherObject as VesselSystem, out throughBulkhead);
			}

			if (otherObject is SceneDoor)
			{
				return GetDistance(otherObject as SceneDoor, out throughBulkhead);
			}

			return null;
		}

		public float? GetDistance(SpaceObjectTransferable otherObject, out bool throughBulkhead)
		{
			if (otherObject.Parent is Player)
			{
				return GetDistance(otherObject.Parent.transform.position,
					(otherObject.Parent as Player).CurrentRoomTrigger, out throughBulkhead);
			}

			return GetDistance(otherObject.transform.position, otherObject.CurrentRoomTrigger, out throughBulkhead);
		}

		public float? GetDistance(VesselSystem vesselComponent, out bool throughBulkhead)
		{
			return GetDistance(vesselComponent.transform.position, vesselComponent.Room, out throughBulkhead);
		}

		public float? GetDistance(SceneDoor door, out bool throughBulkhead)
		{
			return GetDistance(door.DoorPassageTrigger.bounds.center, !(door.Room1 != null) ? door.Room2 : door.Room1,
				out throughBulkhead, door);
		}

		public float? GetDistance(Vector3 position, SceneTriggerRoom room, out bool throughBulkhead,
			SceneDoor ignoreDoor = null)
		{
			throughBulkhead = false;
			if (!IsInsideSpaceObject || room == null || CurrentRoomTrigger == null)
			{
				return null;
			}

			if (CurrentRoomTrigger == room || (ignoreDoor != null && ignoreDoor.Room1 == room) ||
				(ignoreDoor != null && ignoreDoor.Room2 == room))
			{
				return (transform.position - position).magnitude;
			}

			if (CurrentRoomTrigger.CompoundRoomID == room.CompoundRoomID ||
				(ignoreDoor != null && ignoreDoor.Room1 != null &&
				 ignoreDoor.Room1.CompoundRoomID == CurrentRoomTrigger.CompoundRoomID) || (ignoreDoor != null &&
					ignoreDoor.Room2 != null &&
					ignoreDoor.Room2.CompoundRoomID == CurrentRoomTrigger.CompoundRoomID))
			{
				SceneTriggerRoom currentRoomTrigger = CurrentRoomTrigger;
				List<SceneTriggerRoom> list = FindPath(currentRoomTrigger, room, null, ignoreDoor);
				if (list != null)
				{
					List<Vector3> list2 = new List<Vector3>();
					SceneTriggerRoom sceneTriggerRoom = list[0];
					list.RemoveAt(0);
					foreach (SceneTriggerRoom item in list)
					{
						foreach (SceneDoor item2 in sceneTriggerRoom.Doors.FindAll((SceneDoor m) =>
									 m.IsOpen || !m.IsSealable))
						{
							if ((item2.Room1 == sceneTriggerRoom && item2.Room2 == item) ||
								(item2.Room2 == sceneTriggerRoom && item2.Room1 == item))
							{
								list2.Add(item2.DoorPassageTrigger.bounds.center);
								break;
							}
						}

						if (sceneTriggerRoom.ParentVessel != item.ParentVessel)
						{
							foreach (SceneDoor item3 in item.Doors.FindAll((SceneDoor m) => m.IsOpen || !m.IsSealable))
							{
								if ((item3.Room1 == sceneTriggerRoom && item3.Room2 == item) ||
									(item3.Room2 == sceneTriggerRoom && item3.Room1 == item))
								{
									list2.Add(item3.DoorPassageTrigger.bounds.center);
									break;
								}
							}
						}

						sceneTriggerRoom = item;
					}

					list2.Add(position);
					float num = 0f;
					Vector3 vector = transform.position;
					foreach (Vector3 item4 in list2)
					{
						num += (vector - item4).magnitude;
						vector = item4;
					}

					return num;
				}
			}

			throughBulkhead = true;
			return (transform.position - position).magnitude;
		}

		private List<SceneTriggerRoom> FindPath(SceneTriggerRoom current, SceneTriggerRoom target,
			HashSet<SceneTriggerRoom> traversedRooms = null, SceneDoor ignoreDoor = null)
		{
			if (traversedRooms == null)
			{
				traversedRooms = new HashSet<SceneTriggerRoom>();
			}

			if (current == target)
			{
				List<SceneTriggerRoom> list = new List<SceneTriggerRoom>();
				list.Add(target);
				return list;
			}

			if (current == null || !traversedRooms.Add(current))
			{
				return null;
			}

			List<SceneDoor> list2 =
				current.Doors.FindAll((SceneDoor m) => m.IsOpen || !m.IsSealable || m == ignoreDoor);
			foreach (SceneDoor item in list2)
			{
				List<SceneTriggerRoom> list3 = FindPath(!(item.Room1 == current) ? item.Room1 : item.Room2, target,
					traversedRooms);
				if (list3 != null)
				{
					list3.Insert(0, current);
					return list3;
				}
			}

			return null;
		}

		public void EnableTransitionTrigger()
		{
			TransitionTrigger.Enabled = true;
		}

		public void DisableTransitionTrigger()
		{
			TransitionTrigger.Enabled = false;
		}

		public bool IsInVesselHierarchy(SpaceObjectVessel vessel)
		{
			if (vessel == null || !(Parent is SpaceObjectVessel))
			{
				return false;
			}

			return Parent == vessel || vessel.AllVessels.Contains(Parent as SpaceObjectVessel);
		}

		public void HighlightAttachPoints(ItemType? item = null, GenericItemSubType? generic = null,
			MachineryPartType? part = null, int? partTier = null)
		{
			Collider[] array = Physics.OverlapSphere(transform.position, HighlightAttachPointsRange,
				_highlightAttachPointMask, QueryTriggerInteraction.Collide);
			foreach (Collider collider in array)
			{
				BaseSceneAttachPoint baseSceneAttachPoint = collider.GetComponentInParent<BaseSceneAttachPoint>();
				if (baseSceneAttachPoint != null && baseSceneAttachPoint.Collider == collider && (!item.HasValue ||
						baseSceneAttachPoint.CanAttachItemType(item.Value, generic, part, partTier)))
				{
					if (!item.HasValue)
					{
						World.InGameGUI.HighlightSlotMaterial.mainTexture =
							World.InGameGUI.HighlightSlotNormal;
					}
					else
					{
						World.InGameGUI.HighlightSlotMaterial.mainTexture =
							World.InGameGUI.HighlightSlotItemHere;
					}
				}
			}
		}

		public void HideHighlightedAttachPoints()
		{
			foreach (GameObject higlightedAttachPoint in _higlightedAttachPoints)
			{
				higlightedAttachPoint.SetActive(value: false);
			}

			_higlightedAttachPoints.Clear();
		}

		public void MeleeAttack()
		{
			SpaceObject parent = Instance.Parent is not SpaceObjectVessel
				? Instance.Parent
				: (Instance.Parent as SpaceObjectVessel).MainVessel;
			ShotData shotData = new ShotData
			{
				Position = (parent.transform.rotation *
								 Instance.FpsController.MainCamera.transform.position).ToArray(),
				Orientation = (parent.transform.rotation *
									Instance.FpsController.MainCamera.transform.forward.normalized).ToArray(),
				parentGUID = parent.Guid,
				parentType = parent.Type,
				Range = MeleeRange,
				IsMeleeAttack = true
			};
			Attack(shotData, CurrentActiveItem, 0f, 0f);
		}

		private IEnumerator DebrisFieldCheckCoroutine()
		{
			while (true)
			{
				if (PlayerReady && Parent != null && Parent is ArtificialBody)
				{
					DebrisField inDebrisField = null;
					Vector3D orbitVelocity = Vector3D.Zero;
					foreach (DebrisField debrisField in World.DebrisFields)
					{
						if (debrisField.CheckObject(Parent as ArtificialBody, out orbitVelocity))
						{
							inDebrisField = debrisField;
							World.DebrisEffect.UpdateEffect(playStop: true,
								orbitVelocity.ToVector3(),
								debrisField.FragmentsDensity, debrisField.FragmentsVelocity);
							break;
						}

						World.DebrisEffect.UpdateEffect(playStop: false, Vector3.zero, -1f, -1f);
					}

					DebrisField inDebrisField2 = InDebrisField;
					InDebrisField = inDebrisField;
					DebrisFieldVelocityDirection = orbitVelocity;
					if (inDebrisField2 != InDebrisField)
					{
						World.InGameGUI.HelmetHud.WarningsUpdate();
					}
				}

				yield return new WaitForSeconds(1f);
			}
		}

		private void OnCollisionEnter(Collision collision)
		{
			Corpse componentInParent = collision.collider.GetComponentInParent<Corpse>();
			if (componentInParent != null)
			{
				componentInParent.PushedByMyPlayer(collision.relativeVelocity);
			}
		}

		private void LockToTriggerMessageListener(NetworkData data)
		{
			LockToTriggerMessage ltm = data as LockToTriggerMessage;
			if (ltm.TriggerID == null || (!(LockedToTrigger == null) && LockedToTrigger.GetID().Equals(ltm.TriggerID)))
			{
				return;
			}

			if (LookingAtTrigger == null || !LookingAtTrigger.GetID().Equals(ltm.TriggerID))
			{
				SpaceObjectVessel vessel = World.GetVessel(ltm.TriggerID.VesselGUID);
				if (vessel != null)
				{
					LookingAtTrigger = vessel.GeometryRoot.GetComponentsInChildren<BaseSceneTrigger>()
						.FirstOrDefault((BaseSceneTrigger m) => m.GetID().InSceneID == ltm.TriggerID.InSceneID);
				}
			}

			if (LookingAtTrigger != null && !LookingAtTrigger.Interact(this))
			{
				World.InGameGUI.Alert(Localization.UnauthorizedAccess.ToUpper());
			}
		}

		private void QuestStatsMessageListener(NetworkData data)
		{
			QuestStatsMessage questStatsMessage = data as QuestStatsMessage;
			SetQuestDetails(questStatsMessage.QuestDetails);
		}

		private void UpdateBlueprintsMessageListener(NetworkData data)
		{
			UpdateBlueprintsMessage updateBlueprintsMessage = data as UpdateBlueprintsMessage;
			if (updateBlueprintsMessage.Blueprints != null)
			{
				Blueprints = updateBlueprintsMessage.Blueprints;
			}
		}

		public void SetQuestDetails(QuestDetails details, bool showNotifications = true, bool playCutScenes = true)
		{
			Quest quest = World.Quests.FirstOrDefault((Quest m) => m.ID == details.ID);
			if (quest == null)
			{
				return;
			}

			quest.SetDetails(details, World, showNotifications, playCutScenes);
			if (quest.Status != 0)
			{
				QuestUI questUI =
					World.InGameGUI.PlayerOverview.Quests.AllQuests.FirstOrDefault((QuestUI m) =>
						m.Quest == quest);
				if (questUI == null)
				{
					questUI = World.InGameGUI.PlayerOverview.Quests.CreateQuestUI(quest);
				}

				questUI.RefreshQuestUI();
			}

			if (World.InGameGUI.PlayerOverview.Quests.gameObject.activeInHierarchy)
			{
				World.InGameGUI.PlayerOverview.Quests.RefreshSelectedQuest();
			}
		}

		public void CheckEquipmentAchievement()
		{
			if (!(CurrentOutfit == null) && !(CurrentHelmet == null) && !(FpsController.CurrentJetpack == null) &&
				!IsAdmin && CurrentOutfit.Type == ItemType.SoePressurisedSuit &&
				CurrentHelmet.Type == ItemType.SoePressurisedHelmet &&
				FpsController.CurrentJetpack.Type == ItemType.SoePressurisedJetpack)
			{
				RichPresenceManager.SetAchievement(AchievementID.collection_full_soe_outfit);
			}
		}

		public void OnApplicationFocus(bool focusStatus)
		{
			if (InLockState && !IsLockedToTrigger &&
				Parent is SpaceObjectVessel &&
				(Parent as SpaceObjectVessel).SpawnPoints.Values.FirstOrDefault(
					(SceneSpawnPoint m) => m.PlayerGUID == Guid) != null)
			{
				FpsController.CameraController.ToggleFreeLook(isActive: true);
			}

			HideHighlightedAttachPoints();
		}
	}
}
