using ZeroGravity.Data;

namespace ZeroGravity;

public class PersistenceObjectDataSpawnPoint : PersistenceObjectData
{
	public int SpawnID;

	public SpawnPointType SpawnType;

	public SpawnPointState SpawnState;

	public long? PlayerGUID;

	public bool? IsPlayerInSpawnPoint;
}
