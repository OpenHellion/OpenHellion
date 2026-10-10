using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class QuestStatsMessage : NetworkData
{
	public QuestDetails QuestDetails;
}
