using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class DistressCallRequest : NetworkData
{
	public long GUID;

	public bool IsDistressActive;
}
