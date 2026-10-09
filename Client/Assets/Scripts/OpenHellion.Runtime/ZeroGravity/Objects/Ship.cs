using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using OpenHellion;
using OpenHellion.Net;
using OpenHellion.Net.Message;
using OpenHellion.Social.RichPresence;
using OpenHellion.UI;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;
using ZeroGravity.Data;
using ZeroGravity.Effects;
using ZeroGravity.LevelDesign;
using ZeroGravity.Math;
using ZeroGravity.Network;
using ZeroGravity.ShipComponents;
using ZeroGravity.UI;

namespace ZeroGravity.Objects
{
	// TODO test if vessel data is updated when needed
	public class Ship : SpaceObjectVessel
	{
		public delegate void DockCompletedDelegate(bool isInitialize);

		private bool _shipStatsChanged;

		private ShipStatsMessage _shipStatsMsg;

		public float EngineThrustPercentage;

		public double EndWarpTime;

		public Vector3 AutoStabilize = Vector3.zero;

		[NonSerialized] public float[] ResDebug;

		public Vector3 CurrRcsMoveThrust;

		public Vector3 CurrRcsRotationThrust;

		private Vector3 _inputThrust;

		private Vector3 _inputRotation;

		private Vector3? _inputAutoStabilise;

		private uint _inputSequence;

		private Vector3 _sentThrust;

		private Vector3 _sentRotation;

		private float _sentEngineThrustPercentage;

		[FormerlySerializedAs("gatherAtmos")] public bool GatherAtmos;

		public bool IsRotationStabilized;

		public NavigationPanel NavPanel;

		private EngineThrusters _engineThrusters;

		private WarpEffect _warpEffect;

		private WarpStartEffect _warpStartEffect;

		public Action WarpStartEffectTask;

		private WarpEndEffect _warpEndEffect;

		public Action WarpEndEffectTask;

		private WarpInductorExecutor _warpInductorExecutor;

		private RCSThrusters _rcsThrusters;

		private RefuelingStationUI _refuelingStation;

		private Queue<VesselObjects> _shipObjectsLoadingQueue;

		public DockCompletedDelegate OnDockCompleted;

		public DockCompletedDelegate OnUndockCompleted;

		public DockCompletedDelegate OnUndockStarted;

		private SceneDockingPortDetails _currDockDetails;

		private float _lerpTimer;

		public override SpaceObjectType Type => SpaceObjectType.Ship;

		public long CourseWaitingActivation { get; private set; }

		public Vector3 CourseStartDirection { get; private set; }

		public double CourseStartTime { get; private set; }

		public double CourseEndTime { get; private set; }

		public SecuritySystem SecuritySystem { get; private set; }

		public float[] CollidersCenterOffset { get; private set; }

		public bool IsThrusting => _inputThrust.IsNotEpsilonZero() || _inputRotation.IsNotEpsilonZero();

		public static async UniTask<Ship> Create(World world, long guid, Vector3 position, Quaternion rotation, string vesselRegistration, string vesselName, string tag, GameScenes.SceneId sceneId,
			float[] collidersCenterOffset, bool isDebrisFragment, double radarSignature, bool isDistressSignalActive, bool isAlwaysVisible,
			bool dockingControlsDisabled, bool securityPanelLocked, VesselObjects vesselObjects, DockedVesselData[] dockedVessels, bool isMainObject)
		{
			Ship ship = InitialiseArtificialBody(world, guid, SpaceObjectType.Ship, position, rotation) as Ship;
			ship.VesselRegistration = vesselRegistration;
			ship.VesselName = vesselName;
			ship.Tag = tag;
			ship.SceneId = sceneId;
			ship.CollidersCenterOffset = collidersCenterOffset;
			ship.IsDebrisFragment = isDebrisFragment;
			ship.RadarSignature = radarSignature;
			ship.IsDistressSignalActive = isDistressSignalActive;
			ship.IsAlwaysVisible = isAlwaysVisible;
			ship.DockingControlsDisabled = dockingControlsDisabled;
			ship.SecurityPanelsLocked = securityPanelLocked;
			ship.gameObject.SetActive(true);
			await ship.LoadAllShipScenesAsync(vesselObjects, dockedVessels, !isMainObject);
			return ship;
		}

		private void Start()
		{
			ConnectMessageListeners();
			this.InvokeRepeating(ProximityCanvasCheck, 1f, MathHelper.RandomRange(1f, 1.5f));
		}

		private void Update()
		{
			if (MyPlayer.Instance.Parent != null)
			{
				UpdatePositionAndRotation(!IsMainObject && DockedToMainVessel == null);
			}
		}

		protected override void FixedUpdate()
		{
			base.FixedUpdate();
			if (AutoStabilize.IsNotEpsilonZero())
			{
				_inputAutoStabilise = AngularVelocity != Vector3.zero ? AutoStabilize : Vector3.one;
				if (AngularVelocity == Vector3.zero)
				{
					AutoStabilize = Vector3.zero;
				}
			}

			if (MyPlayer.Instance.Parent == this && MyPlayer.Instance.IsDrivingShip)
			{
				Vector3 thrust = _inputThrust.sqrMagnitude > 1f ? _inputThrust.normalized : _inputThrust;
				Vector3 rotation = _inputRotation.sqrMagnitude > 1f ? _inputRotation.normalized : _inputRotation;

				if (thrust.IsNotEpsilonZero() || rotation.IsNotEpsilonZero() ||
					thrust != _sentThrust || rotation != _sentRotation ||
					!Mathf.Approximately(EngineThrustPercentage, _sentEngineThrustPercentage) ||
					_inputAutoStabilise.HasValue)
				{
					NetworkController.SendAndForget(new ShipThrustMessage
					{
						VesselGuid = Guid,
						Sequence = ++_inputSequence,
						Thrust = thrust.ToArray(),
						Rotation = rotation.ToArray(),
						EngineThrustPercentage = EngineThrustPercentage,
						AutoStabilise = _inputAutoStabilise.HasValue ? _inputAutoStabilise.Value.ToArray() : null
					});

					_sentThrust = thrust;
					_sentRotation = rotation;
					_sentEngineThrustPercentage = EngineThrustPercentage;
				}

				World.PilotedVesselGuid = Guid;
				if (RCS != null)
				{
					double compoundMass = MainVessel.GetCompoundMass();
					float massRatio = compoundMass > 0.0 ? (float)(Mass / compoundMass) : 1f;
					float rate = RCS.MaxOperationRate * massRatio * Time.fixedDeltaTime;

					World.AnchorThrustVelocity += thrust * (RCS.Acceleration * rate);
					SetVelocity(Velocity, AngularVelocity + rotation * (RCS.RotationAcceleration * Mathf.Deg2Rad * rate));
				}

				World.AnchorOffset += World.AnchorThrustVelocity * Time.fixedDeltaTime;

				// The pilot knows their own thrust, so the thruster effects need no round trip.
				CurrRcsMoveThrust = RCS == null ? Vector3.zero : RCS.Acceleration * RCS.MaxOperationRate * thrust;
				CurrRcsRotationThrust = RCS == null ? Vector3.zero : RCS.RotationAcceleration * RCS.MaxOperationRate * rotation;
				UpdateRcsThrusterEffects();

				_inputAutoStabilise = null;
			}
			else if (World.PilotedVesselGuid == Guid)
			{
				// Leaving the seat stops the ship control messages, so the prediction has to be released here or
				// this vessel would keep spinning on with nothing left to correct it.
				World.PilotedVesselGuid = 0;
				World.AnchorOffset = Vector3.zero;
				World.AnchorThrustVelocity = Vector3.zero;
			}

			if (_shipStatsChanged)
			{
				NetworkController.SendAndForget(_shipStatsMsg);
				_shipStatsMsg = new ShipStatsMessage
				{
					Guid = Guid,
					VesselObjects = new VesselObjects
					{
						SubSystems = new List<SubSystemDetails>(),
						Generators = new List<GeneratorDetails>(),
						RoomTriggers = new List<RoomDetails>(),
						Doors = new List<DoorDetails>(),
						SceneTriggerExecutors = new List<SceneTriggerExecutorDetails>(),
						DockingPorts = new List<SceneDockingPortDetails>(),
						AttachPoints = new List<AttachPointDetails>(),
						SpawnPoints = new List<SpawnPointStats>()
					}
				};
				_shipStatsChanged = false;
			}
		}

		/// <summary>
		/// 	Update our ship's thrust and rotation to be sent to the server for re-simulation.
		/// </summary>
		public void SetPilotInput(Vector3 thrust, Vector3 rotation)
		{
			_inputThrust = thrust;
			_inputRotation = rotation;
		}

		private void UpdateRcsThrusterEffects()
		{
			if (_rcsThrusters == null)
			{
				return;
			}

			bool active = CurrRcsMoveThrust.magnitude > 0f || CurrRcsRotationThrust.magnitude > 0f;
			_rcsThrusters.SetMoveVector(active ? transform.rotation.Inverse() * CurrRcsMoveThrust : Vector3.zero);
			_rcsThrusters.SetRotateVector(active ? CurrRcsRotationThrust : Vector3.zero);
			_rcsThrusters.UpdateThrusters();
		}

		public void ConnectMessageListeners()
		{
			EventSystem.AddListener(typeof(ShipStatsMessage), ShipStatsMessageListener);
			EventSystem.AddListener(typeof(ManeuverCourseResponse), ManeuverCourseResponseListener);
			EventSystem.AddListener(typeof(VesselSecurityResponse), VesselSecurityResponseListener);
			EventSystem.AddListener(typeof(NameTagMessage), NameTagMessageListener);
			EventSystem.AddListener(typeof(VesselRequestResponse), VesselRequestResponseListener);
			EventSystem.AddListener(typeof(DestroyVesselMessage), DestroyVesselMessageListener);
		}

		private void DestroyVesselMessageListener(NetworkData data)
		{
			DestroyVesselMessage destroyVesselMessage = data as DestroyVesselMessage;
			if (destroyVesselMessage.GUID == Guid && GeometryPlaceholder != null && DestructionEffects != null)
			{
				DestructionEffects.transform.parent = GeometryPlaceholder.transform;
				DestructionEffects.transform.Reset();
				DestructionEffects.transform.parent = World.SpaceObjectsRoot.transform;
				DestructionEffects.gameObject.SetActive(value: true);
				DestructionEffects.enabled = true;
			}
		}

