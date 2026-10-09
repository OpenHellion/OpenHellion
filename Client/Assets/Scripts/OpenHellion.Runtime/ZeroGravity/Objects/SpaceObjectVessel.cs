using System;
using System.Collections.Generic;
using System.Linq;
using OpenHellion;
using UnityEngine;
using UnityEngine.Serialization;
using ZeroGravity.Data;
using ZeroGravity.LevelDesign;
using ZeroGravity.Math;
using ZeroGravity.Network;
using ZeroGravity.ShipComponents;

namespace ZeroGravity.Objects
{
	public abstract class SpaceObjectVessel : ArtificialBody
	{
		public enum VesselObjectType
		{
			SubSystem = 1,
			Generator,
			RoomTrigger,
			ResourceContainer,
			Door,
			SceneTriggerExecutor,
			AttachPoint,
			DockingPort,
			SpawnPoint,
			NameTag,
			RepairPoint
		}

		public GameObject RootObject;

		public Dictionary<int, SubSystem> SubSystems = new Dictionary<int, SubSystem>();

		public Dictionary<int, Generator> Generators = new Dictionary<int, Generator>();

		public Dictionary<int, SceneTriggerRoom> RoomTriggers = new Dictionary<int, SceneTriggerRoom>();

		public Dictionary<int, ResourceContainer> ResourceContainers = new Dictionary<int, ResourceContainer>();

		public Dictionary<int, SceneDoor> Doors = new Dictionary<int, SceneDoor>();

		[FormerlySerializedAs("SceneTriggerExecutors")]
		public Dictionary<int, SceneTriggerExecutor>
			SceneTriggerExecutors = new Dictionary<int, SceneTriggerExecutor>();

		public Dictionary<int, BaseSceneAttachPoint> AttachPoints = new Dictionary<int, BaseSceneAttachPoint>();

		public Dictionary<int, SceneDockingPort> DockingPorts = new Dictionary<int, SceneDockingPort>();

		public Dictionary<int, SceneSpawnPoint> SpawnPoints = new Dictionary<int, SceneSpawnPoint>();

		public Dictionary<int, SceneNameTag> NameTags = new Dictionary<int, SceneNameTag>();

		public Dictionary<int, VesselRepairPoint> RepairPoints = new Dictionary<int, VesselRepairPoint>();

		public List<SceneVesselEmblem> Emblems = new List<SceneVesselEmblem>();

		public List<DamagePointData> DamagePoints = new List<DamagePointData>();

		public float Mass;

		public SpaceObjectVessel DockedToMainVessel;

		public SpaceObjectVessel DockedToVessel;

		public List<SpaceObjectVessel> DockedVessels = new List<SpaceObjectVessel>();

		public Vector3 RelativePosition = Vector3.zero;

		public Quaternion RelativeRotation = Quaternion.identity;

		public float Temperature;

		public float Health;

		public float Armor;

		public AnimationCurve DamagePointEffectFrequency;

		public float? SelfDestructTimer;

		public SubSystemEngine Engine;

		public SubSystemRCS RCS;

		public SubSystemFTL FTLEngine;

		public GeneratorCapacitor Capacitor;

		public SceneCargoBay CargoBay;

		public List<SpaceObjectVessel> AllDockedVessels = new List<SpaceObjectVessel>();

		public VesselBaseSystem VesselBaseSystem;

		public static double VesselDecayRateMultiplier = 1.0;

		protected List<Collider> OptimizationColliders;

		protected List<Collider> DontOptimizeColliders;

		public bool DockingControlsDisabled;

		public bool SecurityPanelsLocked;

		public string VesselRegistration;

		public string VesselName;

		public string Tag;

		public long SpawnRuleId;

		public GameScenes.SceneId SceneId;

		public bool IsDebrisFragment;

		private float posDifferenceCheck = 3.33E-05f;

		private float rotDifferenceCheck = 1E-08f;

		internal bool OffSpeedHelper = true;

