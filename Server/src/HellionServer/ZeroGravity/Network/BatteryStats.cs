using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class BatteryStats : DynamicObjectStats
{
	public float? CurrentPower;

	public float? MaxPower;
}
