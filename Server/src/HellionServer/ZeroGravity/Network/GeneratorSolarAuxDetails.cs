using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class GeneratorSolarAuxDetails : IAuxDetails
{
	public float ExposureToSunlight;
}