		private void VesselRequestResponseListener(NetworkData data)
		{
			VesselRequestResponse vesselRequestResponse = data as VesselRequestResponse;
			if (vesselRequestResponse.GUID == Guid)
			{
				if (vesselRequestResponse.Message == RescueShipMessages.ShipCalled)
				{
					World.InGameGUI.Notification(
						Localization.GetLocalizedField("RescueShipWillArriveIn") + " " +
						FormatHelper.PeriodFormat(vesselRequestResponse.Time), InGameGUI.NotificationType.Alert);
				}

				if (vesselRequestResponse.Message == RescueShipMessages.ShipEnRoute)
				{
					World.InGameGUI.Notification(
						Localization.GetLocalizedField("RescueShipEnRoute") + " " +
						FormatHelper.PeriodFormat(vesselRequestResponse.Time), InGameGUI.NotificationType.Alert);
				}

				if (vesselRequestResponse.Message == RescueShipMessages.AnotherShipInRange)
				{
					World.InGameGUI.Notification(Localization.GetLocalizedField("AnotherShipInRange"),
						InGameGUI.NotificationType.Alert);
				}

				if (vesselRequestResponse.Message == RescueShipMessages.ShipArrived)
				{
					World.InGameGUI.Notification(Localization.GetLocalizedField("RescueShipArrived"),
						InGameGUI.NotificationType.Alert);
				}
			}
		}
		public void SendCancelManeuverCourseRequest()
		{
			ManeuverCourseRequest maneuverCourseRequestData =
				World.Map.GetManeuverCourseRequestData(lockCourse: true);
			if (maneuverCourseRequestData == null)
			{
				maneuverCourseRequestData.ShipGUID = Guid;
				maneuverCourseRequestData.Activate = false;
				NetworkController.SendAndForget(maneuverCourseRequestData);
			}
		}

		public void SendManeuverCourseRequest()
		{
			ManeuverCourseRequest maneuverCourseRequestData =
				World.Map.GetManeuverCourseRequestData(lockCourse: true);
			if (maneuverCourseRequestData != null)
			{
				maneuverCourseRequestData.ShipGUID = Guid;
				NetworkController.SendAndForget(maneuverCourseRequestData);
			}
		}

		public void SendManeuverCourseActivationRequest()
		{
			if (CourseWaitingActivation > 0)
			{
				NetworkController.SendAndForget(new ManeuverCourseRequest
				{
					CourseGUID = CourseWaitingActivation,
					ShipGUID = Guid,
					Activate = true
				});
			}
		}

		private void ManeuverCourseResponseListener(NetworkData data)
		{
			ManeuverCourseResponse maneuverCourseResponse = data as ManeuverCourseResponse;
			if (maneuverCourseResponse.VesselGUID != Guid)
			{
				return;
			}

			if (maneuverCourseResponse.IsValid)
			{
				if (maneuverCourseResponse.StartDirection != null && maneuverCourseResponse.StartTime.HasValue)
				{
					CourseWaitingActivation = maneuverCourseResponse.CourseGUID;
					CourseStartDirection = maneuverCourseResponse.StartDirection.ToVector3();
					CourseStartTime = maneuverCourseResponse.StartTime.Value;
					CourseEndTime = maneuverCourseResponse.EndTime.Value;
					if (World.Map.WarpManeuver != null)
					{
						World.Map.WarpManeuver.Initialized = true;
					}

					if (_warpInductorExecutor != null)
					{
						_warpInductorExecutor.ToggleInductor(isActive: true, isInstant: false);
					}
				}

				if (!maneuverCourseResponse.IsActivated.HasValue || maneuverCourseResponse.IsActivated.Value)
				{
				}

				if (maneuverCourseResponse.StaringSoon.HasValue && maneuverCourseResponse.StaringSoon.Value)
				{
					IsWarpOnline = true;
					if (maneuverCourseResponse.EndTime.HasValue)
					{
						EndWarpTime = maneuverCourseResponse.EndTime.Value;
					}

					if (MyPlayer.Instance.IsInVesselHierarchy(this))
					{
						MyPlayer.Instance.CheckCameraShake();
					}
					else if (_warpStartEffect != null)
					{
						WarpStartEffectTask = new Action(delegate
						{
							ActivateWarpStartEffect();
							WarpStartEffectTask = null;
						});
					}

					if (_warpEffect != null && MyPlayer.Instance.IsInVesselHierarchy(this))
					{
						_warpEffect.SetActive(value: true);
					}

					foreach (SoundEffect warpSound in FTLEngine.WarpSounds)
					{
						warpSound.Play(0, dontPlayIfPlaying: true);
					}
				}
			}
			else
			{
				CancelManeuver();
				if (maneuverCourseResponse.IsFinished.HasValue && maneuverCourseResponse.IsFinished.Value &&
					!MyPlayer.Instance.IsInVesselHierarchy(this))
				{
					WarpEndEffectTask = new Action(delegate
					{
						ActivateWarpEndEffect();
						WarpEndEffectTask = null;
					});
				}
			}

			RichPresenceManager.UpdateStatus();
		}

		public void ActivateWarpStartEffect()
		{
			if (_warpStartEffect != null)
			{
				GameObject gameObject = Instantiate(_warpStartEffect.gameObject,
					World.SpaceObjectsRoot.transform);
				gameObject.transform.position = _warpStartEffect.transform.position;
				gameObject.transform.localScale = _warpStartEffect.transform.localScale;
				gameObject.Activate(value: true);
			}
		}

		public void ActivateWarpEndEffect()
		{
			if (_warpEndEffect != null)
			{
				GameObject gameObject =
					Instantiate(_warpEndEffect.gameObject, World.SpaceObjectsRoot.transform);
				gameObject.transform.position = _warpEndEffect.transform.position;
				gameObject.transform.localScale = _warpEndEffect.transform.localScale;
				gameObject.Activate(value: true);
			}
		}

		public void CancelManeuver()
		{
			CourseWaitingActivation = 0L;
			IsWarpOnline = false;
			if (MyPlayer.Instance.IsInVesselHierarchy(this))
			{
				foreach (SoundEffect warpSound in FTLEngine.WarpSounds)
				{
					warpSound.Play(1);
				}

				if (MyPlayer.Instance.LockedToTrigger is SceneTriggerNavigationPanel)
				{
					World.InWorldPanels.Navigation.CancelWarp();
				}

				World.Map.RemoveManeuverCourse();
			}

			if (MyPlayer.Instance.IsInVesselHierarchy(this))
			{
				MyPlayer.Instance.CheckCameraShake();
			}

			if (_warpEffect != null)
			{
				_warpEffect.SetActive(value: false);
			}

			if (_warpInductorExecutor != null)
			{
				_warpInductorExecutor.ToggleInductor(isActive: false, isInstant: false);
			}
		}

		private void VesselSecurityResponseListener(NetworkData data)
		{
			VesselSecurityResponse vesselSecurityResponse = data as VesselSecurityResponse;
			if (vesselSecurityResponse.VesselGUID == Guid && !(SecuritySystem == null))
			{
				SecuritySystem.ParseSecurityData(vesselSecurityResponse.Data);
				if (MyPlayer.Instance.LockedToTrigger is SceneTriggerPowerSupplyPanel)
				{
					(MyPlayer.Instance.LockedToTrigger as SceneTriggerPowerSupplyPanel).MyPowerSupply
						.RefreshPowerSupply();
				}

				if (MyPlayer.Instance.LockedToTrigger is SceneTriggerLifeSupportPanel)
				{
					(MyPlayer.Instance.LockedToTrigger as SceneTriggerLifeSupportPanel).MyLifeSupport
						.RefreshLifeSupport();
				}
			}
		}

		private void NameTagMessageListener(NetworkData data)
		{
			NameTagMessage nameTagMessage = data as NameTagMessage;
			if (nameTagMessage.ID.VesselGUID != Guid)
			{
				return;
			}

			try
			{
				NameTags[nameTagMessage.ID.InSceneID].SetNameTagText(nameTagMessage.NameTagText);
			}
			catch
			{
			}
		}

