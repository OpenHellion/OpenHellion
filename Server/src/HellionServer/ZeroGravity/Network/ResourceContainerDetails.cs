using System.Collections.Generic;
using ProtoBuf;
using ZeroGravity.Data;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class ResourceContainerDetails
{
	public int InSceneID;

	public List<CargoResourceData> Resources;

	public float QuantityChangeRate;

	public float Output;

	public float OutputRate;

	public bool IsInUse;

	public IAuxDetails AuxDetails = null;
}