		public float MaxHealth { get; protected set; }

		public bool EngineOnLine => Engine != null && Engine.Status == SystemStatus.Online;

		public bool HasPower => VesselBaseSystem == null || VesselBaseSystem.Status == SystemStatus.Online;

		public bool OptimizationEnabled { get; private set; }

		public bool IsWarpOnline { get; protected set; }

		public SubSystemRadar RadarSystem { get; protected set; }

		public bool IsDocked => DockedToMainVessel != null;

		public bool IsMainVessel => MainVessel == this;

		public SpaceObjectVessel MainVessel => !(DockedToMainVessel != null) ? this : DockedToMainVessel;

		public virtual string Name => VesselRegistration.IsNullOrEmpty()
			? name
			: VesselRegistration + " " + VesselName;

		public virtual string CustomName
		{
			get
			{
				if (!VesselName.IsNullOrEmpty())
				{
					return VesselName;
				}

				if (!VesselRegistration.IsNullOrEmpty())
				{
					return VesselRegistration;
				}

				return Localization.UnknownObject;
			}
		}

		public string CommandVesselName
		{
			get
			{
				SpaceObjectVessel commandVessel =
					MainVessel.AllDockedVessels.FirstOrDefault((SpaceObjectVessel m) =>
						m.SceneId == GameScenes.SceneId.AltCorp_Command_Module);
				if (commandVessel != null)
				{
					return commandVessel.CustomName;
				}

				return MainVessel.CustomName;
			}
		}

		public List<SpaceObjectVessel> AllVessels
		{
			get
			{
				List<SpaceObjectVessel> list = new List<SpaceObjectVessel>();
				list.Add(MainVessel);
				list.AddRange(MainVessel.AllDockedVessels);
				return list;
			}
		}

		public override Vector3 Velocity => IsMainVessel ? base.Velocity : MainVessel.Velocity;

		public bool IsStation
		{
			get
			{
				return AllDockedVessels.Count > 0 && AllDockedVessels.FirstOrDefault((SpaceObjectVessel m) =>
					m.SceneId == GameScenes.SceneId.AltCorp_Command_Module) != null;
			}
		}

		public bool IsOutpost
		{
			get
			{
				return AllDockedVessels.Count > 0 && AllDockedVessels.FirstOrDefault((SpaceObjectVessel m) =>
					m.SceneId == GameScenes.SceneId.AltCorp_Command_Module) == null;
			}
		}

		public bool IsOutpostOrStation => AllDockedVessels.Count > 0;

		public float ExposureDamage =>
			World.GetVesselExposureDamage(World.LocalToWorldPosition(MainVessel.transform.position).Magnitude);

		private double _radarSignatureInternal;

		public override double RadarSignature
		{
			get
			{
				if (!IsMainVessel)
				{
					return MainVessel.RadarSignature;
				}

				return _radarSignatureInternal;
			}
			set
			{
				_radarSignatureInternal = value;
			}
		}

		protected override bool ShouldSetLocalTransform => !IsDocked && base.ShouldSetLocalTransform;

		protected override bool ShouldUpdateTransform => !IsDocked;

		public bool SoundObjectsEnabled { get; private set; }

		private object GetVesselObject(VesselObjectType objectType, int inSceneID)
		{
			try
			{
				switch (objectType)
				{
					case VesselObjectType.SubSystem:
						return SubSystems[inSceneID];
					case VesselObjectType.Generator:
						return Generators[inSceneID];
					case VesselObjectType.RoomTrigger:
						return RoomTriggers[inSceneID];
					case VesselObjectType.ResourceContainer:
						return ResourceContainers[inSceneID];
					case VesselObjectType.Door:
						return Doors[inSceneID];
					case VesselObjectType.SceneTriggerExecutor:
						return SceneTriggerExecutors[inSceneID];
					case VesselObjectType.AttachPoint:
						return AttachPoints[inSceneID];
					case VesselObjectType.DockingPort:
						return DockingPorts[inSceneID];
					case VesselObjectType.SpawnPoint:
						return SpawnPoints[inSceneID];
					case VesselObjectType.NameTag:
						return NameTags[inSceneID];
					case VesselObjectType.RepairPoint:
						return RepairPoints[inSceneID];
					default:
						Debug.LogError("Cannot get structure object. Unsupported object type." + objectType + inSceneID);
						return null;
				}
			}
			catch
			{
				Debug.LogError("Cannot get structure object. Object ID not found." + objectType + inSceneID);
				return null;
			}
		}

