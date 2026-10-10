using System.Collections.Generic;
using System.Threading.Tasks;
using ZeroGravity.Data;

namespace ZeroGravity.ShipComponents;

public interface ICargo
{
	List<CargoCompartmentData> Compartments { get; }

	CargoCompartmentData GetCompartment(int? id = null);

	Task<float> ChangeQuantityByAsync(int compartmentID, ResourceType resourceType, float quantity, bool wholeAmount = false);
}
