using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class VesselMiscStatuses
{
	public int InSceneID;

	public CourseItemData CourseInProgress;

	public bool IsMatchedToTarget;
}
