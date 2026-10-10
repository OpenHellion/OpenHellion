using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class DoorDetails
{
	public int InSceneID;

	public bool IsLocked;

	public bool IsOpen;

	public bool HasPower;

	public bool EquilizePressure;

	public float PressureEquilizationTime;

	public int AirFlowDirection;

	public float AirSpeed;

	public VesselObjectID Room1ID;

	public VesselObjectID Room2ID;
}
