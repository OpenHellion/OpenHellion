using System.Collections.Generic;
using ProtoBuf;
using ZeroGravity.Data;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class CargoCompartmentDetails
{
	public short ID;

	public List<CargoResourceData> Resources;
}
