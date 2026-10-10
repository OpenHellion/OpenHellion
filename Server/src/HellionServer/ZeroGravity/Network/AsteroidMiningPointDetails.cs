using ProtoBuf;
using ZeroGravity.Data;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class AsteroidMiningPointDetails
{
	public int InSceneID;

	public ResourceType ResourceType;

	public float Quantity;

	public float MaxQuantity;
}
