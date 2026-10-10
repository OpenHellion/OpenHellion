using System.Collections.Generic;

namespace ZeroGravity.ShipComponents;

public interface IResourceUser
{
	Dictionary<DistributionSystemType, SortedSet<IResourceProvider>> ConnectedProviders { get; }
}
