using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class TextChatMessage : NetworkData
{
	public long GUID;

	public bool Local;

	public string Name;

	public string MessageText;

	public SystemMessagesTypes? MessageType;

	public string[] MessageParam;
}