		private void UpdateShipObjects(VesselObjects shipObjects, bool isInitialize)
		{
			if (shipObjects == null || !gameObject.activeInHierarchy)
			{
				return;
			}

			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			if (shipObjects.MiscStatuses != null)
			{
				if (shipObjects.MiscStatuses.CourseInProgress != null)
				{
					IsWarpOnline = true;
					EndWarpTime = shipObjects.MiscStatuses.CourseInProgress.EndSolarSystemTime;
				}

				if (_engineThrusters != null)
				{
					_engineThrusters.OnOff = EngineOnLine;
				}

				if (_warpEffect != null)
				{
					_warpEffect.SetActive(IsWarpOnline && MyPlayer.Instance.IsInVesselHierarchy(this));
				}

				if (_warpInductorExecutor != null)
				{
					_warpInductorExecutor.ToggleInductor(IsWarpOnline, isInstant: true);
				}

				MyPlayer.Instance.CheckCameraShake();
			}

			if (shipObjects.SecurityData != null && SecuritySystem != null)
			{
				SecuritySystem.ParseSecurityData(shipObjects.SecurityData);
			}

			if (shipObjects.SubSystems != null)
			{
				foreach (SubSystemDetails subSystem in shipObjects.SubSystems)
				{
					try
					{
						SubSystem structureObject = GetStructureObject<SubSystem>(subSystem.InSceneID);
						structureObject.SetDetails(subSystem);
						if (MyPlayer.Instance.Parent == this && structureObject is VesselBaseSystem &&
							MyPlayer.Instance.ShipControlMode == ShipControlMode.Navigation &&
							structureObject.Status == SystemStatus.Offline)
						{
							MyPlayer.Instance.ShipControlMode = ShipControlMode.Piloting;
						}

						if ((structureObject is IPowerConsumer || structureObject is VesselBaseSystem) &&
							MyPlayer.Instance.LockedToTrigger is SceneTriggerPowerSupplyPanel)
						{
							flag2 = true;
						}

						if (structureObject is SubSystemRefinery)
						{
							if (MyPlayer.Instance.LockedToTrigger is SceneTriggerCargoPanel)
							{
								flag3 = true;
							}
						}
						else if (structureObject is SubSystemFabricator)
						{
							if (MyPlayer.Instance.LockedToTrigger is SceneTriggerCargoPanel)
							{
								flag3 = true;
							}
						}
						else if (structureObject is SubSystemEngine)
						{
							if (MyPlayer.Instance.IsInVesselHierarchy(this))
							{
								MyPlayer.Instance.CheckCameraShake();
							}

							if (_engineThrusters != null)
							{
								_engineThrusters.OnOff = structureObject.Status == SystemStatus.Online;
							}
						}
						else if (structureObject is SubSystemFTL && _warpEffect != null && isInitialize)
						{
							_warpEffect.SetActive(
								structureObject.Status == SystemStatus.Online &&
								MyPlayer.Instance.IsInVesselHierarchy(this), instant: true);
						}

						if (structureObject.Room != null)
						{
							structureObject.Room.ExecuteBehaviourScripts();
						}
					}
					catch (Exception ex)
					{
						Debug.LogErrorFormat("SubSystemDetails exception {0} {1} {2} {3} {4}", Guid, SceneId.ToString(), subSystem.InSceneID,
							ex.Message, ex.StackTrace);
					}
				}
			}

			if (shipObjects.NameTags != null)
			{
				foreach (NameTagData nameTag in shipObjects.NameTags)
				{
					try
					{
						SceneNameTag structureObject2 = GetStructureObject<SceneNameTag>(nameTag.InSceneID);
						structureObject2.SetNameTagText(nameTag.NameTagText);
					}
					catch (Exception ex2)
					{
						Debug.LogErrorFormat("NameTagData exception {0}, {1}, {2}, {3}, {4}", Guid, SceneId.ToString(), nameTag.InSceneID,
							ex2.Message, ex2.StackTrace);
					}
				}
			}

			if (shipObjects.Generators != null)
			{
				foreach (GeneratorDetails generator in shipObjects.Generators)
				{
					try
					{
						Generator structureObject3 = GetStructureObject<Generator>(generator.InSceneID);
						structureObject3.SetDetails(generator);
						if (structureObject3 is GeneratorAir || structureObject3 is GeneratorScrubbedAir)
						{
							flag = true;
							flag2 = true;
						}

						if (MyPlayer.Instance.LockedToTrigger is SceneTriggerPowerSupplyPanel)
						{
							(MyPlayer.Instance.LockedToTrigger as SceneTriggerPowerSupplyPanel).MyPowerSupply
								.UpdateGenerator(structureObject3);
							(MyPlayer.Instance.LockedToTrigger as SceneTriggerPowerSupplyPanel).MyPowerSupply
								.UpdateCapacitor(structureObject3);
						}

						if (MyPlayer.Instance.LockedToTrigger is SceneTriggerLifeSupportPanel)
						{
							(MyPlayer.Instance.LockedToTrigger as SceneTriggerLifeSupportPanel).MyLifeSupport
								.UpdateGenerator(structureObject3);
						}
					}
					catch (Exception ex3)
					{
						Debug.LogErrorFormat("GeneratorDetails exception {0}, {1}, {2}, {3}, {4}", Guid, SceneId.ToString(), generator.InSceneID,
							ex3.Message, ex3.StackTrace);
					}
				}
			}

			if (shipObjects.RoomTriggers != null)
			{
				bool flag4 = false;
				foreach (RoomDetails roomTrigger in shipObjects.RoomTriggers)
				{
					try
					{
						SceneTriggerRoom structureObject4 = GetStructureObject<SceneTriggerRoom>(roomTrigger.InSceneID);
						if (structureObject4.AirPressure < 1f && roomTrigger.AirPressure == 1f)
						{
							SceneQuestTrigger.OnTrigger(structureObject4.gameObject, SceneQuestTriggerEvent.Pressurize);
						}
						else if (structureObject4.AirPressure > 0f && roomTrigger.AirPressure == 0f)
						{
							SceneQuestTrigger.OnTrigger(structureObject4.gameObject,
								SceneQuestTriggerEvent.Depressurize);
						}

						short compoundRoomID = structureObject4.CompoundRoomID;
						if (structureObject4.UseGravity != roomTrigger.UseGravity)
						{
							structureObject4.UseGravity = roomTrigger.UseGravity;
							MyPlayer.Instance.CheckRoomTrigger(structureObject4);
							DynamicObject[] componentsInChildren =
								TransferableObjectsRoot.GetComponentsInChildren<DynamicObject>();
							foreach (DynamicObject dynamicObject in componentsInChildren)
							{
								dynamicObject.CheckRoomTrigger(structureObject4);
							}

							Corpse[] componentsInChildren2 = TransferableObjectsRoot.GetComponentsInChildren<Corpse>();
							foreach (Corpse corpse in componentsInChildren2)
							{
								corpse.CheckRoomTrigger(structureObject4);
							}
						}

						structureObject4.AirFiltering = roomTrigger.AirFiltering;
						structureObject4.AirPressure = roomTrigger.AirPressure;
						structureObject4.AirQuality = roomTrigger.AirQuality;
						structureObject4.AirPressureChangeRate = roomTrigger.AirPressureChangeRate;
						structureObject4.AirQualityChangeRate = roomTrigger.AirQualityChangeRate;
						structureObject4.PressurizationStatus = roomTrigger.PressurizationStatus;
						structureObject4.CompoundRoomID = roomTrigger.CompoundRoomID;
						structureObject4.Fire = roomTrigger.Fire;
						structureObject4.Breach = roomTrigger.Breach;
						bool flag5 = structureObject4.GravityMalfunction != roomTrigger.GravityMalfunction &&
									 MyPlayer.Instance.CurrentRoomTrigger == structureObject4;
						structureObject4.GravityMalfunction = roomTrigger.GravityMalfunction;
						structureObject4.ExecuteBehaviourScripts();
						if (flag5)
						{
							MyPlayer.Instance.CheckRoomTrigger(null);
						}

						if (MyPlayer.Instance.IsLockedToTrigger &&
							MyPlayer.Instance.LockedToTrigger is SceneTriggerLifeSupportPanel)
						{
							(MyPlayer.Instance.LockedToTrigger as SceneTriggerLifeSupportPanel).MyLifeSupport
								.UpdateRoom(structureObject4);
						}

						if (MyPlayer.Instance.CurrentRoomTrigger != null &&
							((MyPlayer.Instance.CurrentRoomTrigger.CompoundRoomID == compoundRoomID &&
							  compoundRoomID != roomTrigger.CompoundRoomID) ||
							 MyPlayer.Instance.CurrentRoomTrigger.CompoundRoomID == structureObject4.CompoundRoomID))
						{
							flag4 = true;
						}
					}
					catch (Exception ex4)
					{
						Debug.LogErrorFormat("RoomDetails exception {0}, {1}, {2}, {3}, {4}", Guid, SceneId.ToString(), roomTrigger.InSceneID,
							ex4.Message, ex4.StackTrace);
					}
				}

				if (flag4)
				{
					World.InGameGUI.HelmetHud.WarningsUpdate();
				}
			}

			if (shipObjects.ResourceContainers != null)
			{
				foreach (ResourceContainerDetails resourceContainer in shipObjects.ResourceContainers)
				{
					try
					{
						ResourceContainer structureObject5 =
							GetStructureObject<ResourceContainer>(resourceContainer.InSceneID);
						float quantity = structureObject5.Quantity;
						structureObject5.SetDetails(resourceContainer);
						if (MyPlayer.Instance.IsLockedToTrigger &&
							MyPlayer.Instance.LockedToTrigger is SceneTriggerCargoPanel)
						{
							(MyPlayer.Instance.LockedToTrigger as SceneTriggerCargoPanel).CargoPanel
								.RefreshSystemObject(structureObject5);
							if (structureObject5.Quantity > quantity)
							{
								SceneQuestTrigger.OnTrigger(structureObject5.gameObject,
									SceneQuestTriggerEvent.IncreaseQuantity);
							}
							else if (structureObject5.Quantity < quantity)
							{
								SceneQuestTrigger.OnTrigger(structureObject5.gameObject,
									SceneQuestTriggerEvent.DecreaseQuantity);
							}
						}

						if (_refuelingStation != null)
						{
							_refuelingStation.UpdateResourceContainer(structureObject5);
						}

						if (MyPlayer.Instance.IsLockedToTrigger &&
							MyPlayer.Instance.LockedToTrigger is SceneTriggerPowerSupplyPanel)
						{
							(MyPlayer.Instance.LockedToTrigger as SceneTriggerPowerSupplyPanel).MyPowerSupply
								.UpdateGenerator(structureObject5);
						}
					}
					catch (Exception ex5)
					{
						Debug.LogErrorFormat("ResourceContainerDetails exception {0}, {1}, {2}, {3}, {4}", Guid, SceneId.ToString(),
							resourceContainer.InSceneID, ex5.Message, ex5.StackTrace);
					}
				}
			}

			if (shipObjects.Doors != null)
			{
				foreach (DoorDetails door in shipObjects.Doors)
				{
					try
					{
						SceneDoor structureObject6 = GetStructureObject<SceneDoor>(door.InSceneID);
						structureObject6.SetDoorDetails(door);
						structureObject6.UpdateDoorUI();
					}
					catch (Exception ex6)
					{
						Debug.LogErrorFormat("DoorDetails exception {0}, {1}, {2}, {3}, {4}", Guid, SceneId.ToString(), door.InSceneID,
							ex6.Message, ex6.StackTrace);
					}
				}
			}

			if (shipObjects.SceneTriggerExecutors != null)
			{
				foreach (SceneTriggerExecutorDetails sceneTriggerExecuter in shipObjects.SceneTriggerExecutors)
				{
					try
					{
						SceneTriggerExecutor structureObject7 =
							GetStructureObject<SceneTriggerExecutor>(sceneTriggerExecuter.InSceneID);
						structureObject7.SetExecutorDetails(sceneTriggerExecuter, isInitialize);
					}
					catch (Exception ex7)
					{
						Debug.LogErrorFormat("SceneTriggerExecuterDetails exception {0}, {1}, {2}, {3}, {4}", Guid, SceneId.ToString(),
							sceneTriggerExecuter.InSceneID, ex7.Message, ex7.StackTrace);
					}
				}
			}

			if (shipObjects.DockingPorts != null)
			{
				foreach (SceneDockingPortDetails dockingPort in shipObjects.DockingPorts)
				{
					try
					{
						SceneDockingPort structureObject8 =
							GetStructureObject<SceneDockingPort>(dockingPort.ID.InSceneID);
						if (MyPlayer.Instance.LockedToTrigger is SceneTriggerDockingPanel ||
							MyPlayer.Instance.LockedToTrigger is SceneTriggerShipControl)
						{
							if (structureObject8.DockedToPort == null && dockingPort.DockedToID != null)
							{
								SceneQuestTrigger.OnTrigger(structureObject8.gameObject, SceneQuestTriggerEvent.Dock);
							}
							else if (structureObject8.DockedToPort != null && dockingPort.DockedToID == null)
							{
								SceneQuestTrigger.OnTrigger(structureObject8.gameObject, SceneQuestTriggerEvent.Undock);
							}
						}

						structureObject8.SetDetails(dockingPort, isInitialize);
						if (MyPlayer.Instance.LockedToTrigger is SceneTriggerPowerSupplyPanel)
						{
							(MyPlayer.Instance.LockedToTrigger as SceneTriggerPowerSupplyPanel).MyPowerSupply
								.RefreshPowerSupply();
						}

						if (MyPlayer.Instance.LockedToTrigger is SceneTriggerCargoPanel)
						{
							(MyPlayer.Instance.LockedToTrigger as SceneTriggerCargoPanel).CargoPanel
								.RefreshCargoPanel();
						}

						if (MyPlayer.Instance.LockedToTrigger is SceneTriggerLifeSupportPanel)
						{
							(MyPlayer.Instance.LockedToTrigger as SceneTriggerLifeSupportPanel).MyLifeSupport
								.RefreshLifeSupport();
						}

						if (MyPlayer.Instance.LockedToTrigger is SceneTriggerAirlockPanel)
						{
							(MyPlayer.Instance.LockedToTrigger as SceneTriggerAirlockPanel).AirlockUI
								.GetVesselAndAirTanks();
						}
					}
					catch (Exception ex)
					{
						Debug.LogErrorFormat("SceneDockingPortDetails exception {0}, {1}, {2}, {3}, {4}", Guid, SceneId.ToString(),
							dockingPort.ID, ex.Message, ex.StackTrace);
					}
				}
			}

			if (shipObjects.CargoBay != null)
			{
				try
				{
					CargoBay.SetDetails(shipObjects.CargoBay);
					if (MyPlayer.Instance.IsLockedToTrigger &&
						MyPlayer.Instance.LockedToTrigger is SceneTriggerCargoPanel)
					{
						CargoPanel cargoPanel =
							(MyPlayer.Instance.LockedToTrigger as SceneTriggerCargoPanel).CargoPanel;
						if (cargoPanel.CurrentlySelectedCargoBay == CargoBay)
						{
							cargoPanel.RefreshMainCargoResources();
						}

						cargoPanel.UpdateVesselObjects(CargoBay.ParentVessel);
					}
				}
				catch (Exception ex)
				{
					Debug.LogErrorFormat("CargoBayDetails exception {0}, {1}, {2}, {3}, {4}", Guid, SceneId.ToString(),
						shipObjects.CargoBay.InSceneID, ex.Message, ex.StackTrace);
				}
			}

			if (shipObjects.SpawnWithChance != null)
			{
			}

			if (shipObjects.SpawnPoints != null)
			{
				foreach (SpawnPointStats spawnPoint in shipObjects.SpawnPoints)
				{
					try
					{
						SceneSpawnPoint structureObject9 = GetStructureObject<SceneSpawnPoint>(spawnPoint.InSceneID);
						if (MyPlayer.Instance.LockedToTrigger is SceneTriggerCryoPod)
						{
							long? playerGUID = spawnPoint.PlayerGUID;
							if (playerGUID.GetValueOrDefault() == MyPlayer.Instance.Guid && playerGUID.HasValue &&
								spawnPoint.NewState == SpawnPointState.Authorized)
							{
								SceneQuestTrigger.OnTrigger(structureObject9.gameObject,
									SceneQuestTriggerEvent.AssignOnCryoPod);
							}
						}

						structureObject9.SetStats(spawnPoint);
					}
					catch (Exception ex)
					{
						Debug.LogErrorFormat("SpawnPointStats exception {0}, {1}, {2}, {3}, {4}", Guid, SceneId.ToString(), spawnPoint.InSceneID,
							ex.Message, ex.StackTrace);
					}
				}
			}

			if (shipObjects.RepairPoints != null)
			{
				foreach (VesselRepairPointDetails repairPoint in shipObjects.RepairPoints)
				{
					try
					{
						VesselRepairPoint structureObject10 =
							GetStructureObject<VesselRepairPoint>(repairPoint.InSceneID);
						structureObject10.SetDetails(repairPoint);
					}
					catch (Exception ex)
					{
						Debug.LogErrorFormat("VesselRepairPoint exception {0}, {1}, {2}, {3}, {4}", Guid, SceneId.ToString(),
							repairPoint.InSceneID, ex.Message, ex.StackTrace);
					}
				}
			}

			if (shipObjects.EmblemId != null)
			{
				try
				{
					foreach (SceneVesselEmblem item in Emblems.Where((SceneVesselEmblem m) => m != null))
					{
						item.SetEmblem(shipObjects.EmblemId);
					}

					if (SecuritySystem != null)
					{
						SecuritySystem.UpdateUI();
					}
				}
				catch (Exception ex)
				{
					Debug.LogErrorFormat("Emblem exception {0}, {1}, {2}, {3}", Guid, SceneId.ToString(), ex.Message, ex.StackTrace);
				}
			}

			if (flag && MyPlayer.Instance.LockedToTrigger is SceneTriggerLifeSupportPanel)
			{
				(MyPlayer.Instance.LockedToTrigger as SceneTriggerLifeSupportPanel).MyLifeSupport.GetPowerStatus();
			}

			if (flag2 && MyPlayer.Instance.LockedToTrigger is SceneTriggerPowerSupplyPanel)
			{
				(MyPlayer.Instance.LockedToTrigger as SceneTriggerPowerSupplyPanel).MyPowerSupply
					.UpdateVesselConsumers(this);
			}

			if (flag3 && MyPlayer.Instance.LockedToTrigger is SceneTriggerCargoPanel)
			{
				if ((MyPlayer.Instance.LockedToTrigger as SceneTriggerCargoPanel).CargoPanel.RefineryActive
					.activeInHierarchy)
				{
					(MyPlayer.Instance.LockedToTrigger as SceneTriggerCargoPanel).CargoPanel.UpdateRefineryResources();
				}
				else if ((MyPlayer.Instance.LockedToTrigger as SceneTriggerCargoPanel).CargoPanel.CraftingActive
						 .activeInHierarchy)
				{
					(MyPlayer.Instance.LockedToTrigger as SceneTriggerCargoPanel).CargoPanel.UpdateCraftingResources();
				}
			}
		}

