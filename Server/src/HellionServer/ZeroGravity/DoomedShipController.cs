using System.Threading.Tasks;
using ZeroGravity.Math;

namespace ZeroGravity;

public class DoomedShipController : IPersistantObject
{
	public static double DestroyTimerMinSec = 1800.0;

	public static double DestroyTimerMaxSec = 7200.0;

	public static double SpawnFrequencySec = 10800.0;

	public static double SpawnChance = 0.5;

	public static double AdditionalSpawnChance1 = 0.75;

	public static double AdditionalSpawnChance2 = 0.75;

	private double spawnTimer;

	public void SubscribeToTimer()
	{
		if (SpawnFrequencySec > 60.0)
		{
			Server.Instance.SubscribeToTimer(UpdateTimer.TimerStep.Step_1_0_min, UpdateTimerCallback);
		}
		else
		{
			Server.Instance.SubscribeToTimer(UpdateTimer.TimerStep.Step_0_5_sec, UpdateTimerCallback);
		}
	}

	private Task UpdateTimerCallback(double deltaTime)
	{
		spawnTimer += deltaTime;
		if (spawnTimer > SpawnFrequencySec)
		{
			if (MathHelper.RandomNextDouble() <= SpawnChance)
			{
				SpawnDoomedShip();
			}
			spawnTimer = 0.0;
		}
		return Task.CompletedTask;
	}

	public void SpawnDoomedShip()
	{
	}

	public PersistenceObjectData GetPersistenceData()
	{
		PersistenceDataDoomController data = new PersistenceDataDoomController();
		data.SpawnTimer = spawnTimer;
		return data;
	}

	public Task LoadPersistenceData(PersistenceObjectData persistenceData)
	{
		if (persistenceData is not PersistenceDataDoomController data)
		{
			Debug.LogWarning("PersistenceDataDoomController wrong type");
		}
		else
		{
			spawnTimer = data.SpawnTimer;
		}

		return Task.CompletedTask;
	}
}
