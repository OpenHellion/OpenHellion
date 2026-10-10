using System.Collections.Generic;
using ZeroGravity.Data;

namespace ZeroGravity.ShipComponents;

public interface IResourceConsumer : IResourceUser
{
	Dictionary<DistributionSystemType, ResourceRequirement> ResourceRequirements { get; set; }

	Dictionary<DistributionSystemType, HashSet<ResourceContainer>> ResourceContainers { get; }
}
