using System.Collections.Generic;

namespace ZeroGravity.Data;

public class StructureSceneData : ISceneData
{
	public short ItemID;

	public string ScenePath;

	public string SceneName;

	public string GameName;

	public float Mass;

	public float RadarSignature;

	public float[] RadarSignatureHealthMultipliers;

	public float HeatCollectionFactor;

	public float HeatDissipationFactor;

	public List<StructureSceneConnectionData> StructureConnections;

	public List<SpawnPointData> SpawnPoints;

	public List<DynamicObjectSceneData> DynamicObjects;

	public List<BaseAttachPointData> AttachPoints;

	public List<SubSystemData> SubSystems;

	public List<GeneratorData> Generators;

	public List<RoomData> Rooms;

	public List<ResourceContainerData> ResourceContainers;

	public string Collision;

	public ServerCollisionData Colliders;

	public List<DoorData> Doors;

	public List<SceneTriggerExecutorData> SceneTriggerExecutors;

	public List<SceneDockingPortData> DockingPorts;

	public List<SpawnObjectsWithChanceData> SpawnObjectChanceData;

	public CargoBayData CargoBay;

	public List<NameTagData> NameTags;

	public List<VesselRepairPointData> RepairPoints;

	public bool HasSecuritySystem;

	public float MaxHealth;

	public float Health;

	public float BaseArmor;

	public bool InvulnerableWhenDocked;

	public SceneSpawnSettings[] SpawnSettings;

	public TagChance[] AdditionalTags;
}
