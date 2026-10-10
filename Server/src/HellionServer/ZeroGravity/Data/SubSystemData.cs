using System.Collections.Generic;
using ZeroGravity.Network;
using ZeroGravity.ShipComponents;

namespace ZeroGravity.Data;

public class SubSystemData : ISceneData
{
	public int InSceneID;

	public SubSystemType Type;

	public int RoomID;

	public ResourceRequirement[] ResourceRequirements;

	public SystemSpawnSettings[] SpawnSettings;

	public float OperationRate;

	public bool AutoTuneOperationRate;

	public SystemStatus Status;

	public List<int> MachineryPartSlots;

	public List<int> ResourceContainers;

	public float PowerUpTime;

	public float CoolDownTime;

	public bool AutoReactivate;

	public float RadarSignature;

	public SystemAuxData AuxData;
}
