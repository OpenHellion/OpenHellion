namespace ZeroGravity.Data;

public class GeneratorSolarAuxData : SystemAuxData
{
	public float Efficiency;

	public override SystemAuxDataType AuxDataType => SystemAuxDataType.Solar;
}
