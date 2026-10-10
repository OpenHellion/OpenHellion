using OpenHellion.State;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Network;
using ZeroGravity.ShipComponents;

namespace ZeroGravity.Objects;

internal class RepairTool : Item, ICargo
{
	public float RepairAmount;

	public float UsageCooldown;

	public float Range;

	public CargoCompartmentData FuelCompartment;

	public float FuelConsumption;

	public bool Active
	{
		get => State.Active(Row);
		set => State.SetActive(Row, value);
	}

	private float currentFuel => State.ResourceQuantity(Row, FuelCompartment.ID, (short)FuelType);

	private ResourceType FuelType => FuelCompartment.Resources[0].ResourceType;

	public override async Task SetData(DynamicObjectAuxData data)
	{
		await base.SetData(data);
		RepairToolData rtd = data as RepairToolData;
		RepairAmount = rtd.RepairAmount;
		UsageCooldown = rtd.UsageCooldown;
		Range = rtd.Range;
		FuelCompartment = ObjectCopier.DeepCopy(rtd.FuelCompartment);
		FuelConsumption = rtd.FuelConsumption;
		LoadCargo([FuelCompartment]);
	}

	private RepairToolData GetData()
	{
		RepairToolData rtd = new RepairToolData
		{
			RepairAmount = RepairAmount,
			UsageCooldown = UsageCooldown,
			Range = Range,
			FuelCompartment = GetCompartment(FuelCompartment.ID),
			FuelConsumption = FuelConsumption
		};
		FillBaseAuxData(rtd);
		return rtd;
	}

	public async Task RepairVessel(VesselObjectID repairPointID)
	{
		SpaceObjectVessel vessel = Server.Instance.GetVessel(repairPointID.VesselGUID);
		if (vessel == null)
		{
			return;
		}
		VesselRepairPoint repairPoint = vessel.RepairPoints.Find((VesselRepairPoint m) => m.ID.Equals(repairPointID));
		if (repairPoint != null)
		{
			float repairAmount = RepairAmount;
			float fuelNeeded = repairAmount * FuelConsumption;
			if (fuelNeeded > currentFuel)
			{
				repairAmount = currentFuel / FuelConsumption;
			}
			float amount = repairPoint.MaxHealth - repairPoint.Health < repairAmount ? repairPoint.MaxHealth - repairPoint.Health : repairAmount;
			await vessel.ChangeHealthBy(amount);
			await repairPoint.SetHealthAsync(repairPoint.Health + amount);
			if (vessel.MaxHealth - vessel.Health < float.Epsilon)
			{
				await repairPoint.SetHealthAsync(repairPoint.MaxHealth);
			}
			if (amount > 0f)
			{
				await ConsumeFuel(amount * FuelConsumption);
			}
		}
	}

	public async Task RepairItem(long guid)
	{
		if (Server.Instance.GetDynamicObject(guid) is not { } dynamicObject)
		{
			return;
		}
		Item item = dynamicObject.Item;
		if (item != null)
		{
			float repairAmount = RepairAmount;
			float fuelNeeded = repairAmount * FuelConsumption;
			if (fuelNeeded > currentFuel)
			{
				repairAmount = currentFuel / FuelConsumption;
			}
			float oldHealth = item.Health;
			item.Health += repairAmount;
			float repairedAmount = item.Health - oldHealth;
			if (repairedAmount > 0f)
			{
				await ConsumeFuel(repairedAmount * FuelConsumption);
			}
		}
	}

	public async Task ConsumeFuel(float amount)
	{
		await ChangeQuantityByAsync(FuelCompartment.ID, FuelType, 0f - amount);
	}

	public Task<float> ChangeQuantityByAsync(int compartmentID, ResourceType resourceType, float quantity, bool wholeAmount = false)
	{
		return Task.FromResult(ChangeCargoQuantity(compartmentID, resourceType, quantity));
	}

	protected override bool KeepsEmptyResource(CargoCompartmentData compartment)
	{
		return true;
	}

	public override DynamicObjectStats NewStats()
	{
		return new RepairToolStats();
	}

	public override void FillStats(DynamicObjectStats stats, ItemChanges fields)
	{
		base.FillStats(stats, fields);
		RepairToolStats repairTool = (RepairToolStats)stats;
		if ((fields & ItemChanges.Active) != 0)
		{
			repairTool.Active = Active;
		}
		if ((fields & ItemChanges.Resources) != 0)
		{
			repairTool.FuelResource = GetCompartment(FuelCompartment.ID).Resources.FirstOrDefault();
		}
	}

	public override PersistenceObjectData GetPersistenceData()
	{
		PersistenceObjectDataRepairTool data = new PersistenceObjectDataRepairTool();
		FillPersistenceData(data);
		data.RepairToolData = GetData();
		return data;
	}

	public override async Task LoadPersistenceData(PersistenceObjectData persistenceData)
	{
		await base.LoadPersistenceData(persistenceData);
		if (persistenceData is not PersistenceObjectDataRepairTool data)
		{
			Debug.LogWarning("PersistenceObjectDataJetpack data is null", GUID);
		}
		else
		{
			await SetData(data.RepairToolData);
		}
	}
}
