namespace ZeroGravity.Data;

public class SubSystemScrubberDeviceAuxData : SystemAuxData
{
	public float NominalScrubbingCapacity;

	public override SystemAuxDataType AuxDataType => SystemAuxDataType.ScrubberDevice;
}
