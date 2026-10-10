using System.Collections.Generic;
using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Math;
using ZeroGravity.Objects;

namespace ZeroGravity;

public class DeathMatchArenaController : IPersistantObject
{
	public SpaceObjectVessel MainVessel;

	public SpaceObjectVessel CurrentSpawnedShip;

	public long arenaAsteroidID;

	public double RespawnTimeForShip;

	public double SquaredDistanceThreshold = 100000000.0;

	public double timePassedSince;

	public DeathMatchArenaController(SpaceObjectVessel main, Ship ship, long arenaAsteroidID, double squaredDistanceThreshold = 100000000.0, float respawnTimeForShip = 100f)
	{
		SquaredDistanceThreshold = squaredDistanceThreshold;
		RespawnTimeForShip = respawnTimeForShip;
		this.arenaAsteroidID = arenaAsteroidID;
		MainVessel = main;
		CurrentSpawnedShip = ship;
	}

	public DeathMatchArenaController()
	{
	}

	public void StartTimerForNewShip()
	{
		timePassedSince = 0.0;
		Server.Instance.SubscribeToTimer(UpdateTimer.TimerStep.Step_1_0_min, SpawnShipCallback);
	}

	public static async Task<Ship> SpawnSara(SpaceObjectVessel mainShip, Vector3D pos, long arenaAsteroidID)
	{
		Ship sara = await Ship.CreateNewShip(GameScenes.SceneId.AltCorp_Shuttle_SARA, "", -1L, new List<long> { mainShip.Guid }, null, pos, null, MathHelper.RandomRotation());
		sara.StabilizeToTarget(Server.Instance.GetVessel(arenaAsteroidID), forceStabilize: true);
		return sara;
	}

	public static Vector3D NewSaraPos(SpaceObjectVessel v)
	{
		double a = MathHelper.RandomRange(0.0, System.Math.PI * 2.0);
		double b = MathHelper.RandomRange(0.0, System.Math.PI);
		double r = MathHelper.RandomRange(v.Radius + 50.0, v.Radius + 150.0);
		return r * new Vector3D(System.Math.Cos(a) * System.Math.Sin(b), System.Math.Sin(a) * System.Math.Sin(b), System.Math.Cos(b));
	}

	public async Task SpawnShipCallback(double dbl)
	{
		timePassedSince += dbl;
		if (timePassedSince > RespawnTimeForShip)
		{
			timePassedSince = 0.0;
			CurrentSpawnedShip = await SpawnSara(MainVessel, NewSaraPos(MainVessel), arenaAsteroidID);
			Server.Instance.UnsubscribeFromTimer(UpdateTimer.TimerStep.Step_1_0_min, SpawnShipCallback);
			Server.Instance.SubscribeToTimer(UpdateTimer.TimerStep.Step_1_0_min, DistanceCallback);
			List<SpaceObjectVessel> allShips = new List<SpaceObjectVessel>(MainVessel.AllDockedVessels)
			{
				MainVessel
			};
		}
	}

	public Task DistanceCallback(double dbl)
	{
		double distanceSquared = CurrentSpawnedShip == null ? double.MaxValue : CurrentSpawnedShip.Position.DistanceSquared(MainVessel.Position);
		if (distanceSquared > SquaredDistanceThreshold)
		{
			CurrentSpawnedShip = null;
			Server.Instance.UnsubscribeFromTimer(UpdateTimer.TimerStep.Step_1_0_min, DistanceCallback);
			StartTimerForNewShip();
		}
		return Task.CompletedTask;
	}

	public PersistenceObjectData GetPersistenceData()
	{
		PersistenceArenaControllerData data = new PersistenceArenaControllerData
		{
			MainShipGUID = MainVessel.Guid,
			CurrentSpawnedShipGUID = CurrentSpawnedShip == null ? -1 : CurrentSpawnedShip.Guid,
			RespawnTimeForShip = RespawnTimeForShip,
			SquaredDistanceThreshold = SquaredDistanceThreshold,
			timePassedSince = timePassedSince
		};
		return data;
	}

	public Task LoadPersistenceData(PersistenceObjectData persistenceData)
	{
		if (persistenceData is PersistenceArenaControllerData data)
		{
			SquaredDistanceThreshold = data.SquaredDistanceThreshold;
			RespawnTimeForShip = data.RespawnTimeForShip;
			MainVessel = Server.Instance.GetVessel(data.MainShipGUID);
			if (data.CurrentSpawnedShipGUID < 0 && MainVessel.IsDistressSignalActive)
			{
				CurrentSpawnedShip = null;
				timePassedSince = data.timePassedSince;
				Server.Instance.SubscribeToTimer(UpdateTimer.TimerStep.Step_1_0_min, SpawnShipCallback);
			}
			else
			{
				CurrentSpawnedShip = Server.Instance.GetVessel(data.CurrentSpawnedShipGUID);
				Server.Instance.SubscribeToTimer(UpdateTimer.TimerStep.Step_1_0_min, DistanceCallback);
			}
			Server.Instance.DeathMatchArenaControllers.Add(this);
		}
		else
		{
			Debug.LogWarning("PersistenceArenaControllerData wrong type");
		}

		return Task.CompletedTask;
	}
}
