namespace ZeroGravity.Data;

public class DoorData : ISceneData
{
	public int InSceneID;

	public int Room1ID;

	public int Room2ID;

	public float PassageArea;

	public bool IsSealable;

	public bool HasPower;

	public bool IsLocked;

	public bool IsOpen;

	public bool LockedAutoToggle;

	public float[] PositionRelativeToDockingPort;
}
