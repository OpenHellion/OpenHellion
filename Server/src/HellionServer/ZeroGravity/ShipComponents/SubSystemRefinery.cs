using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity.ShipComponents;

public class SubSystemRefinery : SubSystem, ICargo
{
	public float Capacity;

	public float ProcessingTime;

	public List<RefinedResourcesData> Resources;

	public float _CurrentProgress;

	public List<CargoCompartmentData> CargoCompartments;

	private double unitsPerSec;

	public override float PowerUpTime => 0f;

	public float CurrentProgress
	{
		get
		{
			return _CurrentProgress;
		}
		set
		{
			if (value != _CurrentProgress)
			{
				StatusChanged = true;
				_CurrentProgress = value;
			}
		}
	}

	public override SubSystemType Type => SubSystemType.Refinery;

	public List<CargoCompartmentData> Compartments => CargoCompartments;

	public override bool CanBlueprintForceState => false;

	public SubSystemRefinery(SpaceObjectVessel vessel, VesselObjectID id, SubSystemData ssData)
		: base(vessel, id, ssData)
	{
	}

	public override void SetAuxData(SystemAuxData auxData)
	{
		SubSystemRefineryAuxData aux = auxData as SubSystemRefineryAuxData;
		Capacity = aux.Capacity;
		ProcessingTime = aux.ProcessingTime;
		Resources = aux.Resources;
		CargoCompartments = aux.CargoCompartments;
		unitsPerSec = Capacity / ProcessingTime;
	}

	public override IAuxDetails GetAuxDetails()
	{
		List<CargoCompartmentDetails> list = new List<CargoCompartmentDetails>();
		foreach (CargoCompartmentData ccd in CargoCompartments)
		{
			list.Add(new CargoCompartmentDetails
			{
				ID = ccd.ID,
				Resources = new List<CargoResourceData>(ccd.Resources)
			});
		}
		return new RefineryAuxDetails
		{
			CargoCompartments = list
		};
	}

	public override async Task Update(double duration)
	{
		await base.Update(duration);
		if (Status != SystemStatus.OnLine)
		{
			return;
		}
		float units = (float)(unitsPerSec * duration);
		bool shutDown = true;
		CargoCompartmentData ccd = GetCompartment();
		if (ccd.Resources is { Count: > 0 })
		{
			List<CargoResourceData> rawResources = new List<CargoResourceData>(ccd.Resources);
			foreach (CargoResourceData raw in rawResources)
			{
				RefinedResourcesData rrd = Resources.FirstOrDefault((RefinedResourcesData m) => m.RawResource == raw.ResourceType);
				if (rrd == null)
				{
					continue;
				}
				float rawQty = (float)(unitsPerSec * duration);
				if (raw.Quantity < rawQty)
				{
					rawQty = raw.Quantity;
				}
				if (rawQty > units)
				{
					rawQty = units;
				}
				units -= rawQty;
				shutDown = false;
				if (rawQty > float.Epsilon)
				{
					await ChangeQuantityByAsync(ccd.ID, raw.ResourceType, 0f - rawQty);
					foreach (CargoResourceData refined in rrd.RefinedResources)
					{
						await ChangeQuantityByAsync(ccd.ID, refined.ResourceType, rawQty * refined.Quantity);
					}
				}
				if (!(units <= float.Epsilon))
				{
					continue;
				}
				break;
			}
		}
		if (shutDown)
		{
			await GoOffLine(autoRestart: false);
		}
		else
		{
			StatusChanged = true;
		}
	}

	public CargoCompartmentData GetCompartment(int? id = null)
	{
		if (id.HasValue)
		{
			return CargoCompartments.Find((CargoCompartmentData m) => m.ID == id.Value);
		}
		return CargoCompartments[0];
	}

	public Task<float> ChangeQuantityByAsync(int compartmentID, ResourceType resourceType, float quantity, bool wholeAmount = false)
	{
		CargoCompartmentData compartment = Compartments.Find((CargoCompartmentData m) => m.ID == compartmentID);
		CargoResourceData res = compartment.Resources.Find((CargoResourceData m) => m.ResourceType == resourceType);
		if (res == null)
		{
			if (wholeAmount)
			{
				return Task.FromResult(0f);
			}
			res = new CargoResourceData
			{
				ResourceType = resourceType,
				Quantity = 0f
			};
			compartment.Resources.Add(res);
		}
		float freeSpace = compartment.Capacity - compartment.Resources.Sum((CargoResourceData m) => m.Quantity);
		float qty = quantity;
		float resourceAvailable = res.Quantity;
		if (wholeAmount && resourceAvailable - quantity < float.Epsilon)
		{
			return Task.FromResult(0f);
		}
		if (quantity > 0f && quantity > freeSpace)
		{
			qty = freeSpace;
		}
		else if (quantity < 0f && 0f - qty > resourceAvailable)
		{
			qty = 0f - resourceAvailable;
		}
		res.Quantity = resourceAvailable + qty;
		if (res.Quantity <= float.Epsilon)
		{
			compartment.Resources.Remove(res);
		}
		StatusChanged = true;
		return Task.FromResult(qty);
	}

	public override PersistenceData GetPersistenceAuxData()
	{
		List<CargoCompartmentDetails> list = new List<CargoCompartmentDetails>();
		foreach (CargoCompartmentData ccd in CargoCompartments)
		{
			list.Add(new CargoCompartmentDetails
			{
				ID = ccd.ID,
				Resources = new List<CargoResourceData>(ccd.Resources)
			});
		}
		return new PersistenceObjectAuxDataRefinery
		{
			CargoCompartments = list
		};
	}

	public override void SetPersistenceAuxData(PersistenceData auxData)
	{
		PersistenceObjectAuxDataRefinery data = auxData as PersistenceObjectAuxDataRefinery;
		foreach (CargoCompartmentDetails ccd in data.CargoCompartments)
		{
			CargoCompartmentData cargoCompartment = GetCompartment(ccd.ID);
			cargoCompartment.Resources = new List<CargoResourceData>(ObjectCopier.DeepCopy(ccd.Resources));
		}
	}
}
