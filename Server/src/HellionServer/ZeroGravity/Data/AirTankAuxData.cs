namespace ZeroGravity.Data;

public class AirTankAuxData : SystemAuxData
{
	public float AirQuality;

	public override SystemAuxDataType AuxDataType => SystemAuxDataType.AirTank;
}
