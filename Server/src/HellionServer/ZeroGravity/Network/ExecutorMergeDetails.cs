using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class ExecutorMergeDetails
{
	public VesselObjectID ParentTriggerID;

	public VesselObjectID ChildTriggerID;
}
