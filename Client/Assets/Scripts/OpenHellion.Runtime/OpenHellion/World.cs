using System;
using System.Collections.Generic;
using System.Linq;
using OpenHellion.Net;
using OpenHellion.Social;
using OpenHellion.Social.RichPresence;
using OpenHellion.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using ZeroGravity;
using ZeroGravity.Data;
using ZeroGravity.Effects;
using ZeroGravity.Math;
using ZeroGravity.LevelDesign;
using ZeroGravity.Network;
using ZeroGravity.Objects;
using ZeroGravity.ShipComponents;
using Cysharp.Threading.Tasks;
using OpenHellion.Net.Message;
using System.Collections.Concurrent;

namespace OpenHellion
{
	/// <summary>
	/// 	This is the main component of the game. Is the whole game manager when in world scene. It handles everything from saving to managing parts of the GUI.<br/>
	/// </summary>
	/// <seealso cref="MyPlayer"/>
	[RequireComponent(typeof(SolarSystem))]
	public class World : MonoBehaviour
	{
		[Title("Config")]
		public float RCS_THRUST_SENSITIVITY = 0.5f;

		public float RCS_ROTATION_SENSITIVITY = 5f;

		public static double CELESTIAL_BODY_RADIUS_MULTIPLIER = 1.0;

		public static float DROP_THRESHOLD = 0.2f;

		public static float DROP_MIN_FORCE = 0f;

		public static float DROP_MAX_FORCE = 8f;

		public static float DROP_MAX_TIME = 3f;





		public List<DebrisField> DebrisFields = new List<DebrisField>();

		[Title("Solar system and prefabs")]
		private SolarSystem _solarSystem;

		public GameObject SolarSystemRoot;

		public GameObject SpaceObjectsRoot;

		public Transform SunCameraRootTransform;

		public Transform SunCameraTransform;

		public Transform PlanetsCameraRootTransform;

		public Transform PlanetsCameraTransform;

		public Transform ShipSunLightTransform;

		public Transform PlanetsRootTransform;

		public Transform PlanetsSunLightTransform;

		public Vector3D OriginWorldPosition { get; private set; }

		public long AnchorGuid { get; private set; }

		/// <summary>
		/// 	The vessel the local player is flying, which is predicted locally instead of being driven by
		/// 	the movement stream. Zero when nobody here is piloting.
		/// </summary>
		public long PilotedVesselGuid;

		/// <summary>
		/// 	Displacement the pilot's thrust has added to the anchor since the last movement message, and
		/// 	the velocity it accumulated to get there. The anchor is pinned at the origin, so this reaches
		/// 	the screen as every other body sliding the opposite way.
		/// </summary>
		public Vector3 AnchorOffset;

		public Vector3 AnchorThrustVelocity;

		public RenderToCubeMap CubemapRenderer;

		public ZeroGravity.ShipComponents.Map Map;

		public DebrisFieldEffect DebrisEffect;

		public EffectPrefabs EffectPrefabs;

		public Texture2D DefaultCursor;

		[Title("Quests")]
		public QuestCollectionObject QuestCollection;

		[NonSerialized] public List<Quest> Quests = new List<Quest>();

		private volatile bool _logoutRequestSent;

		public IEnumerable<OtherPlayer> AllPlayers => _spaceObjects.Values.OfType<OtherPlayer>();

		public IEnumerable<DynamicObject> AllDynamicObjects => _spaceObjects.Values.OfType<DynamicObject>();

		public IEnumerable<Corpse> AllCorpses => _spaceObjects.Values.OfType<Corpse>();

		[NonSerialized] public readonly ConcurrentDictionary<long, SpaceObjectVessel> ActiveVessels = new();

		public IEnumerable<ArtificialBody> AllArtificialBodies => _spaceObjects.Values.OfType<ArtificialBody>();

		private readonly ConcurrentDictionary<long, SpaceObject> _spaceObjects = new();

		private readonly HashSet<long> _pendingSpawn = new();
		private readonly HashSet<long> _spawnInFlight = new();
		private readonly Dictionary<long, float> _failedSpawnTimes = new();
		private const float SpawnRetryCooldown = 5f;

		[NonSerialized] public List<ItemIngredientsData> ItemsIngredients;

		private bool _openMainSceneStarted;

		[NonSerialized] public readonly Dictionary<long, CharacterInteractionState> CharacterInteractionStatesQueue =
			new Dictionary<long, CharacterInteractionState>();

		[NonSerialized] public DateTime? ServerRestartTime;

		// TODO move to NetworkController
		private float _lastLatencyMessageTime = -1f;
		private volatile int _latencyMs;

		private long _lastAppliedParentGuid;


		public double ExposureRange;

		private float[] _vesselExposureValues;

		private float[] _playerExposureValues;

		[NonSerialized] public Action ActivatePlayerDelegate;

		[NonSerialized] public bool IsChatOpened;

		public InWorldPanels InWorldPanels;

		public InGameGUI InGameGUI;

		public static int DefaultLayerMask => 1 << LayerMask.NameToLayer("Default");

		public SolarSystem SolarSystem => _solarSystem;

		public int LatencyMs
		{
			get
			{
				if (_lastLatencyMessageTime < 0f)
				{
					return 0;
				}

				float num = Time.realtimeSinceStartup - _lastLatencyMessageTime;
				if (_latencyMs < 0 || num > 5f)
				{
					return (int)(num * 1000f);
				}

				return _latencyMs;
			}
		}


