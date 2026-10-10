using System.Collections.Generic;
using ZeroGravity.Data;
using ZeroGravity.Network;

namespace ZeroGravity;

public class PersistenceObjectAuxDataFabricator : PersistenceData
{
	public double TimeLeft;

	public List<ItemCompoundType> ItemsInQueue;

	public List<CargoCompartmentDetails> CargoCompartments;
}
