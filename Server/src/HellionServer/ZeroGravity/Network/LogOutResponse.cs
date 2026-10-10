using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class LogOutResponse : NetworkData
{
	public LogOutResponse()
	{
		Status = MessageStatus.Success;
	}
}