		public T GetVesselObject<T>(int inSceneID)
		{
			try
			{
				if (typeof(SubSystem).IsAssignableFrom(typeof(T)))
				{
					return (T)GetVesselObject(VesselObjectType.SubSystem, inSceneID);
				}

				if (typeof(Generator).IsAssignableFrom(typeof(T)))
				{
					return (T)GetVesselObject(VesselObjectType.Generator, inSceneID);
				}

				if (typeof(SceneTriggerRoom).IsAssignableFrom(typeof(T)))
				{
					return (T)GetVesselObject(VesselObjectType.RoomTrigger, inSceneID);
				}

				if (typeof(ResourceContainer).IsAssignableFrom(typeof(T)))
				{
					return (T)GetVesselObject(VesselObjectType.ResourceContainer, inSceneID);
				}

				if (typeof(SceneDoor).IsAssignableFrom(typeof(T)))
				{
					return (T)GetVesselObject(VesselObjectType.Door, inSceneID);
				}

				if (typeof(SceneTriggerExecutor).IsAssignableFrom(typeof(T)))
				{
					return (T)GetVesselObject(VesselObjectType.SceneTriggerExecutor, inSceneID);
				}

				if (typeof(BaseSceneAttachPoint).IsAssignableFrom(typeof(T)))
				{
					return (T)GetVesselObject(VesselObjectType.AttachPoint, inSceneID);
				}

				if (typeof(SceneDockingPort).IsAssignableFrom(typeof(T)))
				{
					return (T)GetVesselObject(VesselObjectType.DockingPort, inSceneID);
				}

				if (typeof(SceneSpawnPoint).IsAssignableFrom(typeof(T)))
				{
					return (T)GetVesselObject(VesselObjectType.SpawnPoint, inSceneID);
				}

				if (typeof(SceneNameTag).IsAssignableFrom(typeof(T)))
				{
					return (T)GetVesselObject(VesselObjectType.NameTag, inSceneID);
				}

				if (typeof(VesselRepairPoint).IsAssignableFrom(typeof(T)))
				{
					return (T)GetVesselObject(VesselObjectType.RepairPoint, inSceneID);
				}

				return default(T);
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
				return default(T);
			}
		}

		public T GetStructureObject<T>(int inSceneID)
		{
			return GetVesselObject<T>(inSceneID);
		}

		public List<SceneTriggerRoom> GetRoomTriggers()
		{
			return new List<SceneTriggerRoom>(RoomTriggers.Values);
		}

		public abstract void ChangeStats(Vector3? thrust = null, Vector3? rotation = null,
			Vector3? autoStabilize = null, float? engineThrustPercentage = null, SubSystemDetails subSystem = null,
			GeneratorDetails generator = null, RoomDetails roomTrigger = null, DoorDetails door = null,
			SceneTriggerExecutorDetails sceneTriggerExecutor = null, SceneDockingPortDetails dockingPort = null,
			AttachPointDetails attachPoint = null, long? stabilizationTarget = null, SpawnPointStats spawnPoint = null,
			float? selfDestructTime = null, string emblemId = null);

		public void RecreateDockedVesselsTree()
		{
			DockedToMainVessel = null;
			DockedToVessel = null;
			RecreateDockedVesselsTree(this, null);
		}

