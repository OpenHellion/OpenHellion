using ZeroGravity.Network;

namespace ZeroGravity;

public class PersistenceObjectDataVesselComponent : PersistenceObjectData
{
	public int InSceneID;

	public SystemStatus Status;

	public float StatusChangeCountdown;

	public bool ShouldAutoReactivate;

	public bool AutoReactivate;

	public bool Defective;

	public PersistenceData AuxData;
}
