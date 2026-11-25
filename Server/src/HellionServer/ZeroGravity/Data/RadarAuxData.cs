namespace ZeroGravity.Data;

public class RadarAuxData : SystemAuxData
{
	public float ActiveScanDuration;

	public double PassiveRangePerSquareMetre;

	public double UnidentifiedSphereSize;

	public override SystemAuxDataType AuxDataType => SystemAuxDataType.Radar;
}
