namespace ZeroGravity.Data;

public class RadarAuxData : SystemAuxData
{
	public float ActiveScanDuration;

	public double PassiveThreshold;

	public override SystemAuxDataType AuxDataType => SystemAuxDataType.Radar;
}
