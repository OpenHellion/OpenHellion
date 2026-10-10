using OpenHellion.State;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Math;
using ZeroGravity.Network;

namespace ZeroGravity.Objects;

public class Helmet : Item, IBatteryConsumer, IUpdateable
{
	public bool IsVisorToggleable;

	public float HUDPowerConsumption;

	public float LightPowerConsumption;

	public float DamageReduction;

	public float DamageResistance = 1f;

	public ItemSlot BatterySlot { get; set; }

	public Battery Battery => BatterySlot != null ? BatterySlot.Item as Battery : null;

	public float BatteryPower => Battery != null ? MathHelper.Clamp(Battery.CurrentPower / Battery.MaxPower, 0f, 1f) : 0f;

	public bool IsVisorActive
	{
		get => State.VisorOn(Row);
		set => State.SetVisorOn(Row, value);
	}

	public bool IsLightActive
	{
		get => State.LightOn(Row);
		set => State.SetLightOn(Row, value);
	}

	public override async Task SetData(DynamicObjectAuxData data)
	{
		await base.SetData(data);
		HelmetData hd = data as HelmetData;
		IsLightActive = hd.IsLightActive;
		IsVisorActive = hd.IsVisorActive;
		HUDPowerConsumption = hd.HUDPowerConsumption;
		LightPowerConsumption = hd.LightPowerConsumption;
		IsVisorToggleable = hd.IsVisorToggleable;
		DamageReduction = hd.DamageReduction;
		DamageResistance = hd.DamageResistance;
		BatterySlot = Slots?.Values.FirstOrDefault(m => m.ItemTypes.Contains(ItemType.AltairHandDrillBattery));
	}

	public override DynamicObjectStats NewStats()
	{
		return new HelmetStats();
	}

	public override void FillStats(DynamicObjectStats stats, ItemChanges fields)
	{
		base.FillStats(stats, fields);
		HelmetStats helmet = (HelmetStats)stats;
		if ((fields & ItemChanges.LightOn) != 0)
		{
			helmet.isLightActive = IsLightActive;
		}
		if ((fields & ItemChanges.VisorOn) != 0)
		{
			helmet.isVisorActive = IsVisorActive;
		}
	}

	public override PersistenceObjectData GetPersistenceData()
	{
		PersistenceObjectDataHelmet data = new PersistenceObjectDataHelmet();
		FillPersistenceData(data);
		data.HelmetData = new HelmetData();
		FillBaseAuxData(data.HelmetData);
		data.HelmetData.IsVisorToggleable = IsVisorToggleable;
		data.HelmetData.IsLightActive = IsLightActive;
		data.HelmetData.IsVisorActive = IsVisorActive;
		data.HelmetData.LightPowerConsumption = LightPowerConsumption;
		data.HelmetData.HUDPowerConsumption = HUDPowerConsumption;
		data.HelmetData.DamageReduction = DamageReduction;
		data.HelmetData.DamageResistance = DamageResistance;
		return data;
	}

	public override async Task LoadPersistenceData(PersistenceObjectData persistenceData)
	{
		await base.LoadPersistenceData(persistenceData);
		if (persistenceData is not PersistenceObjectDataHelmet data)
		{
			Debug.LogWarning("PersistenceObjectDataHelmet data is null", GUID);
		}
		else
		{
			await SetData(data.HelmetData);
		}
	}

	public async Task Update(double deltaTime)
	{
		if (Battery != null)
		{
			float cons = 0f;
			if (IsLightActive)
			{
				cons -= LightPowerConsumption * (float)deltaTime;
			}
			if (IsVisorActive)
			{
				cons -= HUDPowerConsumption * (float)deltaTime;
			}
			if (cons != 0f)
			{
				await Battery.ChangeQuantity(cons);
			}
		}
		if (BatteryPower < float.Epsilon && IsLightActive)
		{
			IsLightActive = false;
		}
	}
}
