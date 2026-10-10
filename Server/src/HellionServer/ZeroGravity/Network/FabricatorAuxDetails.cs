using System.Collections.Generic;
using ProtoBuf;
using ZeroGravity.Data;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class FabricatorAuxDetails : IAuxDetails
{
	public float CurrentTimeLeft;

	public float TotalTimeLeft;

	public List<ItemCompoundType> ItemsInQueue;

	public List<CargoCompartmentDetails> CargoCompartments;
}
