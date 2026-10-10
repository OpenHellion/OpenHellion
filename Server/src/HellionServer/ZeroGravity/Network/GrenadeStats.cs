using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class GrenadeStats : DynamicObjectStats
{
	public bool? IsActive;

	public float? Time;

	public bool? Blast;

	public long[] Guids;
}
