using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public struct DynamicObjectInfo
{
	public long GUID;

	public DynamicObjectStats Stats;

	public DynamicObjectAttachData AttachData;
}
