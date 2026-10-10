using ProtoBuf;
using ZeroGravity.Data;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class JetpackStats : DynamicObjectStats
{
	public CargoResourceData Propellant;

	public float? PropellantCapacity;

	public CargoResourceData Oxygen;

	public float? OxygenCapacity;
}
