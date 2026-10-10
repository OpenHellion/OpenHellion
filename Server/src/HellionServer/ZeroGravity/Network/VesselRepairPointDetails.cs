using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class VesselRepairPointDetails
{
	public int InSceneID;

	public float MaxHealth;

	public float Health;

	public bool SecondaryDamageActive;
}
