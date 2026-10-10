using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class ManeuverCourseResponse : NetworkData
{
	public bool IsValid;

	public long CourseGUID;

	public long VesselGUID;

	public bool? IsFinished;

	public bool? IsActivated;

	public double? StartTime;

	public double? EndTime;

	public float[] StartDirection;

	public bool? StaringSoon;
}
