using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class DeleteCharacterResponse : NetworkData
{
	public string Message = "";
}
