using ProtoBuf;
using ZeroGravity.Data;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class RepairToolStats : DynamicObjectStats
{
	public CargoResourceData FuelResource;

	public bool? Active;
}
