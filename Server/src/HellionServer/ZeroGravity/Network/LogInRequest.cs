using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class LogInRequest : NetworkData
{
	public uint ClientHash;

	public bool IsOffline;

	public string PlayerId;

	public string Password;

	public CharacterData CharacterData;
}
