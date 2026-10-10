namespace ZeroGravity.Data;

public class RadarAuxData : SystemAuxData
{
	public double ActiveScanSensitivity;

	public double ActiveScanFuzzySensitivity;

	public float ActiveScanDuration;

	public double PassiveScanSensitivity;

	public double WarpDetectionSensitivity;

	public override SystemAuxDataType AuxDataType => SystemAuxDataType.Radar;
}
