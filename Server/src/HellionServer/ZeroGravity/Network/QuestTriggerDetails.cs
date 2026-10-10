using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class QuestTriggerDetails
{
	public uint ID;

	public QuestStatus Status;

	public long StationMainVesselGUID;
}
