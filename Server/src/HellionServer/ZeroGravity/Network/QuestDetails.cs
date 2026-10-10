using System.Collections.Generic;
using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class QuestDetails
{
	public uint ID;

	public List<QuestTriggerDetails> QuestTriggers;

	public QuestStatus Status;
}
