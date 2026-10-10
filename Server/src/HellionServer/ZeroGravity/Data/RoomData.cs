namespace ZeroGravity.Data;

public class RoomData : ISceneData
{
	public int InSceneID;

	public bool UseGravity;

	public bool GravityAutoToggle;

	public bool AirFiltering;

	public float Volume;

	public float AirQuality;

	public float AirPressure;

	public float PressurizeSpeed;

	public float DepressurizeSpeed;

	public float VentSpeed;

	public int ParentRoomID;
}
