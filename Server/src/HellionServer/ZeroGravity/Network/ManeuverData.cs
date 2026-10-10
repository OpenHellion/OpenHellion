using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class ManeuverData
{
	public long GUID;

	public ManeuverType Type;

	public long ParentGUID;

	public double[] RelPosition;

	public double[] RelVelocity;
}