		private void ShipStatsMessageListener(NetworkData data)
		{
			ShipStatsMessage shipStatsMessage = data as ShipStatsMessage;
			if (shipStatsMessage.Guid != Guid)
			{
				return;
			}

			if (shipStatsMessage.VesselObjects != null)
			{
				if (SceneObjectsLoaded)
				{
					UpdateShipObjects(shipStatsMessage.VesselObjects, isInitialize: false);
				}
				else
				{
					_shipObjectsLoadingQueue ??= new Queue<VesselObjects>();
					_shipObjectsLoadingQueue.Enqueue(shipStatsMessage.VesselObjects);
				}
			}

			if (shipStatsMessage.Temperature.HasValue)
			{
				Temperature = shipStatsMessage.Temperature.Value;
			}

			DoorEnviormentPanel[] doorEnviormentPanels = null;
			if (shipStatsMessage.Health.HasValue || shipStatsMessage.Armor.HasValue)
			{
				doorEnviormentPanels = GeometryRoot.GetComponentsInChildren<DoorEnviormentPanel>();
			}

			if (shipStatsMessage.Health.HasValue)
			{
				Health = shipStatsMessage.Health.Value;
				foreach (DoorEnviormentPanel doorEnviormentPanel in doorEnviormentPanels)
				{
					doorEnviormentPanel.DoorEnviormentUpdateUI();
				}

				foreach (VesselHealthDecal vesselHealthDecal in GeometryRoot.GetComponentsInChildren<VesselHealthDecal>(
							 includeInactive: true))
				{
					vesselHealthDecal.UpdateDecals();
				}

				float num = GetDamagePointsFrequency();
				bool flag2 = this.IsInvoking(ActivateDamagePoints);
				if (num > float.Epsilon && !flag2)
				{
					ActivateDamagePoints();
				}
				else if (num <= float.Epsilon && flag2)
				{
					this.CancelInvoke(ActivateDamagePoints);
				}

				if (MyPlayer.Instance.LockedToTrigger is SceneTriggerLifeSupportPanel)
				{
					(MyPlayer.Instance.LockedToTrigger as SceneTriggerLifeSupportPanel).MyLifeSupport
						.UpdateConnectedVesselsHealth();
				}
			}

			if (shipStatsMessage.Armor.HasValue)
			{
				Armor = shipStatsMessage.Armor.Value;
				foreach (DoorEnviormentPanel doorEnviormentPanel2 in doorEnviormentPanels)
				{
					doorEnviormentPanel2.DoorEnviormentUpdateUI();
				}

				foreach (VesselArmorDecal vesselArmorDecal in GeometryRoot.GetComponentsInChildren<VesselArmorDecal>())
				{
					vesselArmorDecal.UpdateDecals();
				}

				if (MyPlayer.Instance.LockedToTrigger is SceneTriggerLifeSupportPanel)
				{
					(MyPlayer.Instance.LockedToTrigger as SceneTriggerLifeSupportPanel).MyLifeSupport
						.UpdateConnectedVesselsHealth();
				}
			}

			if (shipStatsMessage.SelfDestructTime.HasValue)
			{
				if (shipStatsMessage.SelfDestructTime.Value >= 0f)
				{
					SelfDestructTimer = shipStatsMessage.SelfDestructTime.Value;
				}
				else
				{
					SelfDestructTimer = null;
				}

				DoorEnviormentPanel[] componentsInChildren5 =
					GeometryRoot.GetComponentsInChildren<DoorEnviormentPanel>();
				foreach (DoorEnviormentPanel doorEnviormentPanel3 in componentsInChildren5)
				{
					doorEnviormentPanel3.DoorEnviormentUpdateUI();
				}

				if (SecuritySystem != null)
				{
					SecuritySystem.UpdateSelfDestructTimer();
				}
			}
		}

		private float GetDamagePointsFrequency()
		{
			return DamagePointEffectFrequency == null
				? 0f
				: DamagePointEffectFrequency.Evaluate(Health / MaxHealth);
		}

		private async UniTask LoadAllShipScenesAsync(VesselObjects vesselObjects, DockedVesselData[] dockedVessels, bool optimise = true)
		{
			await LoadInternalAsync(vesselObjects, optimise);

			// Docked vessels arrive embedded in their parent's spawn data and inherit the parent's transform.
			if (dockedVessels != null)
			{
				foreach (DockedVesselData dockedVessel in dockedVessels)
				{
					await SpawnDockedVessel(dockedVessel, optimise);
				}
			}

			// Resolve docking ports now that every vessel in the tree has loaded its scenes.
			ApplyDockingPortDetails(this, vesselObjects);
			if (dockedVessels != null)
			{
				foreach (DockedVesselData dockedVessel in dockedVessels)
				{
					ApplyDockingPortDetails(World.GetVessel(dockedVessel.Guid) as Ship, dockedVessel.VesselObjects);
				}
			}

			DynamicObject[] dynamicObjects = TransferableObjectsRoot.GetComponentsInChildren<DynamicObject>();
			foreach (DynamicObject dynamicObject in dynamicObjects)
			{
				dynamicObject.ToggleEnabled(isEnabled: true, toggleColliders: true);
				dynamicObject.CheckRoomTrigger(null);
			}

			Corpse[] corpses = TransferableObjectsRoot.GetComponentsInChildren<Corpse>();
			foreach (Corpse corpse in corpses)
			{
				corpse.CheckRoomTrigger(null);
			}

			// The security system only exists once its scene has loaded, so the hull name tags are set here.
			if (SecuritySystem != null)
			{
				SecuritySystem.UpdateUI();
				foreach (SceneNameTag nameTag in SecuritySystem.ShipNameTags)
				{
					nameTag.SetNameTagText(VesselName);
				}
			}
		}

		/// <summary>
		/// 	Spawns a single docked vessel as a child Ship and loads its scenes.
		/// </summary>
		private async UniTask SpawnDockedVessel(DockedVesselData dockedVessel, bool optimise)
		{
			Ship childShip = InitialiseArtificialBody(World, dockedVessel.Guid, SpaceObjectType.Ship,
				transform.localPosition, transform.localRotation) as Ship;
			childShip.VesselRegistration = dockedVessel.VesselRegistration;
			childShip.VesselName = dockedVessel.VesselName;
			childShip.Tag = dockedVessel.Tag;
			childShip.SpawnRuleId = dockedVessel.SpawnRuleId;
			childShip.SceneId = dockedVessel.SceneId;
			childShip.CollidersCenterOffset = dockedVessel.CollidersCenterOffset;
			childShip.IsDebrisFragment = dockedVessel.IsDebrisFragment;
			childShip.RadarSignature = dockedVessel.RadarSignature;
			childShip.IsDistressSignalActive = dockedVessel.IsDistressSignalActive;
			childShip.IsAlwaysVisible = dockedVessel.IsAlwaysVisible;
			childShip.DockingControlsDisabled = dockedVessel.DockingControlsDisabled;
			childShip.SecurityPanelsLocked = dockedVessel.SecurityPanelsLocked;
			childShip.gameObject.SetActive(true);
			await childShip.LoadInternalAsync(dockedVessel.VesselObjects, optimise);
		}

