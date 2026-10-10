using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class GeneratorCapacitorAuxDetails : IAuxDetails
{
	public float MaxCapacity;

	public float Capacity;

	public float CapacityChangeRate;
}
