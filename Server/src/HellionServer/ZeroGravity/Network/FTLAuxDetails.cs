using System.Collections.Generic;
using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class FTLAuxDetails : IAuxDetails
{
	public Dictionary<int, float?> WarpCellsFuel;

	public int MaxWarp;

	public float TowingCapacity;
}
