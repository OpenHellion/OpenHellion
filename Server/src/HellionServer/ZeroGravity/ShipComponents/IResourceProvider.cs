using System.Collections.Generic;

namespace ZeroGravity.ShipComponents;

public interface IResourceProvider
{
	HashSet<IResourceUser> ConnectedConsumers { get; }

	float Output { get; set; }

	float NominalOutput { get; set; }

	float MaxOutput { get; }

	float OperationRate { get; set; }

	DistributionSystemType OutputType { get; }
}
