using ProtoBuf;
using ZeroGravity.Data;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class SpawnPointStats
{
	public int InSceneID;

	public SpawnPointType? NewType;

	public SpawnPointState? NewState;

	public bool? HackUnlock;

	public long? PlayerGUID;

	public string PlayerName;

	public string PlayerId;

	public bool? PlayerInvite;

	public string InvitedPlayerId;

	public string InvitedPlayerName;
}
