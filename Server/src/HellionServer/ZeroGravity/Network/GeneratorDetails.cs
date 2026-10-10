using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class GeneratorDetails
{
	public int InSceneID;

	public SystemStatus Status;

	public SystemSecondaryStatus SecondaryStatus;

	public float Output;

	public float MaxOutput;

	public float OutputRate;

	public float InputFactor;

	public float PowerInputFactor;

	public IAuxDetails AuxDetails = null;

	public bool AutoRestart;

	public string DebugInfo = null;
}
