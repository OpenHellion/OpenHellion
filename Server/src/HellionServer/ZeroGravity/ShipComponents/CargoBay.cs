using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenHellion.Net;
using ZeroGravity.Data;
using ZeroGravity.Math;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity.ShipComponents;

public class CargoBay : ICargo, IPersistantObject
{
	public SpaceObjectVessel ParentVessel;

	public int InSceneID;

	public List<CargoCompartmentData> CargoCompartments;

	public List<CargoCompartmentData> Compartments => CargoCompartments;

	public CargoBay(SpaceObjectVessel vessel, List<CargoCompartmentData> cargoCompartments)
	{
		ParentVessel = vessel;
		CargoCompartments = cargoCompartments;
		foreach (CargoCompartmentData compartment in CargoCompartments)
		{
			foreach (CargoResourceData resource in compartment.Resources)
			{
				ResourcesSpawnSettings[] spawnSettings = resource.SpawnSettings;
				foreach (ResourcesSpawnSettings rss in spawnSettings)
				{
					if (vessel.CheckTag(rss.Tag, rss.Case))
					{
						float qty = MathHelper.RandomRange(rss.MinQuantity, rss.MaxQuantity);
						float avail = compartment.Capacity - compartment.Resources.Sum((CargoResourceData m) => m.Quantity);
						resource.Quantity = 0f;
						if (qty < 0f)
						{
							qty = 0f;
						}
						else if (qty > avail)
						{
							qty = avail;
						}
						resource.Quantity = qty;
						break;
					}
				}
			}
		}
		if (Compartments == null)
		{
			return;
		}
		foreach (CargoCompartmentData ccd in Compartments)
		{
			ccd.Resources.RemoveAll((CargoResourceData m) => m == null || m.Quantity <= float.Epsilon);
		}
	}

	public CargoBayDetails GetDetails()
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
		return new CargoBayDetails
		{
			InSceneID = InSceneID,
			CargoCompartments = list
		};
	}

	public CargoCompartmentData GetCompartment(int? id = null)
	{
		if (id.HasValue)
		{
			return CargoCompartments.Find((CargoCompartmentData m) => m.ID == id.Value);
		}
		return CargoCompartments[0];
	}

	public async Task<float> ChangeQuantityByAsync(int compartmentID, ResourceType resourceType, float quantity, bool wholeAmount = false)
	{
		CargoCompartmentData compartment = Compartments.Find((CargoCompartmentData m) => m.ID == compartmentID);
		CargoResourceData res = compartment.Resources.Find((CargoResourceData m) => m.ResourceType == resourceType);
		if (res == null)
		{
			if (wholeAmount)
			{
				return 0f;
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
			return 0f;
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
		await NetworkController.SendToClientsSubscribedTo(new ShipStatsMessage
		{
			Guid = ParentVessel.Guid,
			Temperature = ParentVessel.Temperature,
			Health = ParentVessel.Health,
			Armor = ParentVessel.Armor,
			VesselObjects = new VesselObjects
			{
				CargoBay = GetDetails()
			}
		}, -1L, ParentVessel);
		return qty;
	}

	public PersistenceObjectData GetPersistenceData()
	{
		return new PersistenceObjectDataCargo
		{
			InSceneID = InSceneID,
			CargoCompartments = CargoCompartments
		};
	}

	public Task LoadPersistenceData(PersistenceObjectData persistenceData)
	{
		if (persistenceData is not PersistenceObjectDataCargo data)
		{
			Debug.LogWarning("PersistenceObjectDataCargo data is null");
			return Task.CompletedTask;
		}

		CargoCompartments = data.CargoCompartments;
		foreach (CargoCompartmentData ccd in CargoCompartments)
		{
			ccd.Resources.RemoveAll((CargoResourceData m) => m.Quantity <= float.Epsilon);
		}

		return Task.CompletedTask;
	}
}