		private void Awake()
		{
			RCS_THRUST_SENSITIVITY = Properties.GetProperty("rcs_thrust_sensitivity", RCS_THRUST_SENSITIVITY);
			RCS_ROTATION_SENSITIVITY = Properties.GetProperty("rcs_rotation_sensitivity", RCS_ROTATION_SENSITIVITY);
			CELESTIAL_BODY_RADIUS_MULTIPLIER =
				Properties.GetProperty("celestial_body_radius_multiplier", CELESTIAL_BODY_RADIUS_MULTIPLIER);
			DROP_THRESHOLD = Properties.GetProperty("drop_threshold", DROP_THRESHOLD);
			DROP_MIN_FORCE = Properties.GetProperty("drop_min_force", DROP_MIN_FORCE);
			DROP_MAX_FORCE = Properties.GetProperty("drop_max_force", DROP_MAX_FORCE);
			DROP_MAX_TIME = Properties.GetProperty("drop_max_time", DROP_MAX_TIME);

			StaticData.LoadData();
			Application.runInBackground = true;
			_openMainSceneStarted = false;
			_solarSystem = GetComponent<SolarSystem>();
			Texture[] emblems = Resources.LoadAll<Texture>("Emblems");
			SceneVesselEmblem.Textures = emblems.ToDictionary(x => x.name, y => y);

			Globals.Instance.OnHellionQuit += () =>
			{
				OnDestroy();
			};
		}

		private void Start()
		{
			InWorldPanels.LocalizePanels();

			EventSystem.AddListener(typeof(KillPlayerMessage), KillPlayerMessageListener);
			EventSystem.AddListener(typeof(LogOutResponse), LogOutResponseListener);
			EventSystem.AddListener(typeof(DestroyObjectMessage), DestroyObjectMessageListener);
			EventSystem.AddListener(typeof(MovementMessage), MovementMessageListener);
			EventSystem.AddListener(typeof(ShipThrustStateMessage), PilotStateMessageListener);
			EventSystem.AddListener<PlayersOnServerResponse>(PlayersOnServerResponseListener);
			EventSystem.AddListener(typeof(ShipCollisionMessage), ShipCollisionMessageListener);
			EventSystem.AddListener(typeof(UpdateVesselDataMessage), UpdateVesselDataMessageListener);

			Settings.LoadSettings(Settings.SettingsType.Game);
		}

		private void ShipCollisionMessageListener(NetworkData data)
		{
			ShipCollisionMessage shipCollisionMessage = data as ShipCollisionMessage;
			if (GetVessel(shipCollisionMessage.ShipOne) != MyPlayer.Instance.Parent &&
			    (shipCollisionMessage.ShipTwo == -1 ||
			     GetVessel(shipCollisionMessage.ShipTwo) != MyPlayer.Instance.Parent) ||
			    shipCollisionMessage.CollisionVelocity <= float.Epsilon)
			{
				return;
			}

			MyPlayer.Instance.FpsController.CameraController.cameraShakeController.CamShake(0.8f, 0.3f, 15f, 15f,
				useSparks: true);
			VesselHealthSounds[] healthSoundsArray = GetVessel(shipCollisionMessage.ShipOne).GeometryRoot
				.GetComponentsInChildren<VesselHealthSounds>();
			foreach (VesselHealthSounds vesselHealthSounds in healthSoundsArray)
			{
				vesselHealthSounds.PlaySounds();
			}

			if (shipCollisionMessage.ShipTwo != -1)
			{
				VesselHealthSounds[] vesselHealthSoundsArray = GetVessel(shipCollisionMessage.ShipTwo).GeometryRoot
					.GetComponentsInChildren<VesselHealthSounds>();
				foreach (VesselHealthSounds vesselHealthSounds in vesselHealthSoundsArray)
				{
					vesselHealthSounds.PlaySounds();
				}
			}
		}

		private void LogOutResponseListener(NetworkData data)
		{
			LogOutResponse logOutResponse = data as LogOutResponse;
			if (logOutResponse is null || logOutResponse.Status == NetworkData.MessageStatus.Failure)
			{
				Debug.LogError("Failed to log out properly");
			}

			ReturnToMainMenu();
		}

		private void DestroyObjectMessageListener(NetworkData data)
		{
			DestroyObjectMessage destroyObjectMessage = data as DestroyObjectMessage;
			SpaceObject obj = GetObject(destroyObjectMessage.ID, destroyObjectMessage.ObjectType);
			if (obj == null || obj.Type is SpaceObjectType.PlayerPivot
				or SpaceObjectType.DynamicObjectPivot or SpaceObjectType.CorpsePivot)
			{
				return;
			}
			DespawnObject(obj);
		}

		// Starts logging out.
		public void LogOut()
		{
			GlobalGUI.ShowLoadingScreen(GlobalGUI.LoadingScreenType.Loading);

			if (!_logoutRequestSent)
			{
				_logoutRequestSent = true;
				NetworkController.SendAndForget(new LogOutRequest());
			}
		}

		/// <summary>
		/// 	Closes the current game, returns to main menu, and disconnects from the server.
		/// </summary>
		public void ReturnToMainMenu()
		{
			if (!_openMainSceneStarted)
			{
				Debug.Log("Returning to main menu...");
				_openMainSceneStarted = true;
				Globals.ToggleCursor(true);
				if (MyPlayer.Instance != null)
				{
					Destroy(MyPlayer.Instance.gameObject);
				}
				NetworkController.Disconnect();
				SceneManager.LoadScene("MainMenuScene", LoadSceneMode.Single);
				GlobalGUI.CloseLoadingScreen();
			}
		}

		public void ConnectionFailedListener()
		{
			Debug.LogError("Connection to server failed.");
			ReturnToMainMenu();
		}

		private void OnDestroy()
		{
			EventSystem.RemoveListener(typeof(KillPlayerMessage), KillPlayerMessageListener);
			EventSystem.RemoveListener(typeof(LogOutResponse), LogOutResponseListener);
			EventSystem.RemoveListener(typeof(DestroyObjectMessage), DestroyObjectMessageListener);
			EventSystem.RemoveListener(typeof(MovementMessage), MovementMessageListener);
			EventSystem.RemoveListener(typeof(ShipThrustStateMessage), PilotStateMessageListener);
			EventSystem.RemoveListener<PlayersOnServerResponse>(PlayersOnServerResponseListener);
			EventSystem.RemoveListener(typeof(ShipCollisionMessage), ShipCollisionMessageListener);
			EventSystem.RemoveListener(typeof(UpdateVesselDataMessage), UpdateVesselDataMessageListener);
		}

