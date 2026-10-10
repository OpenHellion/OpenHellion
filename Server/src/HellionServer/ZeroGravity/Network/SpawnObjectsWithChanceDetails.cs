using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class SpawnObjectsWithChanceDetails
{
	public int InSceneID;

	public float Chance;
}
