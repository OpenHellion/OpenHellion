using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class HurtPlayerMessage : NetworkData
{
	public PlayerDamage Damage;

	public float Duration;
}