		private void KillPlayerMessageListener(NetworkData data)
		{
			KillPlayerMessage message = data as KillPlayerMessage;
			if (message.Guid != MyPlayer.Instance.Guid)
			{
				return;
			}

			MyPlayer.Instance.IsAlive = false;
			InGameGUI.ToggleDeadMsg(val: true);
			if (message.CauseOfDeath == HurtType.Shipwreck && message.VesselDamageType != 0)
			{
				InGameGUI.DeadMsgText.text = message.VesselDamageType.ToLocalizedString().ToUpper();
			}
			else
			{
				InGameGUI.DeadMsgText.text = message.CauseOfDeath.ToLocalizedString().ToUpper();
			}
		}

		public bool TryGetSpaceObject<T>(long guid, out T spaceObject) where T : SpaceObject
		{
			if (_spaceObjects.TryGetValue(guid, out SpaceObject found) && found != null && found is T typed)
			{
				spaceObject = typed;
				return true;
			}

			spaceObject = null;
			return false;
		}

		public void AddPlayer(long guid, OtherPlayer pl)
		{
			_spaceObjects.TryAdd(guid, pl);
		}

		public void RemovePlayer(long guid)
		{
			_spaceObjects.TryRemove(guid, out _);
		}

		public OtherPlayer GetPlayer(long guid)
		{
			if (_spaceObjects.TryGetValue(guid, out var value) && value is OtherPlayer player)
			{
				return player;
			}

			if (MyPlayer.Instance != null && MyPlayer.Instance.Parent != null &&
			    MyPlayer.Instance.Parent is SpaceObjectVessel)
			{
				OtherPlayer[] componentsInChildren = (MyPlayer.Instance.Parent as SpaceObjectVessel).MainVessel
					.GetComponentsInChildren<OtherPlayer>();
				foreach (OtherPlayer otherPlayer in componentsInChildren)
				{
					if (otherPlayer.Guid == guid)
					{
						Debug.LogWarning("Player not stored in space objects array, but exists in game: " + guid);
						AddPlayer(otherPlayer.Guid, otherPlayer);
						return otherPlayer;
					}
				}
			}

			OtherPlayer[] componentsInChildren2 = SpaceObjectsRoot.GetComponentsInChildren<OtherPlayer>();
			foreach (OtherPlayer otherPlayer2 in componentsInChildren2)
			{
				if (otherPlayer2.Guid == guid)
				{
					Debug.LogWarning("Player not stored in space objects array, but exists in game: " + guid);
					AddPlayer(otherPlayer2.Guid, otherPlayer2);
					return otherPlayer2;
				}
			}

			Debug.LogWarning("Could not find player in world with guid: " + guid);

			return null;
		}

		public void AddDynamicObject(long guid, DynamicObject obj)
		{
			_spaceObjects.TryAdd(guid, obj);
		}

		public void RemoveDynamicObject(long guid, DynamicObject obj)
		{
			// Safety check so pivot's arent accidentaly removed as they share guid.
			if (_spaceObjects.TryGetValue(guid, out SpaceObject registered) && registered == obj)
			{
				_spaceObjects.TryRemove(guid, out _);
			}
		}

		public DynamicObject GetDynamicObject(long guid)
		{
			if (!_spaceObjects.TryGetValue(guid, out SpaceObject value))
			{
				return null;
			}

			// If it is a pivot, get the child.
			return value as DynamicObject ?? (value as Pivot)?.GetComponentsInChildren<DynamicObject>().FirstOrDefault(m => m.Guid == guid);
		}

		public void AddCorpse(long guid, Corpse obj)
		{
			_spaceObjects.TryAdd(guid, obj);
		}

		public void RemoveCorpse(long guid)
		{
			_spaceObjects.TryRemove(guid, out _);
		}

		public Corpse GetCorpse(long guid)
		{
			if (_spaceObjects.TryGetValue(guid, out var corpse) && corpse is Corpse corpseObject)
			{
				return corpseObject;
			}

			if (MyPlayer.Instance != null && MyPlayer.Instance.Parent != null)
			{
				Corpse[] corpsesInGame = MyPlayer.Instance.Parent.GetComponentsInChildren<Corpse>();
				foreach (Corpse corpseInGame in corpsesInGame)
				{
					if (corpseInGame.Guid == guid)
					{
						Debug.LogWarning("Corpse not stored in space objects array, but exists in game: " + guid);
						AddCorpse(corpseInGame.Guid, corpseInGame);
						return corpseInGame;
					}
				}
			}

			Corpse[] componentsInChildren2 = SpaceObjectsRoot.GetComponentsInChildren<Corpse>();
			foreach (Corpse corpse2 in componentsInChildren2)
			{
				if (corpse2.Guid == guid)
				{
					Debug.LogWarning("Corpse not stored in space objects array, but exists in game: " + guid);
					AddCorpse(corpse2.Guid, corpse2);
					return corpse2;
				}
			}

			Debug.Log("Could not find corpse in world with guid: " + guid);

			return null;
		}

		public void AddArtificialBody(ArtificialBody body)
		{
			// A pivot shares the guid of the object it contains, so it displaces that object here.
			_spaceObjects.TryRemove(body.Guid, out _);
			_spaceObjects.TryAdd(body.Guid, body);
		}

		/// <summary>
		/// 	Drops an artificial body from the index. Pass <paramref name="restoreContained" /> when a
		/// 	pivot dissolves, so the object it wrapped reclaims the guid slot they share.
		/// </summary>
		/// TODO: restoreContained has to exist as long as pivots use the same guid as the objects they contain.
		public void RemoveArtificialBody(ArtificialBody body, SpaceObject restoreContained = null)
		{
			// Check if the artificial body isn't a pivot.
			if (body == null || !_spaceObjects.TryGetValue(body.Guid, out SpaceObject registered) ||
			    !ReferenceEquals(registered, body))
			{
				return;
			}

			_spaceObjects.TryRemove(body.Guid, out _);

			if (restoreContained != null)
			{
				_spaceObjects.TryAdd(body.Guid, restoreContained);
			}
		}

