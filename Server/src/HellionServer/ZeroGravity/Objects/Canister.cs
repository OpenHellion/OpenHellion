using OpenHellion.State;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Network;
using ZeroGravity.ShipComponents;

namespace ZeroGravity.Objects;

internal class Canister : Item, ICargo
{
	private CargoCompartmentData cargoCompartment;


	public bool HasSpace => FreeSpace > float.Epsilon;

	public float FreeSpace => cargoCompartment.Capacity - GetCompartment().Resources.Sum((CargoResourceData m) => m.Quantity);

	public override async Task SetData(DynamicObjectAuxData data)
	{
		await base.SetData(data);
		SetCanisterData((data as CanisterData).CargoCompartment);
		ApplyTierMultiplier();
	}

	private void SetCanisterData(CargoCompartmentData compartmetData)
	{
		cargoCompartment = ObjectCopier.DeepCopy(compartmetData);
		LoadCargo([cargoCompartment]);
	}

	public override void ApplyTierMultiplier()
	{
		if (!TierMultiplierApplied)
		{
			cargoCompartment.Capacity *= TierMultiplier;
		}
		base.ApplyTierMultiplier();
	}

	public Task ChangeQuantity(Dictionary<ResourceType, float> newResources)
	{
		foreach (KeyValuePair<ResourceType, float> res in newResources)
		{
			ChangeCargoQuantity(cargoCompartment.ID, res.Key, res.Value);
		}
		return Task.CompletedTask;
	}

	public Task<float> ChangeQuantityByAsync(int compartmentID, ResourceType resourceType, float quantity, bool wholeAmount = false)
	{
		return Task.FromResult(ChangeCargoQuantity(cargoCompartment.ID, resourceType, quantity));
	}

	protected override bool KeepsEmptyResource(CargoCompartmentData compartment)
	{
		return false;
	}

	public override DynamicObjectStats NewStats()
	{
		return new CanisterStats();
	}

	public override void FillStats(DynamicObjectStats stats, ItemChanges fields)
	{
		base.FillStats(stats, fields);
		CanisterStats canister = (CanisterStats)stats;
		if ((fields & ItemChanges.Resources) != 0)
		{
			canister.Resources = GetCompartment().Resources;
		}
		if (fields == ItemChanges.All)
		{
			canister.Capacity = cargoCompartment.Capacity;
		}
	}

	public override PersistenceObjectData GetPersistenceData()
	{
		PersistenceObjectDataCanister data = new PersistenceObjectDataCanister();
		FillPersistenceData(data);
		data.Compartment = GetCompartment();
		return data;
	}

	public override async Task LoadPersistenceData(PersistenceObjectData persistenceData)
	{
		await base.LoadPersistenceData(persistenceData);
		if (persistenceData is not PersistenceObjectDataCanister data)
		{
			Debug.LogWarning("PersistenceObjectDataCanister data is null", GUID);
		}
		else
		{
			SetCanisterData(data.Compartment);
		}
	}
}
