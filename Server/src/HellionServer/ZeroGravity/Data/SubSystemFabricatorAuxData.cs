using System.Collections.Generic;

namespace ZeroGravity.Data;

public class SubSystemFabricatorAuxData : SystemAuxData
{
	public float TimePerResourceUnit;

	public List<ItemCompoundType> AllowedItemTypes;

	public List<int> AttachPoints;

	public List<CargoCompartmentData> CargoCompartments;

	public override SystemAuxDataType AuxDataType => SystemAuxDataType.Fabricator;
}
