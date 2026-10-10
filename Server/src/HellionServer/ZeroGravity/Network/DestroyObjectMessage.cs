using ProtoBuf;
using ZeroGravity.Objects;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class DestroyObjectMessage : NetworkData
{
	public SpaceObjectType ObjectType;

	public long ID;
}
