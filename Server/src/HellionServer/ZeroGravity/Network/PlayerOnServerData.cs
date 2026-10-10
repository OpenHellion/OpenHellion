using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class PlayerOnServerData
{
	public string PlayerId;

	public string Name;

	public bool AlreadyHasInvite;
}
