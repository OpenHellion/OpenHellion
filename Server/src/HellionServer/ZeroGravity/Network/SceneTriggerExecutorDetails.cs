using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class SceneTriggerExecutorDetails
{
	public int InSceneID;

	public long PlayerThatActivated;

	public int CurrentStateID;

	public int NewStateID;

	public bool IsFail;

	public bool? IsImmediate;

	public int? ProximityTriggerID;

	public bool? ProximityIsEnter;
}
