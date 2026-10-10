using System.Collections.Generic;

namespace ZeroGravity.Data;

public class SubSystemRefineryAuxData : SystemAuxData
{
	public float Capacity;

	public float ProcessingTime;

	public List<RefinedResourcesData> Resources;

	public List<CargoCompartmentData> CargoCompartments;

	public override SystemAuxDataType AuxDataType => SystemAuxDataType.Refinery;
}
