using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class DebrisFieldDetails
{
	public string Name;

	public DebrisFieldType Type;

	public double Radius;

	public float FragmentsDensity;

	public float FragmentsVelocity;

	public OrbitData Orbit;

	public float ScanningSensitivityMultiplier;

	public float RadarSignatureMultiplier;
}
