using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class HelmetStats : DynamicObjectStats
{
	public bool? isLightActive;

	public bool? isVisorActive;
}
