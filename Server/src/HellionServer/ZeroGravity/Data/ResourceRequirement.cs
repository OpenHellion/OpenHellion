using System;
using ZeroGravity.ShipComponents;

namespace ZeroGravity.Data;

[Serializable]
public class ResourceRequirement : ISceneData
{
	public DistributionSystemType ResourceType;

	public float Nominal;

	public float Standby;
}
