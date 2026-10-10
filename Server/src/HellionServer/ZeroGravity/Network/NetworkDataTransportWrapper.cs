using System;
using ProtoBuf;

namespace ZeroGravity.Network;

[Serializable]
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class NetworkDataTransportWrapper
{
	public NetworkData data;
}
