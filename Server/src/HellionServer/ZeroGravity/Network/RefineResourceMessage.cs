using ProtoBuf;
using ZeroGravity.Data;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class RefineResourceMessage : NetworkData
{
	public ResourceLocationType FromLocationType;

	public long FromVesselGuid;

	public int FromInSceneID = -1;

	public int FromCompartmentID;

	public long ToVesselGuid;

	public int ToInSceneID = -1;

	public ResourceType ResourceType;

	public float Quantity;
}
