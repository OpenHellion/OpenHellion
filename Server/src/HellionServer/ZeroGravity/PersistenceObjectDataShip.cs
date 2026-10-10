using System.Collections.Generic;
using ZeroGravity.Data;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity;

public class PersistenceObjectDataShip : PersistenceObjectData
{
	public OrbitData OrbitData;

	public double[] Rotation;

	public double[] AngularVelocity;

	public string Registration;

	public string Name;

	public string EmblemId;

	public string Tag;

	public GameScenes.SceneId SceneID;

	public float Health;

	public bool IsInvulnerable;

	public bool DockingControlsDisabled;

	public bool SecurityPanelsLocked;

	public long? DockedToShipGUID;

	public long? DockedPortID;

	public long? DockedToPortID;

	public double timePassedSinceShipCall;

	public long? StabilizeToTargetGUID;

	public double[] StabilizeToTargetPosition;

	public CourseItemData CourseInProgress;

	public bool IsDistressSignalActive;

	public bool IsAlwaysVisible;

	public bool IsPrefabStationVessel;

	public SelfDestructTimer.SelfDestructTimerData SelfDestructTimer;

	public List<AuthorizedPerson> AuthorizedPersonel;

	public long StartingSetId;

	public List<PersistenceObjectData> DynamicObjects;

	public PersistenceObjectDataCargo CargoBay;

	public List<PersistenceObjectDataCargo> ResourceTanks;

	public List<PersistenceObjectDataVesselComponent> Generators;

	public List<PersistenceObjectDataVesselComponent> SubSystems;

	public List<PersistenceObjectDataRoom> Rooms;

	public List<PersistenceObjectDataDoor> Doors;

	public List<PersistenceObjectDataDockingPort> DockingPorts;

	public List<PersistenceObjectDataExecutor> Executors;

	public List<PersistenceObjectDataNameTag> NameTags;

	public List<PersistenceObjectDataRepairPoint> RepairPoints;
}
