using System.Collections.Generic;
using ProtoBuf;
using ZeroGravity.Data;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class UpdateBlueprintsMessage : NetworkData
{
	public List<ItemCompoundType> Blueprints;
}
