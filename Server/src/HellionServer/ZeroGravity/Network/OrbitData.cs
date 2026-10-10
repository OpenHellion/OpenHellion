using System;
using ProtoBuf;
using ZeroGravity.Objects;

namespace ZeroGravity.Network;

[Serializable]
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class OrbitData
{
	public long ParentGUID;

	public long? GUID;

	public SpaceObjectType? ObjectType;

	public double Eccentricity;

	public double SemiMajorAxis;

	public double LongitudeOfAscendingNode;

	public double ArgumentOfPeriapsis;

	public double Inclination;

	public double TimeSincePeriapsis;

	public double SolarSystemPeriapsisTime;
}
