using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class ServerShutDownMessage : NetworkData
{
	public bool Restrat;

	public bool CleanRestart;
}
