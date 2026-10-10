using System.Collections.Generic;
using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class UpdateVesselDataMessage : NetworkData
{
	public List<VesselDataUpdate> VesselsDataUpdate;
}
