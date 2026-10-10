using ProtoBuf;
using ZeroGravity.Data;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class TransferResourceMessage : NetworkData
{
	public ResourceLocationType FromLocationType;

	public long FromVesselGuid;

	public int FromInSceneID = -1;

	public short FromCompartmentID;

	public ResourceLocationType ToLocationType;

	public long ToVesselGuid;

	public int ToInSceneID = -1;

	public short ToCompartmentID;

	public ResourceType ResourceType;

	public float Quantity;
}
