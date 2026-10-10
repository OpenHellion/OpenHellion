using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class RoomDetails
{
	public int InSceneID;

	public bool UseGravity;

	public bool AirFiltering;

	public float AirQuality;

	public float AirPressure;

	public float Temperature;

	public short CompoundRoomID;

	public float AirPressureChangeRate;

	public float AirQualityChangeRate;

	public RoomPressurizationStatus PressurizationStatus;

	public bool Fire;

	public bool Breach;

	public bool GravityMalfunction;
}
