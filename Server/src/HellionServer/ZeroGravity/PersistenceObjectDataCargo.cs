using System.Collections.Generic;
using ZeroGravity.Data;

namespace ZeroGravity;

public class PersistenceObjectDataCargo : PersistenceObjectData
{
	public int InSceneID;

	public List<CargoCompartmentData> CargoCompartments;

	public PersistenceData AuxData;
}
