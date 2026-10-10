using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class WeaponStats : DynamicObjectStats
{
	public int? CurrentMod;

	public bool? IsOn;
}