		private void RecreateDockedVesselsTree(SpaceObjectVessel mainVessel, SpaceObjectVessel parentVessel)
		{
			AllDockedVessels.Clear();
			DockedVessels.Clear();
			foreach (SceneDockingPort value in DockingPorts.Values)
			{
				if (value.DockedToPort != null && value.DockedToPort.ParentShip != parentVessel)
				{
					SpaceObjectVessel parentShip = value.DockedToPort.ParentShip;
					parentShip.DockedToMainVessel = mainVessel;
					parentShip.DockedToVessel = this;
					mainVessel.AllDockedVessels.Add(parentShip);
					DockedVessels.Add(parentShip);
					parentShip.RecreateDockedVesselsTree(mainVessel, this);
				}
			}
		}

		public void ResetDockedToVessel()
		{
			foreach (SpaceObjectVessel dockedVessel in DockedVessels)
			{
				dockedVessel.DockedToVessel = this;
				dockedVessel.ResetDockedToVessel();
			}
		}

		public void SetMainVesselForChldren(SpaceObjectVessel newMainVessel)
		{
			foreach (SpaceObjectVessel dockedVessel in DockedVessels)
			{
				if (!newMainVessel.AllDockedVessels.Contains(dockedVessel))
				{
					dockedVessel.DockedToMainVessel = newMainVessel;
					newMainVessel.AllDockedVessels.Add(dockedVessel);
					dockedVessel.SetMainVesselForChldren(newMainVessel);
				}
			}
		}

		public void DbgLogDockedVesseslTree()
		{
			SpaceObjectVessel spaceObjectVessel = null;
			if (IsDocked)
			{
				spaceObjectVessel = DockedToMainVessel;
			}
			else if (AllDockedVessels.Count > 0)
			{
				spaceObjectVessel = this;
			}

			if (spaceObjectVessel != null)
			{
				spaceObjectVessel.DbgLogDockedVesslesTreeWorker(1);
			}
		}

		private void DbgLogDockedVesslesTreeWorker(int padding)
		{
			foreach (SpaceObjectVessel dockedVessel in DockedVessels)
			{
				dockedVessel.DbgLogDockedVesslesTreeWorker(padding + 1);
			}
		}

		public virtual void OnSceneLoaded()
		{
		}

		public virtual bool IsPlayerAuthorized(Player pl)
		{
			return true;
		}

		public virtual bool IsPlayerAuthorizedOrNoSecurity(Player pl)
		{
			return true;
		}

		public virtual bool IsPlayerAuthorizedOrFreeSecurity(Player pl)
		{
			return true;
		}

		private void RecalculateDifferenceChecks()
		{
			if (!(MyPlayer.Instance == null) && !(MyPlayer.Instance.transform == null))
			{
				float num = (transform.position - MyPlayer.Instance.transform.position).magnitude -
				            (float)Radius;
				if (num < 100f)
				{
					posDifferenceCheck = 1E-08f;
				}
				else if (num < 1000f)
				{
					posDifferenceCheck = 0.001f;
				}
				else if (num < 1500f)
				{
					posDifferenceCheck = num / 1000f;
				}
				else
				{
					posDifferenceCheck = num / 500f;
				}
			}
		}

		protected override bool PositionAndRotationPhysicsCheck(ref Vector3? nextPos, ref Quaternion? nextRot)
		{
			return (nextPos.HasValue &&
			        !nextPos.Value.IsEpsilonEqual(transform.localPosition, posDifferenceCheck)) ||
			       (nextRot.HasValue &&
			        !nextRot.Value.IsEpsilonEqual(transform.localRotation, rotDifferenceCheck));
		}

