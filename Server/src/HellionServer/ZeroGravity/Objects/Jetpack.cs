using OpenHellion.State;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Network;
using ZeroGravity.ShipComponents;

namespace ZeroGravity.Objects;

public class Jetpack : Item, ICargo
{
	private CargoCompartmentData OxygenCompartment;

	private CargoCompartmentData PropellantCompartment;

	public float OxygenConsumption;

	public float PropellantConsumption;

	public bool HasOxygen => currentOxygen > float.Epsilon;

	private float currentOxygen => GetCompartment(OxygenCompartment.ID).Resources.Sum(m => m.Quantity);

	public async Task ConsumeResources(float? propellant = null, float? oxygen = null)
	{
		if (oxygen is > 0f && GetCompartment(OxygenCompartment.ID).Resources.FirstOrDefault() is { } air)
		{
			await ChangeQuantityByAsync(OxygenCompartment.ID, air.ResourceType, 0f - oxygen.Value);
		}
		if (propellant is > 0f && GetCompartment(PropellantCompartment.ID).Resources.FirstOrDefault() is { } fuel)
		{
			await ChangeQuantityByAsync(PropellantCompartment.ID, fuel.ResourceType, 0f - propellant.Value);
		}
	}

	public static bool CanChangeValue(float currentAmount, float amount, float min, float max)
	{
		return (amount.IsNotEpsilonZero() && (!(amount < 0f) || !currentAmount.IsEpsilonEqual(min))) || (amount > 0f && currentAmount.IsEpsilonEqual(max));
	}

	public Task<float> ChangeQuantityByAsync(int compartmentID, ResourceType resourceType, float quantity, bool wholeAmount = false)
	{
		return Task.FromResult(ChangeCargoQuantity(compartmentID, resourceType, quantity));
	}

	public override DynamicObjectStats NewStats()
	{
		return new JetpackStats();
	}

	public override void FillStats(DynamicObjectStats stats, ItemChanges fields)
	{
		base.FillStats(stats, fields);
		JetpackStats jetpack = (JetpackStats)stats;
		if ((fields & ItemChanges.Resources) != 0)
		{
			jetpack.Oxygen = GetCompartment(OxygenCompartment.ID).Resources.FirstOrDefault();
			jetpack.Propellant = GetCompartment(PropellantCompartment.ID).Resources.FirstOrDefault();
		}
		if (fields == ItemChanges.All)
		{
			jetpack.OxygenCapacity = OxygenCompartment.Capacity;
			jetpack.PropellantCapacity = PropellantCompartment.Capacity;
		}
	}

	public override PersistenceObjectData GetPersistenceData()
	{
		PersistenceObjectDataJetpack data = new PersistenceObjectDataJetpack();
		FillPersistenceData(data);
		data.JetpackData = GetJetpackData();
		return data;
	}

	public override async Task LoadPersistenceData(PersistenceObjectData persistenceData)
	{
		await base.LoadPersistenceData(persistenceData);
		if (persistenceData is not PersistenceObjectDataJetpack data)
		{
			Debug.LogWarning("PersistenceObjectDataJetpack data is null", GUID);
		}
		else
		{
			await SetData(data.JetpackData);
		}
	}

	public override async Task SetData(DynamicObjectAuxData data)
	{
		await base.SetData(data);
		JetpackData jd = data as JetpackData;
		PropellantCompartment = ObjectCopier.DeepCopy(jd.PropellantCompartment);
		OxygenCompartment = ObjectCopier.DeepCopy(jd.OxygenCompartment);
		ApplyTierMultiplier();
		LoadCargo([OxygenCompartment, PropellantCompartment]);
		OxygenConsumption = jd.OxygenConsumption;
		PropellantConsumption = jd.PropellantConsumption;
		MaxHealth = jd.MaxHealth;
		Health = jd.Health;
	}

	public override void ApplyTierMultiplier()
	{
		if (!TierMultiplierApplied)
		{
			PropellantCompartment.Capacity *= TierMultiplier;
			OxygenCompartment.Capacity *= AuxValue;
		}
		base.ApplyTierMultiplier();
	}

	private JetpackData GetJetpackData()
	{
		JetpackData jd = new JetpackData
		{
			OxygenCompartment = GetCompartment(OxygenCompartment.ID),
			PropellantCompartment = GetCompartment(PropellantCompartment.ID),
			OxygenConsumption = OxygenConsumption,
			PropellantConsumption = PropellantConsumption
		};
		FillBaseAuxData(jd);
		return jd;
	}
}
