using OpenHellion.State;
using System;
using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Network;

namespace ZeroGravity.Objects;

public class Grenade : Item
{
	private float detonationTime;

	public long PlayerGUID;

	private double activationTime;

	public void SetActive(bool value, Player sender)
	{
		PlayerGUID = sender.Guid;
		State.SetActive(Row, value);
		if (value)
		{
			activationTime = Server.SolarSystemTime;
			Server.Instance.SubscribeToTimer(UpdateTimer.TimerStep.Step_0_1_sec, CheckDetonation);
		}
		else
		{
			Server.Instance.UnsubscribeFromTimer(UpdateTimer.TimerStep.Step_0_1_sec, CheckDetonation);
		}
	}

	public override async Task SetData(DynamicObjectAuxData data)
	{
		await base.SetData(data);
		GrenadeData i = data as GrenadeData;
		detonationTime = i.DetonationTime;
	}

	private Task CheckDetonation(double deltaTime)
	{
		if (!State.IsAlive(Row) || !State.Active(Row))
		{
			Server.Instance.UnsubscribeFromTimer(UpdateTimer.TimerStep.Step_0_1_sec, CheckDetonation);
		}
		else if (Server.SolarSystemTime - activationTime >= detonationTime)
		{
			Server.Instance.UnsubscribeFromTimer(UpdateTimer.TimerStep.Step_0_1_sec, CheckDetonation);
			State.SetDetonated(Row, true);
		}
		return Task.CompletedTask;
	}

	public override DynamicObjectStats NewStats()
	{
		return new GrenadeStats();
	}

	public override void FillStats(DynamicObjectStats stats, ItemChanges fields)
	{
		base.FillStats(stats, fields);
		GrenadeStats grenade = (GrenadeStats)stats;
		if ((fields & ItemChanges.Active) != 0)
		{
			grenade.IsActive = State.Active(Row);
		}
		if ((fields & ItemChanges.Detonated) != 0 && State.Detonated(Row))
		{
			grenade.Blast = true;
		}
	}

	public override PersistenceObjectData GetPersistenceData()
	{
		PersistenceObjectDataGrenade data = new PersistenceObjectDataGrenade();
		FillPersistenceData(data);
		data.GrenadeData = new GrenadeData();
		FillBaseAuxData(data.GrenadeData);
		data.GrenadeData.DetonationTime = detonationTime;
		data.GrenadeData.IsActive = false;
		return data;
	}

	public override async Task LoadPersistenceData(PersistenceObjectData persistenceData)
	{
		await base.LoadPersistenceData(persistenceData);
		if (persistenceData is not PersistenceObjectDataGrenade data)
		{
			Debug.LogWarning("PersistenceObjectDataHandheldGrenade data is null", GUID);
		}
		else
		{
			await SetData(data.GrenadeData);
		}
	}
}
