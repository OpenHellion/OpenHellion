using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class QuestTriggerMessage : NetworkData
{
	public uint QuestID;

	public uint TriggerID;
}