		/// <summary>
		/// 	Applies the stored docking-port state for a vessel, linking each port to the port it is docked
		/// 	to. Must run only after both vessels of every dock pair have loaded their scenes.
		/// </summary>
		private static void ApplyDockingPortDetails(Ship ship, VesselObjects vesselObjects)
		{
			if (vesselObjects?.DockingPorts == null)
			{
				return;
			}

			SceneDockingPort[] dockingPorts =
				ship.GeometryRoot.GetComponentsInChildren<SceneDockingPort>(includeInactive: true);
			foreach (SceneDockingPort dockingPort in dockingPorts)
			{
				SceneDockingPortDetails details =
					vesselObjects.DockingPorts.Find(m => m.ID.InSceneID == dockingPort.InSceneID);
				if (details != null)
				{
					dockingPort.SetDetails(details, isInitialize: true);
				}
			}
		}

		private async UniTask LoadStructureScenesAsync(GameScenes.SceneId sceneID, Transform rootTransform,
			VesselObjects shipObjects)
		{
			await Globals.SceneLoader.LoadSceneAsync(SceneLoader.SceneType.Structure, (long)sceneID);

			GameObject sceneRoot = Globals.SceneLoader.GetLoadedScene(SceneLoader.SceneType.Structure, sceneID);
			sceneRoot.transform.SetParent(rootTransform);
			sceneRoot.transform.localRotation = Quaternion.identity;
			RootObject = sceneRoot;
			if (GeometryRoot != null)
			{
				DestructionEffects =
					GeometryRoot.GetComponentInChildren<VesselDestructionEffects>(includeInactive: true);
				if (DestructionEffects != null)
				{
					DestructionEffects.gameObject.SetActive(value: false);
				}
			}

			if (TargetRotation.HasValue)
			{
				SetTargetPositionAndRotation(null, TargetRotation.Value, instant: true);
			}
			World.ActiveVessels.TryAdd(Guid, this);
			sceneRoot.SetActive(value: true);
			sceneRoot.SetActive(value: true);
			StructureScene sscene = sceneRoot.GetComponent<StructureScene>();
			Mass = sscene.Mass * 1000f;
			List<StructureSceneConnection> sceneConnections =
				new List<StructureSceneConnection>(sceneRoot.GetComponentsInChildren<StructureSceneConnection>());
			List<long> connectedConnections = new List<long>();
			sceneRoot.transform.localPosition = Vector3.zero;
			SceneHelper.FillAttachPoints(this, sceneRoot, AttachPoints, shipObjects?.AttachPoints);
			SceneHelper.FillSubSystems(sceneRoot, SubSystems, shipObjects?.SubSystems);
			SceneHelper.FillGenerators(sceneRoot, Generators, shipObjects?.Generators);
			SceneHelper.FillRoomTriggers(this, sceneRoot, RoomTriggers, shipObjects?.RoomTriggers);
			SceneHelper.FillResourceContainers(sceneRoot, ResourceContainers, shipObjects?.ResourceContainers);
			SceneHelper.FillDoors(this, sceneRoot, Doors, shipObjects?.Doors);
			SceneHelper.FillSceneTriggerExecutors(this, sceneRoot, SceneTriggerExecutors,
				shipObjects?.SceneTriggerExecutors);
			SceneHelper.FillSpawnWithChanceData(sceneRoot, shipObjects.SpawnWithChance);
			SceneHelper.FillSceneDockingPorts(this, sceneRoot, DockingPorts, shipObjects?.DockingPorts);
			SceneHelper.FillSpawnPoints(this, sceneRoot, SpawnPoints, shipObjects?.SpawnPoints);
			SceneHelper.FillCubemapProbes(sceneRoot, World);
			SceneHelper.FillNameTags(this, sceneRoot, NameTags, shipObjects?.NameTags);
			SceneHelper.FillRepairPoints(this, sceneRoot, RepairPoints, shipObjects?.RepairPoints);
			SceneHelper.CheckTags(sceneRoot, Tag);
			SceneHelper.FillEmblems(sceneRoot, this);
			SceneHelper.FillDamagePoints(sceneRoot, this);
			VesselBaseSystem = sceneRoot.GetComponentInChildren<VesselBaseSystem>();
			RCS = sceneRoot.GetComponentInChildren<SubSystemRCS>();
			Engine = sceneRoot.GetComponentInChildren<SubSystemEngine>();
			FTLEngine = sceneRoot.GetComponentInChildren<SubSystemFTL>();
			Capacitor = sceneRoot.GetComponentInChildren<GeneratorCapacitor>();
			CargoBay = sceneRoot.GetComponentInChildren<SceneCargoBay>();
			NavPanel = sceneRoot.GetComponentInChildren<NavigationPanel>(includeInactive: true);
			RadarSystem = sceneRoot.GetComponentInChildren<SubSystemRadar>();
			SecuritySystem = sceneRoot.GetComponentInChildren<SecuritySystem>(includeInactive: true);
			MaxHealth = sscene.MaxHealth;
			DamagePointEffectFrequency = sscene.DamageEffectsFrequency;
			foreach (StructureSceneConnection item in sceneConnections)
			{
				bool showEnabled = connectedConnections.Contains(item.InSceneID);
				item.ToggleObjects(showEnabled);
			}
		}

		public void ActivateDamagePoints()
		{
			float damagePointsFrequency = GetDamagePointsFrequency();
			if (damagePointsFrequency <= float.Epsilon)
			{
				return;
			}

			int num = MathHelper.Clamp((int)damagePointsFrequency, 1, int.MaxValue);
			int num2 = 0;
			foreach (DamagePointData item in DamagePoints.OrderBy((DamagePointData m) => MathHelper.RandomNextDouble()))
			{
				Vector3 vector = item.ParentTransform.position + item.Position;
				Collider[] source = Physics.OverlapSphere(vector, 0.05f);
				bool flag = source.FirstOrDefault((Collider m) => m.GetComponentInParent<SceneTriggerRoom>()) != null;
				bool flag2 = MyPlayer.Instance.IsInVesselHierarchy(MainVessel);
				if ((!item.UseOcclusion || flag2 == flag) &&
					(vector - MyPlayer.Instance.transform.position).magnitude <= item.VisibilityThreshold)
				{
					Transform transform = item.ParentTransform.Find("DamagePointTemp");
					if (transform == null)
					{
						GameObject gameObject = new GameObject("DamagePointTemp");
						gameObject.Activate(value: false);
						gameObject.transform.parent = item.ParentTransform;
						gameObject.transform.Reset(resetScale: true);
						transform = gameObject.transform;
					}

					GameObject gameObject2 = Instantiate(
						item.Effects.OrderBy((GameObject m) => MathHelper.RandomNextDouble()).FirstOrDefault(),
						transform);
					gameObject2.transform.SetLocalPositionAndRotation(item.Position, item.Rotation);
					gameObject2.transform.localScale = item.Scale;
					if (!flag)
					{
						SoundEffect componentInChildren =
							gameObject2.GetComponentInChildren<SoundEffect>(includeInactive: true);
						if (componentInChildren != null)
						{
							Destroy(componentInChildren);
						}
					}

					gameObject2.transform.parent = item.ParentTransform;
				}

				if (++num2 >= num)
				{
					break;
				}
			}

			this.Invoke(ActivateDamagePoints,
				MathHelper.RandomRange(0f, MathHelper.Clamp(1f / damagePointsFrequency, 1f, 10f)));
		}

		private async UniTask LoadInternalAsync(VesselObjects shipObjects, bool optimise)
		{
			World.InGameGUI.ToggleBusyLoading(true);
			await LoadStructureScenesAsync(SceneId, GeometryRoot.transform, shipObjects);
			UpdateShipObjects(shipObjects, isInitialize: true);
			World.InGameGUI.ToggleBusyLoading(false);

			OnSceneLoaded();
			if (shipObjects is { SecurityData: not null } && SecuritySystem != null)
			{
				SecuritySystem.ParseSecurityData(shipObjects.SecurityData);
			}

			if (!CollidersCenterOffset.ToVector3().IsEpsilonEqual(Vector3.zero))
			{
				GeometryPlaceholder.transform.localPosition = -CollidersCenterOffset.ToVector3();
				base.UpdateArtificialBodyPosition(updateChildren: false);
			}

			if (optimise)
			{
				ToggleOptimization(true);
			}

			if (_shipObjectsLoadingQueue != null)
			{
				while (_shipObjectsLoadingQueue.Count > 0)
				{
					UpdateShipObjects(_shipObjectsLoadingQueue.Dequeue(), isInitialize: false);
				}
			}

			SceneObjectsLoaded = true;
			ZeroOcclusion.CheckOcclusionFor(this, onlyCheckDistance: false);
		}

