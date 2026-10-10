using OpenHellion.State;
using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Math;
using ZeroGravity.Network;

namespace ZeroGravity.Objects;

public class Battery : Item
{
	public bool HasPower => CurrentPower > float.Epsilon;

	public float CurrentPower
	{
		get => State.Power(Row);
		set => State.SetPower(Row, value);
	}

	public float MaxPower
	{
		get => State.MaxPower(Row);
		set => State.SetMaxPower(Row, value);
	}

	public float ChargeAmount => 1f;

	public override async Task SetData(DynamicObjectAuxData data)
	{
		await base.SetData(data);
		BatteryData bd = data as BatteryData;
		MaxPower = bd.MaxPower;
		CurrentPower = bd.CurrentPower;
		ApplyTierMultiplier();
	}

	public override void ApplyTierMultiplier()
	{
		if (!TierMultiplierApplied)
		{
			MaxPower *= TierMultiplier;
			CurrentPower *= TierMultiplier;
		}
		base.ApplyTierMultiplier();
	}

	public async Task ChangeQuantity(float amount)
	{
		CurrentPower = MathHelper.Clamp(CurrentPower + amount, 0f, MaxPower);
	}

	public override DynamicObjectStats NewStats()
	{
		return new BatteryStats();
	}

	public override void FillStats(DynamicObjectStats stats, ItemChanges fields)
	{
		base.FillStats(stats, fields);
		BatteryStats battery = (BatteryStats)stats;
		if ((fields & ItemChanges.Power) != 0)
		{
			battery.CurrentPower = CurrentPower;
		}
		if ((fields & ItemChanges.MaxPower) != 0)
		{
			battery.MaxPower = MaxPower;
		}
	}

	public override PersistenceObjectData GetPersistenceData()
	{
		PersistenceObjectDataBattery data = new PersistenceObjectDataBattery();
		FillPersistenceData(data);
		data.BatteryData = new BatteryData();
		FillBaseAuxData(data.BatteryData);
		data.BatteryData.CurrentPower = CurrentPower;
		data.BatteryData.MaxPower = MaxPower;
		return data;
	}

	public override async Task LoadPersistenceData(PersistenceObjectData persistenceData)
	{
		await base.LoadPersistenceData(persistenceData);
		if (persistenceData is not PersistenceObjectDataBattery data)
		{
			Debug.LogWarning("PersistenceObjectDataBattery data is null", GUID);
		}
		else
		{
			await SetData(data.BatteryData);
		}
	}
}
