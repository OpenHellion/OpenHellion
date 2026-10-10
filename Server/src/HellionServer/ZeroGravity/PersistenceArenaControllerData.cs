namespace ZeroGravity;

public class PersistenceArenaControllerData : PersistenceObjectData
{
	public long MainShipGUID;

	public long CurrentSpawnedShipGUID;

	public double RespawnTimeForShip;

	public double SquaredDistanceThreshold;

	public double timePassedSince;
}
