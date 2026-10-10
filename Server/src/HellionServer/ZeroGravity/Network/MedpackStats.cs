using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class MedpackStats : DynamicObjectStats
{
	public bool? Use;
}
