using ProtoBuf;
using ZeroGravity.Data;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class SaveFileAuxData
{
	public string CelestialBody;

	public GameScenes.SceneId ParentSceneID;

	public string LockedToTrigger;

	public byte[] Screenshot;

	public string ClientVersion;

	public uint ClientHash;
}
