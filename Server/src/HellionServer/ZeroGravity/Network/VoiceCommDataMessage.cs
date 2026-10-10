using System.Collections.Generic;
using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class VoiceCommDataMessage : NetworkData, ISteamP2PMessage
{
	public long SourceGUID;

	public bool IsRadioComm;

	public List<byte[]> AudioPackets;
}
