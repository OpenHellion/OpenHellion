namespace ZeroGravity.Data;

public class GeneratorScrubbedAirAuxData : SystemAuxData
{
	public float ScrubberCartridgeConsumption;

	public override SystemAuxDataType AuxDataType => SystemAuxDataType.ScrubbedAirGenerator;
}
