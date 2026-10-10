using System.Collections.Generic;
using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class ManeuverCourseRequest : NetworkData
{
	public long CourseGUID;

	public long ShipGUID;

	public List<CourseItemData> CourseItems;

	public bool? Activate;
}
