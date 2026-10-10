using System.Collections.Generic;
using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class RefineryAuxDetails : IAuxDetails
{
	public List<CargoCompartmentDetails> CargoCompartments;
}