		private void DockUndockCompleted(bool isDock, bool isInitialize)
		{
			if (_currDockDetails == null)
			{
				return;
			}

			if (OnDockCompleted != null && isDock)
			{
				OnDockCompleted(isInitialize);
				OnDockCompleted = null;
			}
			else if (OnUndockCompleted != null && !isDock)
			{
				OnUndockCompleted(isInitialize);
				OnUndockCompleted = null;
			}

			if (isDock)
			{
				if (_currDockDetails.ExecutorsMerge != null && _currDockDetails.ExecutorsMerge.Count > 0)
				{
					Ship ship = _currDockDetails.ExecutorsMerge[0].ParentTriggerID.VesselGUID != Guid
						? World.GetVessel(_currDockDetails.ExecutorsMerge[0].ParentTriggerID.VesselGUID) as Ship
						: this;
					Ship ship2 = _currDockDetails.ExecutorsMerge[0].ChildTriggerID.VesselGUID != Guid
						? World.GetVessel(_currDockDetails.ExecutorsMerge[0].ChildTriggerID.VesselGUID) as Ship
						: this;
					if (ship != null && ship.SceneObjectsLoaded && ship2 != null && ship2.SceneObjectsLoaded)
					{
						foreach (ExecutorMergeDetails item in _currDockDetails.ExecutorsMerge)
						{
							SceneTriggerExecutor sceneTriggerExecutor =
								ship.GetStructureObject<SceneTriggerExecutor>(item.ParentTriggerID.InSceneID);
							SceneTriggerExecutor sceneTriggerExecuter2 =
								ship2.GetStructureObject<SceneTriggerExecutor>(item.ChildTriggerID.InSceneID);
							if (sceneTriggerExecutor != null && sceneTriggerExecuter2 != null)
							{
								sceneTriggerExecutor.SetChild(sceneTriggerExecuter2, isInitialize);
							}
						}
					}
				}
			}
			else if (_currDockDetails.ExecutorsMerge != null && _currDockDetails.ExecutorsMerge.Count > 0)
			{
				Ship ship3 = _currDockDetails.ExecutorsMerge[0].ParentTriggerID.VesselGUID != Guid
					? World.GetVessel(_currDockDetails.ExecutorsMerge[0].ParentTriggerID.VesselGUID) as Ship
					: this;
				if (ship3 != null && ship3.SceneObjectsLoaded)
				{
					foreach (ExecutorMergeDetails item2 in _currDockDetails.ExecutorsMerge)
					{
						SceneTriggerExecutor sceneTriggerExecuter3 =
							ship3.GetStructureObject<SceneTriggerExecutor>(item2.ParentTriggerID.InSceneID);
						if (sceneTriggerExecuter3 != null)
						{
							if (sceneTriggerExecuter3.ParentExecutor != null)
							{
								sceneTriggerExecuter3.ParentExecutor.SetChild(null, isInitialize);
							}
							else if (sceneTriggerExecuter3.ChildExecutor != null)
							{
								sceneTriggerExecuter3.SetChild(null, isInitialize);
							}
						}
					}
				}
			}

			if (_currDockDetails.PairedDoors != null && _currDockDetails.PairedDoors.Count > 0)
			{
				foreach (PairedDoorsDetails pairedDoor in _currDockDetails.PairedDoors)
				{
					if (pairedDoor.DoorID == null)
					{
						continue;
					}

					SpaceObjectVessel vessel = World.GetVessel(pairedDoor.DoorID.VesselGUID);
					if (vessel == null)
					{
						continue;
					}

					SceneDoor structureObject = vessel.GetStructureObject<SceneDoor>(pairedDoor.DoorID.InSceneID);
					if (structureObject == null || structureObject == null)
					{
						continue;
					}

					if (pairedDoor.PairedDoorID == null || pairedDoor.DoorID == null)
					{
						continue;
					}

					Ship ship4 = pairedDoor.PairedDoorID.VesselGUID != Guid
						? World.GetVessel(pairedDoor.PairedDoorID.VesselGUID) as Ship
						: this;
					Ship ship5 = pairedDoor.DoorID.VesselGUID != Guid
						? World.GetVessel(pairedDoor.DoorID.VesselGUID) as Ship
						: this;
					if (!isDock)
					{
						continue;
					}

					try
					{
						SceneDoor structureObject2 =
							ship4.GetStructureObject<SceneDoor>(pairedDoor.PairedDoorID.InSceneID);
						SceneDoor structureObject3 = ship5.GetStructureObject<SceneDoor>(pairedDoor.DoorID.InSceneID);
						float num = Vector3.Distance(structureObject2.DoorPassageTrigger.transform.position,
							structureObject3.DoorPassageTrigger.transform.position);
						if (structureObject2.DockingDoorPatch != null)
						{
							Destroy(structureObject2.DockingDoorPatch);
						}

						if (structureObject3.DockingDoorPatch != null)
						{
							Destroy(structureObject3.DockingDoorPatch);
						}

						structureObject2.DockingDoorPatch = Instantiate(
							structureObject2.DoorPassageTrigger.gameObject,
							structureObject2.Room1 != null
								? structureObject2.Room1.transform
								: !(structureObject2.Room2 != null) ? null : structureObject2.Room2.transform);
						structureObject2.DockingDoorPatch.transform.position =
							structureObject2.DoorPassageTrigger.transform.position;
						structureObject2.DockingDoorPatch.transform.rotation =
							structureObject2.DoorPassageTrigger.transform.rotation;
						GameObject dockingDoorPatch = structureObject2.DockingDoorPatch;
						dockingDoorPatch.name = dockingDoorPatch.name + structureObject2.InSceneID + "_patch";
						BoxCollider component = structureObject2.DockingDoorPatch.GetComponent<BoxCollider>();
						component.size = new Vector3(component.size.x, component.size.y, (num + component.size.z) / 2f);
						component.center = new Vector3(component.center.x, component.center.y,
							component.center.z + num / 4f);
						component.enabled = true;
						structureObject2.DockingDoorPatch.AddComponent<SceneTriggerRoomSegment>().BaseRoom =
							structureObject2.Room1 != null
								? structureObject2.Room1
								: !(structureObject2.Room2 != null) ? null : structureObject2.Room2;
						structureObject2.DockingDoorPatch.tag = "Ignore";
						structureObject3.DockingDoorPatch = Instantiate(
							structureObject3.DoorPassageTrigger.gameObject,
							structureObject3.Room1 != null
								? structureObject3.Room1.transform
								: !(structureObject3.Room2 != null) ? null : structureObject3.Room2.transform);
						structureObject3.DockingDoorPatch.transform.position =
							structureObject3.DoorPassageTrigger.transform.position;
						structureObject3.DockingDoorPatch.transform.rotation =
							structureObject3.DoorPassageTrigger.transform.rotation;
						GameObject dockingDoorPatch2 = structureObject3.DockingDoorPatch;
						dockingDoorPatch2.name = dockingDoorPatch2.name + structureObject3.InSceneID + "_patch";
						component = structureObject3.DockingDoorPatch.GetComponent<BoxCollider>();
						component.size = new Vector3(component.size.x, component.size.y, (num + component.size.z) / 2f);
						component.center = new Vector3(component.center.x, component.center.y,
							component.center.z + num / 4f);
						component.enabled = true;
						structureObject3.DockingDoorPatch.AddComponent<SceneTriggerRoomSegment>().BaseRoom =
							structureObject3.Room1 != null
								? structureObject3.Room1
								: !(structureObject3.Room2 != null) ? null : structureObject3.Room2;
						structureObject3.DockingDoorPatch.tag = "Ignore";
					}
					catch
					{
					}
				}
			}

			if (!isDock)
			{
				SceneDockingPort structureObject4 = World.GetVessel(_currDockDetails.ID.VesselGUID)
					.GetStructureObject<SceneDockingPort>(_currDockDetails.ID.InSceneID);
				SceneDockingPort structureObject5 = World.GetVessel(_currDockDetails.DockedToID.VesselGUID)
					.GetStructureObject<SceneDockingPort>(_currDockDetails.DockedToID.InSceneID);
				if (structureObject4 != null)
				{
					foreach (SceneDoor door in structureObject4.Doors)
					{
						if (door.DockingDoorPatch != null)
						{
							Destroy(door.DockingDoorPatch);
						}
					}
				}

				if (structureObject5 != null)
				{
					foreach (SceneDoor door2 in structureObject5.Doors)
					{
						if (door2.DockingDoorPatch != null)
						{
							Destroy(door2.DockingDoorPatch);
						}
					}
				}
			}

			MyPlayer.Instance.EnableTransitionTrigger();
			if (isDock)
			{
				SetRcsThrustersCenterOfMass(this, resetIsOn: true);
			}
			else
			{
				Ship ship6 = _currDockDetails.ID.VesselGUID != Guid
					? World.GetVessel(_currDockDetails.ID.VesselGUID) as Ship
					: this;
				Ship ship7 = _currDockDetails.DockedToID.VesselGUID != Guid
					? World.GetVessel(_currDockDetails.DockedToID.VesselGUID) as Ship
					: this;
				if (ship6 != null)
				{
					SetRcsThrustersCenterOfMass(ship6, resetIsOn: true);
				}

				if (ship7 != null)
				{
					SetRcsThrustersCenterOfMass(ship7, resetIsOn: true);
				}
			}

			_currDockDetails = null;
			if (MyPlayer.Instance.ShipControlMode == ShipControlMode.Docking)
			{
				World.InWorldPanels.Docking.UpdateDockingPorts();
			}
		}

		private static void SetRcsThrustersCenterOfMass(Ship sh, bool resetIsOn)
		{
			Ship ship = sh;
			if (sh.DockedToMainVessel != null)
			{
				ship = sh.DockedToMainVessel as Ship;
			}

			if (!(ship != null))
			{
				return;
			}

			if (ship._rcsThrusters != null)
			{
				ship._rcsThrusters.CenterOfMass = ship.transform;
			}

			foreach (Ship allDockedVessel in ship.AllDockedVessels.Cast<Ship>())
			{
				if (allDockedVessel != null && allDockedVessel._rcsThrusters != null)
				{
					allDockedVessel._rcsThrusters.CenterOfMass = ship.transform;
				}
			}
		}

		public void DockToShip(SceneDockingPort myPort, Ship dockToShip, SceneDockingPort dockToPort,
			SceneDockingPortDetails details, bool isInitialize)
		{
			_currDockDetails = details;
			SpaceObjectVessel spaceObjectVessel = !dockToShip.IsDocked ? dockToShip : dockToShip.DockedToMainVessel;
			spaceObjectVessel.RecreateDockedVesselsTree();
			if (MyPlayer.Instance.Parent is Ship && (MyPlayer.Instance.Parent == DockedToMainVessel ||
													 DockedToMainVessel.AllDockedVessels.Contains(
														 MyPlayer.Instance.Parent as SpaceObjectVessel)))
			{
				MyPlayer.Instance.DisableTransitionTrigger();
			}

			if (details.RelativePositionUpdate != null && details.RelativeRotationUpdate != null)
			{
				foreach (KeyValuePair<long, float[]> item in details.RelativePositionUpdate)
				{
					SpaceObjectVessel vessel = World.GetVessel(item.Key);
					vessel.RelativePosition = item.Value.ToVector3();
					vessel.RelativeRotation = details.RelativeRotationUpdate[item.Key].ToQuaternion();
				}
			}

			if (isInitialize && details.RelativePosition != null && details.RelativeRotation != null)
			{
				RelativePosition = details.RelativePosition.ToVector3();
				RelativeRotation = details.RelativeRotation.ToQuaternion();
			}

			transform.parent = dockToPort.transform;
			foreach (SpaceObjectVessel allDockedVessel in DockedToMainVessel.AllDockedVessels)
			{
				if (allDockedVessel != this && allDockedVessel.transform.parent !=
					allDockedVessel.DockedToVessel.ConnectedObjectsRoot.transform)
				{
					allDockedVessel.transform.parent = allDockedVessel.DockedToVessel.ConnectedObjectsRoot.transform;
				}
			}

			Ship ship = DockedToMainVessel as Ship;
			Vector3 vector = ship.CollidersCenterOffset.ToVector3() -
							 details.CollidersCenterOffset.ToVector3();
			Vector3 vector2 = ship.transform.rotation * vector;
			ship.ModifyPositionAndRotation(-vector2);
			ship.CollidersCenterOffset = details.CollidersCenterOffset;
			ship.GeometryPlaceholder.transform.localPosition = -ship.CollidersCenterOffset.ToVector3();
			ship.ConnectedObjectsRoot.transform.localPosition = -ship.CollidersCenterOffset.ToVector3();
			transform.position += GeometryPlaceholder.transform.localPosition;
			GeometryPlaceholder.transform.localPosition = Vector3.zero;
			ship.UpdateArtificialBodyPosition(updateChildren: false);
			foreach (SpaceObjectVessel allDockedVessel2 in DockedToMainVessel.AllDockedVessels)
			{
				Ship ship2 = allDockedVessel2 as Ship;
				ship2.GeometryPlaceholder.transform.localPosition = Vector3.zero;
				if (allDockedVessel2 != this)
				{
					ship2.transform.SetLocalPositionAndRotation(ship2.RelativePosition, ship2.RelativeRotation);
					ship2.UpdateArtificialBodyPosition(updateChildren: false);
				}

				ship2.ConnectedObjectsRoot.transform.Reset();
			}

			transform.parent = dockToShip.ConnectedObjectsRoot.transform;
			DockedToMainVessel.transform.position -= vector2;
			if (MyPlayer.Instance.Parent is Ship && (MyPlayer.Instance.Parent == DockedToMainVessel ||
													 DockedToMainVessel.AllDockedVessels.Contains(
														 MyPlayer.Instance.Parent as SpaceObjectVessel)))
			{
				MyPlayer.Instance.SendDockUndockMsg = true;
			}

			if (!isInitialize)
			{
				StartCoroutine(LerpDock(RelativePosition, RelativeRotation));
			}
			else
			{
				if (MyPlayer.Instance.Parent is Ship && (MyPlayer.Instance.Parent == DockedToMainVessel ||
														 DockedToMainVessel.AllDockedVessels.Contains(
															 MyPlayer.Instance.Parent as SpaceObjectVessel)))
				{
					transform.SetLocalPositionAndRotation(RelativePosition, RelativeRotation);
					SetTargetPositionAndRotation(transform.localPosition, transform.localRotation, instant: true);
					MyPlayer.Instance.SendDockUndockMsg = true;
				}
				else
				{
					transform.SetLocalPositionAndRotation(RelativePosition, RelativeRotation);
					SetTargetPositionAndRotation(transform.localPosition, transform.localRotation, instant: true);
				}

				UpdateArtificialBodyPosition(updateChildren: false);
				DockUndockCompleted(isDock: true, isInitialize: true);
				ZeroOcclusion.CheckOcclusionFor(MainVessel, onlyCheckDistance: false);
				foreach (SpaceObjectVessel allDockedVessel3 in MainVessel.AllDockedVessels)
				{
					ZeroOcclusion.CheckOcclusionFor(allDockedVessel3, onlyCheckDistance: false);
				}
			}

			if (RCS != null)
			{
				RCS.Status = SystemStatus.Offline;
			}

			if (dockToShip.RCS != null)
			{
				dockToShip.RCS.Status = SystemStatus.Offline;
			}
		}

