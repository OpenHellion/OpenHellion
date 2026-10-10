using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class MagazineStats : DynamicObjectStats
{
	public int? BulletCount;

	public long? BulletsFrom;

	public long? BulletsTo;
}