		public override void UpdateArtificialBodyPosition(bool updateChildren)
		{
			if (ArtificialRigidbody != null && GeometryPlaceholder != null)
			{
				GeometryRoot.transform.position = GeometryPlaceholder.transform.position;
				GeometryRoot.transform.rotation = GeometryPlaceholder.transform.rotation;
				ArtificialRigidbody.position = GeometryPlaceholder.transform.position;
				ArtificialRigidbody.rotation = GeometryPlaceholder.transform.rotation;
			}

			if (!updateChildren || AllDockedVessels == null || AllDockedVessels.Count <= 0)
			{
				return;
			}

			foreach (SpaceObjectVessel allDockedVessel in AllDockedVessels)
			{
				if (allDockedVessel.ArtificialRigidbody != null && allDockedVessel.GeometryPlaceholder != null)
				{
					allDockedVessel.GeometryRoot.transform.position =
						allDockedVessel.GeometryPlaceholder.transform.position;
					allDockedVessel.GeometryRoot.transform.rotation =
						allDockedVessel.GeometryPlaceholder.transform.rotation;
					allDockedVessel.ArtificialRigidbody.position =
						allDockedVessel.GeometryPlaceholder.transform.position;
					allDockedVessel.ArtificialRigidbody.rotation =
						allDockedVessel.GeometryPlaceholder.transform.rotation;
				}
			}
		}

		protected override void UpdatePositionAndRotation(bool setLocalPositionAndRotation)
		{
			if (!IsDocked && setLocalPositionAndRotation)
			{
				RecalculateDifferenceChecks();
			}

			base.UpdatePositionAndRotation(setLocalPositionAndRotation);
			ZeroOcclusion.CheckOcclusionFor(this, onlyCheckDistance: true);
		}

		public void ActivateSelfDestruct(float time)
		{
			if (time >= 0f)
			{
				SelfDestructTimer = time;
				float? selfDestructTime = time;
				ChangeStats(null, null, null, null, null, null, null, null, null, null, null, null, null,
					selfDestructTime);
			}
		}

		public void CancelSelfDestruct()
		{
			SelfDestructTimer = null;
			ChangeStats(null, null, null, null, null, null, null, null, null, null, null, null, null, -1f);
		}

		public override void DestroyGeometry()
		{
			base.DestroyGeometry();
		}

		public virtual void ToggleOptimization(bool optimizationEnabled)
		{
			OptimizationEnabled = optimizationEnabled;
			if (OptimizationColliders != null && OptimizationColliders.Count > 0)
			{
				foreach (Collider optimizationCollider in OptimizationColliders)
				{
					if (optimizationCollider != null)
					{
						optimizationCollider.enabled = !optimizationEnabled;
					}
				}
			}

			bool flag = true;
			if (DontOptimizeColliders != null && DontOptimizeColliders.Count > 0)
			{
				foreach (Collider dontOptimizeCollider in DontOptimizeColliders)
				{
					if (dontOptimizeCollider != null)
					{
						dontOptimizeCollider.enabled = flag;
					}
				}
			}

			DynamicObject[] componentsInChildren = TransferableObjectsRoot.GetComponentsInChildren<DynamicObject>();
			foreach (DynamicObject dynamicObject in componentsInChildren)
			{
				if (!dynamicObject.IsAttached)
				{
					dynamicObject.ToggleKinematic(optimizationEnabled);
				}
			}
		}

		public string GetDescription()
		{
			SpaceObjectVessel mainVessel = MainVessel;
			int num = mainVessel.AllDockedVessels.Count;
			if (num == 0)
			{
				if (GameScenes.Ranges.IsShip(mainVessel.SceneId))
				{
					return Localization.Ship + " - " + mainVessel.SceneId.ToLocalizedString();
				}

				return Localization.Module + " - " + mainVessel.SceneId.ToLocalizedString();
			}

			if (IsStation)
			{
				if (num < 3)
				{
					return Localization.SmallStation;
				}

				if (num < 8)
				{
					return Localization.MediumStation;
				}

				return Localization.LargeStation;
			}

			if (num < 3)
			{
				return Localization.SmallOutpost;
			}

			if (num < 6)
			{
				return Localization.MediumOutpost;
			}

			return Localization.LargeOutpost;
		}


		public double GetCompoundMass()
		{
			return MainVessel.AllVessels.Sum((SpaceObjectVessel m) => m.Mass);
		}
	}
}
