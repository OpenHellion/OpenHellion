using ProtoBuf;

namespace ZeroGravity.Data;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class NameTagData : ISceneData
{
	public int InSceneID;

	public string NameTagText;
}
