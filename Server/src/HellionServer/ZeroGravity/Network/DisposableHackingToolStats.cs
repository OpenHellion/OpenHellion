using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class DisposableHackingToolStats : DynamicObjectStats
{
	public bool? Use;
}