		public SpaceObjectVessel GetVessel(long guid)
		{
			if (_spaceObjects.TryGetValue(guid, out SpaceObject value) && value is SpaceObjectVessel spaceObject)
			{
				return spaceObject;
			}

			SpaceObjectVessel[] componentsInChildren = SpaceObjectsRoot.GetComponentsInChildren<SpaceObjectVessel>();
			foreach (SpaceObjectVessel spaceObjectVessel in componentsInChildren)
			{
				if (spaceObjectVessel.Guid == guid)
				{
					Debug.LogWarning("Vessel not stored in space objects array, but exists in game: " + guid);
					return spaceObjectVessel;
				}
			}

			Debug.Log("Could not find space object vessel in world with guid: " + guid);

			return null;
		}

		public void SendVesselRequest(SpaceObjectVessel obj, float time, GameScenes.SceneId sceneID, string tag)
		{
			NetworkController.SendAndForget(new VesselRequest
			{
				GUID = obj.Guid,
				Time = time,
				RescueShipSceneID = sceneID,
				RescueShipTag = tag
			});
		}

		public void SendDistressCall(ArtificialBody body, bool isDistressActive)
		{
			NetworkController.SendAndForget(new DistressCallRequest
			{
				GUID = body.Guid,
				IsDistressActive = isDistressActive
			});
		}

