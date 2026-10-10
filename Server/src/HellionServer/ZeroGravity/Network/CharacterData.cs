using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class CharacterData
{
	public string Name;

	public Gender Gender;

	public byte HeadType;

	public byte HairType;
}
