using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class AirTankAuxDetails : IAuxDetails
{
	public float AirQuality;
}
