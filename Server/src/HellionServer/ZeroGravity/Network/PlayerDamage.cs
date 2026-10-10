using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class PlayerDamage
{
	public HurtType HurtType;

	public float Amount;
}
