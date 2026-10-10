using System.Collections.Generic;
using ProtoBuf;
using ZeroGravity.Data;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class VesselObjects
{
	public VesselMiscStatuses MiscStatuses;

	public List<SubSystemDetails> SubSystems;

	public List<GeneratorDetails> Generators;

	public List<ResourceContainerDetails> ResourceContainers;

	public List<RoomDetails> RoomTriggers;

	public List<DoorDetails> Doors;

	public List<SceneTriggerExecutorDetails> SceneTriggerExecutors;

	public List<AttachPointDetails> AttachPoints;

	public List<SceneDockingPortDetails> DockingPorts;

	public List<SpawnObjectsWithChanceDetails> SpawnWithChance;

	public CargoBayDetails CargoBay;

	public List<SpawnPointStats> SpawnPoints;

	public List<NameTagData> NameTags;

	public List<VesselRepairPointDetails> RepairPoints;

	public VesselSecurityData SecurityData;

	public string EmblemId;
}
