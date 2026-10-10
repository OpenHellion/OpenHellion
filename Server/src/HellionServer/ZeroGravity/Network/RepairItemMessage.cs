using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class RepairItemMessage : NetworkData
{
	public long GUID;
}
