using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class LogItemStats : DynamicObjectStats
{
	public int? LogID;
}