		private IEnumerator LerpDock(Vector3 targetPos, Quaternion targetRot)
		{
			Vector3 startingPosition = transform.localPosition;
			Quaternion startingRotation = transform.localRotation;

			_lerpTimer = 0f;
			while (_lerpTimer < 1f)
			{
				if (MyPlayer.Instance.Parent is Ship && (MyPlayer.Instance.Parent == DockedToMainVessel ||
														 DockedToMainVessel.AllDockedVessels.Contains(
															 MyPlayer.Instance.Parent as SpaceObjectVessel)))
				{
					transform.SetLocalPositionAndRotation(
Vector3.Lerp(startingPosition, targetPos, Mathf.SmoothStep(0f, 1f, _lerpTimer)),
Quaternion.Lerp(startingRotation, targetRot, Mathf.SmoothStep(0f, 1f, _lerpTimer)));
					SetTargetPositionAndRotation(transform.localPosition, transform.localRotation, instant: true);
				}
				else
				{
					transform.SetLocalPositionAndRotation(
Vector3.Lerp(startingPosition, targetPos, Mathf.SmoothStep(0f, 1f, _lerpTimer)),
Quaternion.Lerp(startingRotation, targetRot, Mathf.SmoothStep(0f, 1f, _lerpTimer)));
					SetTargetPositionAndRotation(transform.localPosition, transform.localRotation, instant: true);
				}

				UpdateArtificialBodyPosition(updateChildren: false);

				_lerpTimer += Time.deltaTime * 0.5f;
				yield return new WaitForEndOfFrame();
			}

			if (MyPlayer.Instance.Parent is Ship && (MyPlayer.Instance.Parent == DockedToMainVessel ||
													 DockedToMainVessel.AllDockedVessels.Contains(
														 MyPlayer.Instance.Parent as SpaceObjectVessel)))
			{
				transform.SetLocalPositionAndRotation(RelativePosition, RelativeRotation);
			}
			else
			{
				transform.SetLocalPositionAndRotation(RelativePosition, RelativeRotation);
			}

			DockUndockCompleted(isDock: true, isInitialize: false);
		}

		public void UndockFromShip(SceneDockingPort myPort, Ship dockedToShip, SceneDockingPort dockedToPort,
			SceneDockingPortDetails details)
		{
			if (myPort == null || dockedToPort == null || !dockedToShip)
			{
				return;
			}

			_currDockDetails = details;
			myPort.LeverPulse = false;
			dockedToPort.LeverPulse = false;
			if (OnUndockStarted != null)
			{
				OnUndockStarted(isInitialize: false);
				OnUndockStarted = null;
			}

			DockedVessels.Remove(dockedToShip);
			dockedToShip.DockedVessels.Remove(this);
			DockedToVessel = null;
			dockedToShip.DockedToVessel = null;
			SpaceObjectVessel spaceObjectVessel =
				!(DockedToMainVessel != null) ? dockedToShip.DockedToMainVessel : DockedToMainVessel;
			spaceObjectVessel.transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
			Vector3 vector = (spaceObjectVessel as Ship).CollidersCenterOffset.ToVector3();
			Quaternion value = spaceObjectVessel.transform.rotation;
			spaceObjectVessel.ResetDockedToVessel();
			Ship vessel = World.GetVessel(details.VesselOrbit.GUID.Value) as Ship;
			Ship vessel2 = World.GetVessel(details.VesselOrbitOther.GUID.Value) as Ship;
			vessel.RecreateDockedVesselsTree();
			vessel2.RecreateDockedVesselsTree();
			vessel.transform.SetParent(World.SpaceObjectsRoot.transform);
			vessel2.transform.SetParent(World.SpaceObjectsRoot.transform);
			Vector3 vector2 = Vector3.zero;
			Vector3 vector3 = Vector3.zero;
			if (details.RelativePositionUpdate != null && details.RelativeRotationUpdate != null)
			{
				foreach (KeyValuePair<long, float[]> item in details.RelativePositionUpdate)
				{
					SpaceObjectVessel vessel3 = World.GetVessel(item.Key);
					if (item.Key == vessel.Guid)
					{
						Quaternion value2 = details.RelativeRotationUpdate[item.Key].ToQuaternion();
						vessel3.RelativePosition = Vector3.zero;
						vessel3.RelativeRotation = Quaternion.identity;
						vessel3.SetTargetPositionAndRotation(null, value2, instant: true);
						vector2 = details.RelativePositionUpdate[item.Key].ToVector3();
					}
					else if (item.Key == vessel2.Guid)
					{
						Quaternion value3 = details.RelativeRotationUpdate[item.Key].ToQuaternion();
						vessel3.RelativePosition = Vector3.zero;
						vessel3.RelativeRotation = Quaternion.identity;
						vessel3.SetTargetPositionAndRotation(null, value3, instant: true);
						vector3 = details.RelativePositionUpdate[item.Key].ToVector3();
					}
					else
					{
						vessel3.RelativePosition = item.Value.ToVector3();
						vessel3.RelativeRotation = details.RelativeRotationUpdate[item.Key].ToQuaternion();
					}
				}
			}

			foreach (SpaceObjectVessel allDockedVessel in vessel.AllDockedVessels)
			{
				if (allDockedVessel != this && allDockedVessel.transform.parent !=
					allDockedVessel.DockedToVessel.ConnectedObjectsRoot.transform)
				{
					allDockedVessel.transform.parent = allDockedVessel.DockedToVessel.ConnectedObjectsRoot.transform;
				}
			}

			foreach (SpaceObjectVessel allDockedVessel2 in vessel2.AllDockedVessels)
			{
				if (allDockedVessel2 != this && allDockedVessel2.transform.parent !=
					allDockedVessel2.DockedToVessel.ConnectedObjectsRoot.transform)
				{
					allDockedVessel2.transform.parent = allDockedVessel2.DockedToVessel.ConnectedObjectsRoot.transform;
				}
			}

			vessel.CollidersCenterOffset = details.CollidersCenterOffset.ToArray();
			vessel.GeometryPlaceholder.transform.localPosition = -vessel.CollidersCenterOffset.ToVector3();
			vessel.ConnectedObjectsRoot.transform.localPosition = -vessel.CollidersCenterOffset.ToVector3();
			vessel.UpdateArtificialBodyPosition(updateChildren: false);
			foreach (SpaceObjectVessel allDockedVessel3 in vessel.AllDockedVessels)
			{
				Ship ship = allDockedVessel3 as Ship;
				ship.GeometryPlaceholder.transform.localPosition = Vector3.zero;
				ship.transform.SetLocalPositionAndRotation(ship.RelativePosition, ship.RelativeRotation);
				ship.ConnectedObjectsRoot.transform.Reset();
				ship.UpdateArtificialBodyPosition(updateChildren: false);
			}

			vessel2.CollidersCenterOffset = details.CollidersCenterOffsetOther.ToArray();
			vessel2.GeometryPlaceholder.transform.localPosition = -vessel2.CollidersCenterOffset.ToVector3();
			vessel2.ConnectedObjectsRoot.transform.localPosition =
				-vessel2.CollidersCenterOffset.ToVector3();
			vessel2.UpdateArtificialBodyPosition(updateChildren: false);
			foreach (SpaceObjectVessel allDockedVessel4 in vessel2.AllDockedVessels)
			{
				Ship ship2 = allDockedVessel4 as Ship;
				ship2.GeometryPlaceholder.transform.localPosition = Vector3.zero;
				ship2.transform.SetLocalPositionAndRotation(ship2.RelativePosition, ship2.RelativeRotation);
				ship2.ConnectedObjectsRoot.transform.Reset();
				ship2.UpdateArtificialBodyPosition(updateChildren: false);
			}

			vessel.transform.position = position + rotation * (-vector + vector2 + value.Inverse() *
				vessel.transform.rotation * vessel.CollidersCenterOffset.ToVector3());
			vessel2.transform.position = position + rotation * (-vector + vector3 + value.Inverse() *
				vessel2.transform.rotation * vessel2.CollidersCenterOffset.ToVector3());
			vessel.SetTargetPositionAndRotation(vessel.transform.localPosition, null, instant: true);
			vessel2.SetTargetPositionAndRotation(vessel2.transform.localPosition, null, instant: true);
			if (MyPlayer.Instance.Parent is Ship ship3
				&& (ship3 == vessel || vessel.AllDockedVessels.Contains(ship3)
					|| ship3 == vessel2 || vessel2.AllDockedVessels.Contains(ship3)))
			{
				MyPlayer.Instance.SendDockUndockMsg = true;
			}

			World.SolarSystem.CenterPlanets();
			MyPlayer.Instance.UpdateCameraPositions();
			DockUndockCompleted(isDock: false, isInitialize: false);
			ZeroOcclusion.CheckOcclusionFor(MainVessel, onlyCheckDistance: false);
			foreach (SpaceObjectVessel allDockedVessel5 in MainVessel.AllDockedVessels)
			{
				ZeroOcclusion.CheckOcclusionFor(allDockedVessel5, onlyCheckDistance: false);
			}

			ZeroOcclusion.CheckOcclusionFor(dockedToShip.MainVessel, onlyCheckDistance: false);
			foreach (SpaceObjectVessel allDockedVessel6 in dockedToShip.MainVessel.AllDockedVessels)
			{
				ZeroOcclusion.CheckOcclusionFor(allDockedVessel6, onlyCheckDistance: false);
			}

		}

