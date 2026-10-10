namespace ZeroGravity.Data;

public class SubSystemFTLAuxData : SystemAuxData
{
	public float BaseTowingCapacity;

	public WarpData[] WarpsData;

	public override SystemAuxDataType AuxDataType => SystemAuxDataType.FTL;
}
