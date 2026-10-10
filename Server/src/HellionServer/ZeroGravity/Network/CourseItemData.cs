using System.Collections.Generic;
using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class CourseItemData
{
	public long GUID;

	public ManeuverType Type;

	public float StartOrbitAngle = 0f;

	public float EndOrbitAngle = 0f;

	public double StartSolarSystemTime;

	public double EndSolarSystemTime;

	public double TravelTime = 0.0;

	public int WarpIndex = 0;

	public List<int> WarpCells = null;

	public OrbitData StartOrbit = null;

	public OrbitData EndOrbit = null;
}