		public override void OnSceneLoaded()
		{
			base.OnSceneLoaded();
			_engineThrusters = GeometryRoot.GetComponentInChildren<EngineThrusters>(includeInactive: true);
			if (_engineThrusters != null)
			{
				_engineThrusters.OnOff = EngineOnLine;
			}

			_warpEffect = GeometryRoot.GetComponentInChildren<WarpEffect>(includeInactive: true);
			if (_warpEffect != null)
			{
				_warpEffect.SetActive(IsWarpOnline, instant: true);
			}

			_warpInductorExecutor = GeometryRoot.GetComponentInChildren<WarpInductorExecutor>(includeInactive: true);
			if (_warpInductorExecutor != null)
			{
				_warpInductorExecutor.ToggleInductor(isActive: false, isInstant: false);
			}

			_warpStartEffect = GeometryRoot.GetComponentInChildren<WarpStartEffect>(includeInactive: true);
			if (_warpStartEffect != null)
			{
				_warpStartEffect.gameObject.Activate(value: false);
			}

			_warpEndEffect = GeometryRoot.GetComponentInChildren<WarpEndEffect>(includeInactive: true);
			if (_warpEndEffect != null)
			{
				_warpEndEffect.gameObject.Activate(value: false);
			}

			if (SecuritySystem != null)
			{
				SecuritySystem.UpdateUI();
			}

			_refuelingStation = GeometryRoot.GetComponentInChildren<RefuelingStationUI>(includeInactive: true);
			_rcsThrusters = GeometryRoot.GetComponentInChildren<RCSThrusters>(includeInactive: true);
			if (_rcsThrusters != null && !IsDocked)
			{
				_rcsThrusters.CenterOfMass = transform;
			}

			OptimizationColliders = new List<Collider>();
			DontOptimizeColliders = new List<Collider>();
			Collider[] componentsInChildren = GeometryRoot.GetComponentsInChildren<Collider>();
			foreach (Collider collider in componentsInChildren)
			{
				if (collider.enabled && !collider.CompareTag("Ignore") &&
					!(collider.GetComponent<SceneDockingTrigger>() != null) &&
					(!(collider.GetComponentInParent<BaseSceneAttachPoint>() != null) ||
					 !(collider.GetComponentInParent<BaseSceneAttachPoint>().Collider == collider)))
				{
					if (!collider.CompareTag("DontOptimize"))
					{
						OptimizationColliders.Add(collider);
					}
					else
					{
						DontOptimizeColliders.Add(collider);
					}
				}
			}

			ZeroOcclusion.AddOccludersFrom(this);
			if (!SceneHelper.CompareTags(Tag, "_RescueVessel") ||
				MyPlayer.Instance.IsInVesselHierarchy(this) || !(_warpEndEffect != null))
			{
				return;
			}

			WarpEndEffectTask = new Action(delegate
			{
				ActivateWarpEndEffect();
				WarpEndEffectTask = null;
			});
		}

		public override void OnStabilizationChanged(bool isStabilized)
		{
			if (!IsMainVessel)
			{
				return;
			}

			foreach (SpaceObjectVessel allDockedVessel in MainVessel.AllDockedVessels)
			{
				allDockedVessel.OnStabilizationChanged(isStabilized);
			}
		}

		public override bool IsPlayerAuthorized(Player pl)
		{
			return IsPlayerAuthorized(pl, new HashSet<SpaceObjectVessel>(), new HashSet<SecuritySystem>());
		}

		public override bool IsPlayerAuthorizedOrNoSecurity(Player pl)
		{
			HashSet<SecuritySystem> hashSet = new HashSet<SecuritySystem>();
			return IsPlayerAuthorized(pl, new HashSet<SpaceObjectVessel>(), hashSet) || hashSet.Count == 0;
		}

		public override bool IsPlayerAuthorizedOrFreeSecurity(Player pl)
		{
			HashSet<SecuritySystem> hashSet = new HashSet<SecuritySystem>();
			return IsPlayerAuthorized(pl, new HashSet<SpaceObjectVessel>(), hashSet) || hashSet.Count == 0 ||
				   hashSet.Count((SecuritySystem m) => m.AuthorizedPlayers.Count == 0) == hashSet.Count;
		}

		private bool IsPlayerAuthorized(Player pl, HashSet<SpaceObjectVessel> traversedVessels,
			HashSet<SecuritySystem> securitySystems)
		{
			if (!traversedVessels.Add(this))
			{
				return false;
			}

			if (SecuritySystem != null)
			{
				securitySystems.Add(SecuritySystem);
			}

			// Player is authorised on this ship.
			bool flag = SecuritySystem != null &&
						SecuritySystem.AuthorizedPlayers.Find((AuthorizedPerson m) => m.PlayerId == pl.PlayerId) !=
						null;
			if (GameScenes.Ranges.IsShip(SceneId) || flag)
			{
				return flag;
			}

			// Player is authorised in docked vessel.
			if (DockedToVessel != null && DockedToVessel is Ship && !GameScenes.Ranges.IsShip(DockedToVessel.SceneId) &&
				(DockedToVessel as Ship).IsPlayerAuthorized(pl, traversedVessels, securitySystems))
			{
				return true;
			}

			foreach (SpaceObjectVessel dockedVessel in DockedVessels)
			{
				if (dockedVessel is Ship && !GameScenes.Ranges.IsShip(dockedVessel.SceneId) &&
					(dockedVessel as Ship).IsPlayerAuthorized(pl, traversedVessels, securitySystems))
				{
					return true;
				}
			}

			return false;
		}

		public void ProximityCanvasCheck()
		{
			Canvas[] componentsInChildren = GetComponentsInChildren<Canvas>(includeInactive: true);
			foreach (Canvas canvas in componentsInChildren)
			{
				float magnitude = (MyPlayer.Instance.transform.position - canvas.transform.position).magnitude;
				canvas.gameObject.Activate(magnitude < MyPlayer.Instance.HideCanvasDistance);
			}

			foreach (DecalProjector decal in GetComponentsInChildren<DecalProjector>(includeInactive: true))
			{
				float magnitude2 = (MyPlayer.Instance.transform.position - decal.transform.position).magnitude;
				decal.enabled = magnitude2 < MyPlayer.Instance.HideCanvasDistance;
			}
		}

		public void ToggleGatheringAtmosphere(bool? isGathering = null)
		{
			if (isGathering.HasValue)
			{
				GatherAtmos = isGathering.HasValue;
			}
			else
			{
				GatherAtmos = !GatherAtmos;
			}

			if (_shipStatsMsg == null)
			{
				_shipStatsMsg = new ShipStatsMessage();
			}

			_shipStatsChanged = true;
		}

		public override void ChangeStats(Vector3? thrust = null, Vector3? rotation = null,
			Vector3? autoStabilize = null, float? engineThrustPercentage = null, SubSystemDetails subSystem = null,
			GeneratorDetails generator = null, RoomDetails roomTrigger = null, DoorDetails door = null,
			SceneTriggerExecutorDetails sceneTriggerExecutor = null, SceneDockingPortDetails dockingPort = null,
			AttachPointDetails attachPoint = null, long? stabilizationTarget = null, SpawnPointStats spawnPoint = null,
			float? selfDestructTime = null, string emblemId = null)
		{
			_shipStatsMsg ??= new ShipStatsMessage
			{
				Guid = Guid,
				VesselObjects = new VesselObjects
				{
					SubSystems = new List<SubSystemDetails>(),
					Generators = new List<GeneratorDetails>(),
					RoomTriggers = new List<RoomDetails>(),
					Doors = new List<DoorDetails>(),
					SceneTriggerExecutors = new List<SceneTriggerExecutorDetails>(),
					DockingPorts = new List<SceneDockingPortDetails>(),
					AttachPoints = new List<AttachPointDetails>(),
					SpawnPoints = new List<SpawnPointStats>()
				}
			};

			if (thrust.HasValue)
			{
				_inputThrust = thrust.Value;
			}

			if (rotation.HasValue)
			{
				_inputRotation = rotation.Value;
			}

			if (autoStabilize.HasValue)
			{
				_inputAutoStabilise = autoStabilize.Value;
			}

			if (engineThrustPercentage.HasValue)
			{
				EngineThrustPercentage = engineThrustPercentage.Value;
			}

			if (subSystem != null)
			{
				_shipStatsMsg.VesselObjects.SubSystems.Add(subSystem);
				_shipStatsChanged = true;
			}

			if (generator != null)
			{
				_shipStatsMsg.VesselObjects.Generators.Add(generator);
				_shipStatsChanged = true;
			}

			if (roomTrigger != null)
			{
				_shipStatsMsg.VesselObjects.RoomTriggers.Add(roomTrigger);
				_shipStatsChanged = true;
			}

			if (door != null)
			{
				_shipStatsMsg.VesselObjects.Doors.Add(door);
				_shipStatsChanged = true;
			}

			if (sceneTriggerExecutor != null)
			{
				_shipStatsMsg.VesselObjects.SceneTriggerExecutors.Add(sceneTriggerExecutor);
				_shipStatsChanged = true;
			}

			if (dockingPort != null)
			{
				_shipStatsMsg.VesselObjects.DockingPorts.Add(dockingPort);
				_shipStatsChanged = true;
			}

			if (attachPoint != null)
			{
				_shipStatsMsg.VesselObjects.AttachPoints.Add(attachPoint);
				_shipStatsChanged = true;
			}

			if (stabilizationTarget.HasValue)
			{
				_shipStatsMsg.TargetStabilizationGuid = stabilizationTarget.Value;
				_shipStatsChanged = true;
			}

			if (spawnPoint != null)
			{
				_shipStatsMsg.VesselObjects.SpawnPoints.Add(spawnPoint);
				_shipStatsChanged = true;
			}

			if (selfDestructTime.HasValue)
			{
				_shipStatsMsg.SelfDestructTime = selfDestructTime;
				_shipStatsChanged = true;
			}

			if (emblemId != null)
			{
				_shipStatsMsg.VesselObjects.EmblemId = emblemId;
				_shipStatsChanged = true;
			}
		}

		public override void DestroyGeometry()
		{
			base.DestroyGeometry();
			if (IsMainVessel)
			{
				foreach (SpaceObjectVessel allDockedVessel in AllDockedVessels)
				{
					allDockedVessel.DestroyGeometry();
				}
			}

			SceneObjectsLoaded = false;
		}

		private void OnDestroy()
		{
			this.CancelInvoke(ActivateDamagePoints);

			EventSystem.RemoveListener(typeof(ShipStatsMessage), ShipStatsMessageListener);
			EventSystem.RemoveListener(typeof(ManeuverCourseResponse), ManeuverCourseResponseListener);
			EventSystem.RemoveListener(typeof(VesselSecurityResponse), VesselSecurityResponseListener);
			EventSystem.RemoveListener(typeof(NameTagMessage), NameTagMessageListener);
			EventSystem.RemoveListener(typeof(VesselRequestResponse), VesselRequestResponseListener);
			EventSystem.RemoveListener(typeof(DestroyVesselMessage), DestroyVesselMessageListener);

			World.RemoveArtificialBody(this);
			SceneHelper.RemoveCubemapProbes(gameObject, World);
			World.ActiveVessels.TryRemove(Guid, out _);
		}
	}
}
