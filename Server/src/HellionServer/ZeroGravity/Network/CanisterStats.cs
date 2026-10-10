using System.Collections.Generic;
using ProtoBuf;
using ZeroGravity.Data;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class CanisterStats : DynamicObjectStats
{
	public List<CargoResourceData> Resources;

	public bool? UseCanister;

	public float? Capacity;
}