		private void PlayersOnServerResponseListener(NetworkData data)
		{
			try
			{
				PlayersOnServerResponse playersOnServerResponse = data as PlayersOnServerResponse;
				if (playersOnServerResponse.SpawnPointID != null)
				{
					GetVessel(playersOnServerResponse.SpawnPointID.VesselGUID)
						.GetStructureObject<SceneSpawnPoint>(playersOnServerResponse.SpawnPointID.InSceneID)
						.ParsePlayersOnServerResponse(playersOnServerResponse).Forget();
				}
				else if (playersOnServerResponse.SecuritySystemID != null)
				{
					(GetVessel(playersOnServerResponse.SecuritySystemID.VesselGUID) as Ship).SecuritySystem
						.ParsePlayersOnServerResponse(playersOnServerResponse);
				}
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}

		private void LateUpdate()
		{
			if (MyPlayer.Instance == null || !MyPlayer.Instance.PlayerReady) return;
			SolarSystem.UpdatePositions();
			SolarSystem.CenterPlanets();

			DrainSpawnQueue();

			RichPresenceManager.Update();
		}

		/// <summary>
		/// 	Authoritative state of the vessel the local player is flying, arriving far more often than a
		/// 	movement message. It corrects the prediction rather than replacing it.
		/// </summary>
		private void PilotStateMessageListener(NetworkData data)
		{
			if (data is not ShipThrustStateMessage message || MyPlayer.Instance == null ||
				!MyPlayer.Instance.PlayerReady || !MyPlayer.Instance.IsDrivingShip ||
				message.Rotation == null || message.AngularVelocity == null ||
				!TryGetSpaceObject(message.VesselGuid, out ArtificialBody vessel))
			{
				return;
			}

			// Orientation is eased on so a correction never shows as a jump, but the rate is adopted
			// outright because the server damps and stabilises in ways the prediction does not model.
			// Translation is left alone: the reported velocity is orbital velocity in world space, while
			// the client works in a frame that already moves with it, so the next movement message
			// rebases it instead.
			vessel.transform.localRotation = Quaternion.Slerp(vessel.transform.localRotation,
				message.Rotation.ToQuaternion(), ArtificialBody.Blend(Time.fixedDeltaTime));
			vessel.SetVelocity(vessel.Velocity, message.AngularVelocity.ToVector3());
		}

		// Caution: Executed very often and must stay synchronous.
		/// <summary>
 		/// 	Handles moving space objects, and queueing spawns for objects we don't know about yet.
 		/// 	Since planets are not space objects, they are moved elsewhere.
		/// </summary>
		private void MovementMessageListener(NetworkData data)
		{
			MovementMessage movementMessage = data as MovementMessage;

			if (movementMessage == null)
			{
				return;
			}

			if (MyPlayer.Instance == null || !MyPlayer.Instance.PlayerReady)
			{
				return;
			}

			try
			{
				if (MyPlayer.Instance.Parent == null)
				{
					return;
				}

				// A different anchor is a rebase.
				bool parentChanged = movementMessage.ParentGuid != _lastAppliedParentGuid;
				_lastAppliedParentGuid = movementMessage.ParentGuid;

				bool anchorChanged = AnchorGuid != movementMessage.AnchorGuid;
				if (anchorChanged)
				{
					AnchorGuid = movementMessage.AnchorGuid;
					ReOrigin();
				}

				if (movementMessage.OriginWorldPosition != null)
				{
					OriginWorldPosition = movementMessage.OriginWorldPosition.ToVector3D();

					// Every body is restated relative to the anchor as it was at this instant, so the
					// deviation predicted up to here is now accounted for.
					AnchorOffset = Vector3.zero;
					AnchorThrustVelocity = Vector3.zero;
				}

				MyPlayer.Instance.ProcessMovementMessage(
					movementMessage.ParentGuid,
					movementMessage.PlayerPosition?.ToVector3(),
					movementMessage.PlayerRotation?.ToQuaternion(),
					movementMessage.PlayerVelocity?.ToVector3(),
					movementMessage.PlayerAnimationData);

				if (movementMessage.ArtificialBodiesMovement != null)
				{
					foreach (MovementMessage.TransformInfo bodyTransform in movementMessage.ArtificialBodiesMovement)
					{
						if (!TryGetSpaceObject(bodyTransform.Guid, out ArtificialBody artificialBody))
						{
							RequestSpawn(bodyTransform.Guid);
							continue;
						}

						// Stabilisation couples position and nothing else.
						// Rotation needs to be added separately.
						bool stabilised = bodyTransform.StabiliseToTargetGuid > 0;

						if (bodyTransform.Rotation == null || bodyTransform.Velocity == null || bodyTransform.AngularVelocity == null ||
						    (stabilised ? bodyTransform.StabilisationOffset == null : bodyTransform.Position == null))
						{
							Debug.LogWarning($"Movement entry for '{bodyTransform.Guid}' had a null field.");
							continue;
						}

						if (stabilised)
						{
							// Places the body from its target, so the message's own position is not ours to use.
							artificialBody.StabilizeToTarget(bodyTransform.StabiliseToTargetGuid,
								bodyTransform.StabilisationOffset.ToVector3());
							artificialBody.SetTargetPositionAndRotation(null,
								bodyTransform.Rotation.ToQuaternion(), parentChanged || anchorChanged);
						}
						else
						{
							artificialBody.DisableStabilization();
							artificialBody.SetTargetPositionAndRotation(bodyTransform.Position.ToVector3(),
								bodyTransform.Rotation.ToQuaternion(), parentChanged || anchorChanged);
						}

						artificialBody.SetVelocity(bodyTransform.Velocity.ToVector3(), bodyTransform.AngularVelocity.ToVector3());

						// Drag any bodies stabilised to this one along with its updated position.
						foreach (ArtificialBody stabilizedChild in artificialBody.StabilizedChildren)
						{
							stabilizedChild.UpdateStabilizedPosition();
						}
					}
				}

				if (movementMessage.DynamicObjectsMovement != null)
				{
					foreach (MovementMessage.TransformInfo objectTransform in movementMessage.DynamicObjectsMovement)
					{
						DynamicObject dynamicObject = GetDynamicObject(objectTransform.Guid);
						if (dynamicObject != null)
						{
							if (objectTransform.Position == null || objectTransform.Rotation == null ||
							    objectTransform.Velocity == null || objectTransform.AngularVelocity == null)
							{
								Debug.LogWarning($"Movement entry for '{objectTransform.Guid}' had a null field.");
								continue;
							}

							dynamicObject.ProcessMovementMessage(objectTransform.ParentGuid, objectTransform.Position.ToVector3(), objectTransform.Rotation.ToQuaternion(),
								objectTransform.Velocity.ToVector3(), objectTransform.AngularVelocity.ToVector3());
						}
						else
						{
							RequestSpawn(objectTransform.Guid);
						}
					}
				}

				if (movementMessage.CorpsesMovement != null)
				{
					foreach (MovementMessage.TransformInfo corpseTransform in movementMessage.CorpsesMovement)
					{
						Corpse corpse = GetCorpse(corpseTransform.Guid);
						if (corpse != null)
						{
							if (corpseTransform.Position == null || corpseTransform.Rotation == null ||
							    corpseTransform.Velocity == null || corpseTransform.AngularVelocity == null)
							{
								Debug.LogWarning($"Movement entry for '{corpseTransform.Guid}' had a null field.");
								continue;
							}

							corpse.ProcessMovementMessage(corpseTransform.ParentGuid, corpseTransform.Position.ToVector3(), corpseTransform.Rotation.ToQuaternion(),
								corpseTransform.Velocity.ToVector3(), corpseTransform.AngularVelocity.ToVector3());
						}
						else
						{
							RequestSpawn(corpseTransform.Guid);
						}
					}
				}

				if (movementMessage.OtherPlayersMovement != null)
				{
					foreach (MovementMessage.OtherPlayerInfo playerInfo in movementMessage.OtherPlayersMovement)
					{
						OtherPlayer otherPlayer = GetPlayer(playerInfo.Guid);
						if (otherPlayer != null)
						{
							if (playerInfo.Position == null || playerInfo.Rotation == null)
							{
								Debug.LogWarning($"Movement entry for '{playerInfo.Guid}' had a null field.");
								continue;
							}

							otherPlayer.ProcessMovementMessage(playerInfo.ParentGuid, playerInfo.Position.ToVector3(), playerInfo.Rotation.ToQuaternion(), playerInfo.FreeLookX,
								playerInfo.FreeLookY, playerInfo.MouseLook, playerInfo.RagdollData, playerInfo.AnimationData, playerInfo.JetpackDirection);
						}
						else
						{
							RequestSpawn(playerInfo.Guid);
						}
					}
				}

				if (movementMessage.VisibleObjects != null)
				{
					ReconcileView(movementMessage.VisibleObjects);
				}
			}
			catch (NullReferenceException ex)
			{
				Debug.LogWarning($"MovementMessage had a null field: {ex}");
			}
		}

		/// <summary>
		/// 	Queue an object to be spawned.
		/// </summary>
		private void RequestSpawn(long guid)
		{
			if (_spawnInFlight.Contains(guid) || TryGetSpaceObject(guid, out SpaceObject _))
			{
				return;
			}

			if (_failedSpawnTimes.TryGetValue(guid, out float failedAt) &&
			    Time.unscaledTime - failedAt < SpawnRetryCooldown)
			{
				return;
			}

			_pendingSpawn.Add(guid);
		}

		/// <summary>
		/// 	Drains the spawn queue. Guids are moved to the in-flight set before awaiting, so subsequent
		/// 	frames won't re-request them while a despawn response is pending.
		/// </summary>
		private void DrainSpawnQueue()
		{
			if (_pendingSpawn.Count == 0)
			{
				return;
			}

			long[] batch = _pendingSpawn.ToArray();
			_pendingSpawn.Clear();
			foreach (long guid in batch)
			{
				_spawnInFlight.Add(guid);
			}

			PumpSpawnQueue(batch).Forget();
		}

		/// <summary>
		/// 	Deletes object outside not in visibleObjects. Ignores the player's parent.
		/// </summary>
		private void ReconcileView(long[] visibleObjects)
		{
			var visible = new HashSet<long>(visibleObjects);

			foreach (long guid in visibleObjects)
			{
				if (!TryGetSpaceObject(guid, out SpaceObject _))
				{
					RequestSpawn(guid);
				}
			}

			List<SpaceObject> toRemove = null;
			foreach (var (guid, spaceObject) in _spaceObjects)
			{
				if (visible.Contains(guid)) continue;
				if (MyPlayer.Instance != null && spaceObject == MyPlayer.Instance.Parent) continue; // TODO: Is this really neccessary?

				toRemove ??= new();
				toRemove.Add(spaceObject);
			}

			if (toRemove == null) return;

			foreach (SpaceObject spaceObject in toRemove)
			{
				if (spaceObject == null) continue;
				DespawnObject(spaceObject);
			}
		}

		/// <summary>
		/// 	The only place where a spaceobject should be deleted.
		/// </summary>
		private void DespawnObject(SpaceObject obj)
		{
			if (obj == null)
			{
				return;
			}

			if (obj is DynamicObject { Parent: Pivot holder } && holder.Guid == obj.Guid)
			{
				obj = holder;
			}

			if (MyPlayer.Instance != null)
			{
				if (obj == MyPlayer.Instance.Parent ||
				    (obj is SpaceObjectVessel vessel && MyPlayer.Instance.Parent is SpaceObjectVessel parentVessel &&
				     vessel.MainVessel == parentVessel.MainVessel))
				{
					Debug.LogWarning($"Refused to despawn '{obj.Guid}': the player is inside this vessel assembly.");
					return;
				}
			}

			bool isPivot = obj.Type is SpaceObjectType.PlayerPivot
				or SpaceObjectType.DynamicObjectPivot
				or SpaceObjectType.CorpsePivot;

			if (!isPivot)
			{
				obj.DestroyGeometry();
			}

			if (obj is DynamicObject { Item: { } item })
			{
				item.AttachPoint?.DetachItem(item);
				if (item.InvSlot is { } invSlot && invSlot.Item == item)
				{
					if (invSlot.SlotType == InventorySlot.Type.Hands && invSlot.Inventory != null)
					{
						invSlot.Inventory.RemoveItemFromHands(resetStance: true);
					}
					else
					{
						invSlot.SetItem(null);
					}
				}

				if (item.DynamicObj.Parent is Player owner)
				{
					item.ChangeEquip(Item.EquipType.None, owner);
				}
			}

			_spaceObjects.TryRemove(obj.Guid, out _);

			Destroy(obj.gameObject);
		}

		private async UniTaskVoid PumpSpawnQueue(long[] guids)
		{
			try
			{
				await MassSpawn(guids);
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
			finally
			{
				foreach (long guid in guids)
				{
					_spawnInFlight.Remove(guid);

					// If the object still isn't present, the server didn't return it. Back off before
					// retrying so a guid we can't spawn isn't requested on every movement message.
					if (!TryGetSpaceObject(guid, out SpaceObject _))
					{
						Debug.LogWarning($"Spawn of '{guid}' produced nothing, backing off before retrying.");
						_failedSpawnTimes[guid] = Time.unscaledTime;
					}
					else
					{
						_failedSpawnTimes.Remove(guid);
					}
				}
			}
		}

		// TODO maybe this can use SceneLoader.LoadScenesWithIDs (see old PlayerSpawnResponse for inspiration)?
		// Order of spawning is very important here. Do not move unless you know what you are doing.
		public async UniTask MassSpawn(long[] guids, long mainObjectGuid = 0)
		{
			ObjectsInfoRequest request = new()
			{
				Guids = guids,
			};

			if (await NetworkController.SendReceiveAsync(request, 10000) is not ObjectsInfoResponse response)
			{
				Debug.LogWarning($"Attempted to spawn objects {string.Join(",", guids)} but got no response.");
				return;
			}

			if (response.ShipObjects != null)
			{
				foreach (ObjectsInfoResponse.ShipData shipData in response.ShipObjects)
				{
					try
					{
						await Ship.Create(this, shipData.Guid, shipData.Position.ToVector3(), shipData.Rotation.ToQuaternion(), shipData.VesselRegistration, shipData.VesselName,
							shipData.Tag,shipData.SceneId, shipData.CollidersCenterOffset, shipData.IsDebrisFragment, shipData.RadarSignature, shipData.IsDistressSignalActive,
							shipData.IsAlwaysVisible, shipData.DockingControlsDisabled, shipData.SecurityPanelsLocked, shipData.VesselObjects, shipData.DockedVessels,
							shipData.Guid == mainObjectGuid || (shipData.DockedVessels != null && shipData.DockedVessels.Any(m => m.Guid == mainObjectGuid)));
					}
					catch (Exception ex)
					{
						Debug.LogError($"Failed to spawn ship '{shipData.Guid}': {ex}");
					}
				}
			}

			if (response.AsteroidObjects != null)
			{
				foreach (ObjectsInfoResponse.AsteroidData asteroidData in response.AsteroidObjects)
				{
					try
					{
						await Asteroid.Create(this, asteroidData.Guid, asteroidData.Position.ToVector3(), asteroidData.Rotation.ToQuaternion(), asteroidData.VesselRegistration,
							asteroidData.VesselName, asteroidData.Tag, asteroidData.SceneId, asteroidData.IsDebrisFragment, asteroidData.IsAlwaysVisible, asteroidData.Radius,
							asteroidData.MiningPoints, asteroidData.Guid == mainObjectGuid);
					}
					catch (Exception ex)
					{
						Debug.LogError($"Failed to spawn asteroid '{asteroidData.Guid}': {ex}");
					}
				}
			}

			if (response.PivotObjects != null)
			{
				foreach (ObjectsInfoResponse.PivotData pivotData in response.PivotObjects)
				{
					try
					{
						Pivot.Create(this, pivotData.Guid, pivotData.PivotType, pivotData.Position.ToVector3(),
							pivotData.Rotation.ToQuaternion(), isMainObject: pivotData.Guid == mainObjectGuid);
					}
					catch (Exception ex)
					{
						Debug.LogError($"Failed to spawn pivot '{pivotData.Guid}': {ex}");
					}
				}
			}

			if (response.Players != null)
			{
				foreach (ObjectsInfoResponse.PlayerData playerData in response.Players)
				{
					try
					{
						OtherPlayer.Create(this, playerData.Guid, playerData.Position.ToVector3(), playerData.Rotation.ToQuaternion(), playerData.ParentId, playerData.Gender,
							playerData.HeadType, playerData.HairType, playerData.Name, playerData.PlayerId, playerData.SpawnPointId,
							playerData.AnimationStatsMask, playerData.LockedToTriggerID, playerData.DynamicObjects);
					}
					catch (Exception ex)
					{
						Debug.LogError($"Failed to spawn player '{playerData.Guid}': {ex}");
					}
				}
			}

			if (response.CorpseObjects != null)
			{
				foreach (ObjectsInfoResponse.CorpseData corpseData in response.CorpseObjects)
				{
					try
					{
						Corpse.Create(this, corpseData.Guid, corpseData.Position.ToVector3(), corpseData.Rotation.ToQuaternion(),
							corpseData.ParentGUID, corpseData.Gender, corpseData.DynamicObjects);
					}
					catch (Exception ex)
					{
						Debug.LogError($"Failed to spawn corpse '{corpseData.Guid}': {ex}");
					}
				}
			}

			if (response.DynamicObjects != null)
			{
				foreach (DynamicObjectDetails objectDetails in response.DynamicObjects)
				{
					if (GetDynamicObject(objectDetails.GUID) == null)
					{
						DynamicObject.CreateDynamicObject(this, objectDetails);
					}
				}
			}
		}


		public SpaceObject GetObject(long guid, SpaceObjectType objectType)
		{
			switch (objectType)
			{
				case SpaceObjectType.Player:
					if (guid == MyPlayer.Instance.Guid)
					{
						return MyPlayer.Instance;
					}

					return GetPlayer(guid);
				case SpaceObjectType.DynamicObject:
					return GetDynamicObject(guid);
				case SpaceObjectType.Corpse:
					return GetCorpse(guid);
				case SpaceObjectType.PlayerPivot:
				case SpaceObjectType.DynamicObjectPivot:
				case SpaceObjectType.CorpsePivot:
				case SpaceObjectType.Ship:
				case SpaceObjectType.Asteroid:
				{
					TryGetSpaceObject(guid, out SpaceObject spaceObject);
					return spaceObject;
				}
			}

			throw new NotImplementedException();
		}

		public void MovePanelCursor(Transform trans, float panelWidth, float panelHeight)
		{
			float x = Mathf.Clamp(
				trans.localPosition.x + Mouse.current.delta.x.ReadValue() * Globals.Instance.MouseSpeedOnPanels, 0f,
				panelWidth);
			float y = Mathf.Clamp(
				trans.localPosition.y + Mouse.current.delta.y.ReadValue() * Globals.Instance.MouseSpeedOnPanels *
				(!Settings.SettingsData.ControlsSettings.InvertMouse ? 1 : (-1)), 0f, panelHeight);
			trans.localPosition = new Vector3(x, y, trans.localPosition.z);
		}

		public async UniTaskVoid LatencyTestMessage()
		{
			_lastLatencyMessageTime = Time.realtimeSinceStartup;

			ServerConnectionInfo server = Globals.LastConnectedServer.Value;
			int latency = await NetworkController.LatencyTest(server.IpAddress, server.StatusPort)
				.AttachExternalCancellation(destroyCancellationToken);

			// A status port that does not answer is not the same as losing the game connection, which the
			// transport reports on its own, so an unanswered probe only means we have no reading.
			if (latency >= 0)
			{
				_latencyMs = latency;
			}

			if (MyPlayer.Instance.IsAlive && latency >= 0)
			{
				if (LatencyMs > 120 && LatencyMs < 150)
				{
					InGameGUI.Latency.color = Colors.SlotGray;
					InGameGUI.Latency.gameObject.Activate(value: true);
				}
				else if (LatencyMs >= 150)
				{
					InGameGUI.Latency.color = Colors.PowerRed;
					InGameGUI.Latency.gameObject.Activate(value: true);
				}
				else
				{
					InGameGUI.Latency.gameObject.Activate(value: false);
				}
			}
			else
			{
				InGameGUI.Latency.gameObject.Activate(value: false);
			}

			Invoke(nameof(LatencyTestMessage), 1f);
		}

		public float GetVesselExposureDamage(double distance)
		{
			if (_vesselExposureValues == null)
			{
				return 1f;
			}

			return _vesselExposureValues[(int)(Mathf.Clamp01((float)(distance / ExposureRange)) * 99f)];
		}

		public float GetPlayerExposureDamage(double distance)
		{
			if (_playerExposureValues == null)
			{
				return 0f;
			}

			return _playerExposureValues[(int)(Mathf.Clamp01((float)(distance / ExposureRange)) * 99f)];
		}

		private void UpdateVesselDataMessageListener(NetworkData data)
		{
			UpdateVesselDataMessage updateVesselDataMessage = data as UpdateVesselDataMessage;
			if (updateVesselDataMessage is null || updateVesselDataMessage.VesselsDataUpdate is null)
			{
				return;
			}

			foreach (VesselDataUpdate item in updateVesselDataMessage.VesselsDataUpdate)
			{
				if (TryGetSpaceObject(item.Guid, out SpaceObjectVessel spaceObjectVessel))
				{
					if (item.VesselName is not null)
					{
						spaceObjectVessel.VesselName = item.VesselName;
					}

					if (item.VesselRegistration is not null)
					{
						spaceObjectVessel.VesselRegistration = item.VesselRegistration;
					}

					if (item.RadarSignature.HasValue)
					{
						spaceObjectVessel.RadarSignature = item.RadarSignature.Value;
					}

					if (item.IsAlwaysVisible.HasValue)
					{
						spaceObjectVessel.IsAlwaysVisible = item.IsAlwaysVisible.Value;
					}

					if (item.IsDistressSignalActive.HasValue)
					{
						spaceObjectVessel.IsDistressSignalActive = item.IsDistressSignalActive.Value;
					}
				}
			}
		}

		/// <summary>
		/// 	Listen for connection drops and attempt to reconnect.
		/// </summary>
		public void ReconnectAutoListener()
		{
			GameStarter gameStarter = GameStarter.Create();
			gameStarter.FindServerAndConnect(true).Forget();
		}

		public async UniTask<bool> OnLogin(LogInResponse logInResponse, SpawnPointDetails chosenSpawn = null,
			VesselObjectID invitedToServerSpawnPointId = null)
		{
			SolarSystem.Set(this, GameObject.Find("/SolarSystemRoot/SunRoot").transform,
				GameObject.Find("/SolarSystemRoot/PlanetsRoot").transform, logInResponse.ServerTime);
			SolarSystem.LoadDataFromResources();

			_pendingSpawn.Clear();
			_spawnInFlight.Clear();
			_failedSpawnTimes.Clear();

			await MyPlayer.SpawnMyPlayer(this, logInResponse);

			foreach (DebrisFieldDetails debrisField in logInResponse.DebrisFields)
			{
				DebrisFields.Add(new DebrisField(this, debrisField));
				Map.InitialiseMapObject(new DebrisField(this, debrisField));
			}

			ItemsIngredients = logInResponse.ItemsIngredients;
			Quests = logInResponse.Quests.Select((QuestData m) => new Quest(m, QuestCollection)).ToList();
			SpaceObjectVessel.VesselDecayRateMultiplier = logInResponse.VesselDecayRateMultiplier;
			ExposureRange = logInResponse.ExposureRange;
			_vesselExposureValues = logInResponse.VesselExposureValues;
			_playerExposureValues = logInResponse.PlayerExposureValues;

			PlayerSpawnRequest playerSpawnRequest;

			if (logInResponse.IsAlive)
			{
				playerSpawnRequest = new PlayerSpawnRequest
				{
					SpawnPointParentId = 0L
				};
			}
			else if (invitedToServerSpawnPointId != null)
			{
				playerSpawnRequest = new PlayerSpawnRequest
				{
					SpawnPointParentId = invitedToServerSpawnPointId.VesselGUID
				};
				invitedToServerSpawnPointId = null;
			}
			else if (chosenSpawn is not null)
			{
				playerSpawnRequest = new PlayerSpawnRequest
				{
					SpawnSetupType = chosenSpawn.SpawnSetupType,
					SpawnPointParentId = chosenSpawn.SpawnPointParentID
				};
			}
			else
			{
				Debug.LogError("Logged in without a spawn point chosen.");
				GlobalGUI.ShowErrorMessage(Localization.SpawnErrorTitle, Localization.SpawnErrorMessage);
				ReturnToMainMenu();
				return false;
			}

			try
			{
				var spawnResponse = await NetworkController.SendReceiveAsync(playerSpawnRequest, 10000) as PlayerSpawnResponse;

				if (spawnResponse.Status != NetworkData.MessageStatus.Success)
				{
					GlobalGUI.ShowErrorMessage(Localization.SpawnErrorTitle, Localization.SpawnErrorMessage);
					Debug.LogWarning("Spawn response returned with failure.");
					ReturnToMainMenu();
					return false;
				}

				AnchorGuid = spawnResponse.AnchorGuid;
				if (spawnResponse.OriginWorldPosition != null)
				{
					OriginWorldPosition = spawnResponse.OriginWorldPosition.ToVector3D();
				}

				ActivatePlayerDelegate = new Action(() =>
				{
					ActivatePlayerDelegate = null;
					MyPlayer.Instance.ActivatePlayer(spawnResponse);
				});

				Debug.Log("Started loading world.");
				SolarSystemRoot.SetActive(true);
				if (spawnResponse.HomeGuid.HasValue)
				{
					MyPlayer.Instance.HomeStationGUID = spawnResponse.HomeGuid.Value;
				}

				await MassSpawn(spawnResponse.AllNearbySpaceObjects, spawnResponse.ParentGuid);

				if (TryGetSpaceObject(spawnResponse.ParentGuid, out SpaceObject parent))
				{
					MyPlayer.Instance.Parent = parent;
				}
				else
				{
					ReturnToMainMenu();
					Debug.LogErrorFormat("Player parent with id {0} was is not near enough the player to load.", spawnResponse.ParentGuid);
					return false;
				}

				if (spawnResponse.TimeUntilServerRestart.HasValue)
				{
					ServerRestartTime = DateTime.UtcNow.AddSeconds(spawnResponse.TimeUntilServerRestart.Value);
				}
				else
				{
					ServerRestartTime = null;
				}

				if (spawnResponse.Quests != null)
				{
					foreach (QuestDetails quest in spawnResponse.Quests)
					{
						MyPlayer.Instance.SetQuestDetails(quest, showNotifications: false, playCutScenes: false);
					}
				}

				if (spawnResponse.Blueprints != null)
				{
					MyPlayer.Instance.Blueprints = spawnResponse.Blueprints;
				}

				return true;
			}
			catch (TimeoutException)
			{
				Debug.Log("Connection timed out when logging in.");
				ReturnToMainMenu();
				return false;
			}
		}

		public void OnDisconnectedFromServer()
		{
			Debug.Log("Client disconnected from server.");

			if (!_logoutRequestSent)
			{
				MainMenuGUI.WasDisconnectUncontrolled = true;
			}

			ReturnToMainMenu();
		}

		/// <summary>
		/// 	Puts the new anchor at the client origin. Every other body, including the old anchor, takes
		/// 	its position from the same message that caused this, already measured from the new anchor,
		/// 	so nothing else has to be shifted.
		/// </summary>
		private void ReOrigin()
		{
			if (!TryGetSpaceObject(AnchorGuid, out ArtificialBody anchor))
			{
				Debug.LogError($"Rebased onto anchor {AnchorGuid}, which is not spawned.");
				return;
			}

			anchor.transform.localPosition = Vector3.zero;
			anchor.UpdateArtificialBodyPosition(updateChildren: true);

			SolarSystem.CenterPlanets();
			MyPlayer.Instance.UpdateCameraPositions();
		}

		/// <summary>
		/// 	Converts a client-space (local) position into an absolute solar-system position using the
		/// 	world transform of the client-space origin. See <see cref="OriginWorldPosition" />.
		/// </summary>
		public Vector3D LocalToWorldPosition(Vector3 localPosition)
		{
			return OriginWorldPosition + localPosition.ToVector3D();
		}
	}
}
