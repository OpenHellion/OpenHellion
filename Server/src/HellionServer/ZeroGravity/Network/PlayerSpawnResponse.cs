using System.Collections.Generic;
using ProtoBuf;
using ZeroGravity.Data;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class PlayerSpawnResponse : NetworkData
{
	/// <summary>
	/// 	In space stations (collection of docked ships) this is the station part that is the parent of the player, not the main element of a ship.
	/// </summary>
	public long ParentGuid;

	public long[] AllNearbySpaceObjects;

	public int Health;

	public bool IsAdmin;

	public int SpawnPointId;

	public long? HomeGuid;

	public double? TimeUntilServerRestart;

	public List<QuestDetails> Quests;

	public List<ItemCompoundType> Blueprints;

	public float[] Position;

	public float[] Rotation;

	public long AnchorGuid;

	public double[] OriginWorldPosition;
}
