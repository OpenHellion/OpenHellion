namespace ZeroGravity.Data;

public class GeneratorPowerAuxData : SystemAuxData
{
	public float ResponseTime;

	public override SystemAuxDataType AuxDataType => SystemAuxDataType.PowerGenerator;
}
