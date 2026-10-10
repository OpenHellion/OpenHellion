namespace ZeroGravity.Data;

public class SubSystemAirDevicesAuxData : SystemAuxData
{
	public float Output;

	public override SystemAuxDataType AuxDataType => SystemAuxDataType.AirDevice;
}
