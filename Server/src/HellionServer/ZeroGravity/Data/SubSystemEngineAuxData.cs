namespace ZeroGravity.Data;

public class SubSystemEngineAuxData : SystemAuxData
{
	public float Acceleration;

	public float ReverseAcceleration;

	public float AccelerationBuildup;

	public override SystemAuxDataType AuxDataType => SystemAuxDataType.Engine;
}
