using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class RagdollItemData
{
	public float[] Position;

	public float[] LocalRotation;
}
