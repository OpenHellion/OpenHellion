using System.Collections.Generic;
using ZeroGravity.Network;
using ZeroGravity.ShipComponents;

namespace ZeroGravity.Data;

public class GeneratorData : ISceneData
{
	public int InSceneID;

	public GeneratorType Type;

	public int RoomID;

	public float Output;

	public float PowerUpTime;

	public float CoolDownTime;

	public bool AutoReactivate;

	public float NominalOutput;

	public float RequestedOutput;

	public float OutputRate;

	public SystemStatus Status;

	public ResourceRequirement[] ResourceRequirements;

	public SystemSpawnSettings[] SpawnSettings;

	public List<int> MachineryPartSlots;

	public List<int> ResourceContainers;

	public float RadarSignature;

	public SystemAuxData AuxData;
}
