using System.Collections.Generic;
using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class CargoBayDetails
{
	public int InSceneID;

	public List<CargoCompartmentDetails> CargoCompartments;
}
