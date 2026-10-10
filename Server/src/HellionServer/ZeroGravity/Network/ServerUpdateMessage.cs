using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class ServerUpdateMessage : NetworkData
{
	public bool CleanStart;
}
