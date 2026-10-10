using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class RCSAuxDetails : IAuxDetails
{
	public float MaxOperationRate;
}
