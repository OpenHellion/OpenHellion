using System.Collections.Generic;

namespace ZeroGravity.Data;

public class CargoBayData : ISceneData
{
	public int InSceneID;

	public List<CargoCompartmentData> CargoCompartments;
}
