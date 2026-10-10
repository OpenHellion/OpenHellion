using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class SubSystemDetails
{
	public int InSceneID;

	public SystemStatus Status;

	public SystemSecondaryStatus SecondaryStatus;

	public float OperationRate;

	public float InputFactor;

	public float PowerInputFactor;

	public IAuxDetails AuxDetails = null;

	public bool AutoRestart;

	public string DebugInfo = null;
}
