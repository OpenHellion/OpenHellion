using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Math;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity.ShipComponents;

public class SubSystemFabricator : SubSystem, ICargo
{
	private double _TimeLeft = -1.0;

	private float TimePerResourceUnit = 1f;

	private List<VesselAttachPoint> AttachPoints;

	public List<ItemCompoundType> ItemsInQueue = new List<ItemCompoundType>();

	public List<CargoCompartmentData> CargoCompartments;

	public List<ItemCompoundType> AllowedItemTypes { get; private set; }

	public override SubSystemType Type => SubSystemType.Fabricator;

	public List<CargoCompartmentData> Compartments => CargoCompartments;

	public override bool CanBlueprintForceState => false;

	public virtual double TimeLeft
	{
		get
		{
			return _TimeLeft;
		}
		set
		{
			if (_TimeLeft != value)
			{
				_TimeLeft = value;
				StatusChanged = true;
			}
		}
	}

	public SubSystemFabricator(SpaceObjectVessel vessel, VesselObjectID id, SubSystemData ssData)
		: base(vessel, id, ssData)
	{
		AllowedItemTypes = null;
	}

	public override void SetAuxData(SystemAuxData auxData)
	{
		SubSystemFabricatorAuxData aux = auxData as SubSystemFabricatorAuxData;
		AllowedItemTypes = aux.AllowedItemTypes == null || aux.AllowedItemTypes.Count == 0 ? null : aux.AllowedItemTypes;
		TimePerResourceUnit = aux.TimePerResourceUnit;
		AttachPoints = ParentVessel.AttachPoints.Values.Where((VesselAttachPoint m) => aux.AttachPoints.Contains(m.InSceneID)).ToList();
		CargoCompartments = aux.CargoCompartments;
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
		return new FabricatorAuxDetails
		{
			CurrentTimeLeft = TimeLeft >= 0.0 ? (float)TimeLeft : 0f,
			ItemsInQueue = ItemsInQueue,
			CargoCompartments = list
		};
	}

	public void Fabricate(ItemCompoundType itemType)
	{
		if (AllowedItemTypes == null || AllowedItemTypes.Contains(itemType))
		{
			ItemsInQueue.Add(itemType);
			StatusChanged = true;
		}
	}

	public async Task Cancel(bool currentItemOnly = false)
	{
		if (ItemsInQueue.Count == 0)
		{
			return;
		}
		if (TimeLeft > 0.0)
		{
			TimeLeft = -1.0;
			Dictionary<ResourceType, float> resources = Item.GetCraftingResources(ItemsInQueue.First());
			foreach (KeyValuePair<ResourceType, float> kv in resources)
			{
				await ChangeQuantityByAsync(GetCompartment().ID, kv.Key, kv.Value, wholeAmount: true);
			}
		}
		if (Status == SystemStatus.OnLine || (Status == SystemStatus.OffLine && SecondaryStatus == SystemSecondaryStatus.Malfunction))
		{
			await GoOffLine(autoRestart: false);
		}
		ItemsInQueue.RemoveAt(0);
		StatusChanged = true;
		if (!currentItemOnly)
		{
			ItemsInQueue.Clear();
		}
	}

	public override async Task Update(double duration)
	{
		await base.Update(duration);
		if (AttachPoints == null || AttachPoints.Count == 0)
		{
			return;
		}
		if (TimeLeft > 0.0 && Status == SystemStatus.OnLine)
		{
			TimeLeft = MathHelper.Clamp(TimeLeft - duration, 0.0, 3.4028234663852886E+38);
		}
		if (TimeLeft == 0.0)
		{
			VesselAttachPoint ap = AttachPoints.FirstOrDefault((VesselAttachPoint m) => m.Item == null);
			if (ap != null)
			{
				ItemCompoundType ict = ItemsInQueue.First();
				await DynamicObject.SpawnDynamicObject(ict.Type, ict.SubType, ict.PartType, ParentVessel, ap.InSceneID, null, null, null, ict.Tier);
				ItemsInQueue.RemoveAt(0);
				StatusChanged = true;
				TimeLeft = -1.0;
			}
			if (Status == SystemStatus.OnLine)
			{
				await GoOffLine(autoRestart: false);
			}
		}
		else
		{
			if (TimeLeft != -1.0 || ItemsInQueue.Count <= 0 || AttachPoints.Count((VesselAttachPoint m) => m.Item == null) <= 0)
			{
				return;
			}
			ItemCompoundType item = ItemsInQueue.First();
			if (!HasEnoughResources(item.Type, item.SubType, item.PartType, item.Tier))
			{
				ItemsInQueue.RemoveAt(0);
				StatusChanged = true;
				return;
			}
			Dictionary<ResourceType, float> resources = Item.GetCraftingResources(item);
			foreach (KeyValuePair<ResourceType, float> kv in resources)
			{
				await ChangeQuantityByAsync(GetCompartment().ID, kv.Key, 0f - kv.Value, wholeAmount: true);
			}
			TimeLeft = resources.Sum((KeyValuePair<ResourceType, float> m) => m.Value) * TimePerResourceUnit;
			await GoOnLine();
		}
	}

	// Why does this override base class functionality?
	public override Task SetDetails(SubSystemDetails details)
	{
		return Task.CompletedTask;
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

	public bool HasEnoughResources(ItemType itemType, GenericItemSubType subType, MachineryPartType partType, int tier)
	{
		Dictionary<ResourceType, float> ingredients = Item.GetCraftingResources(itemType, subType, partType, tier);
		foreach (KeyValuePair<ResourceType, float> kv in ingredients)
		{
			CargoResourceData res = GetCompartment().Resources.FirstOrDefault((CargoResourceData m) => m.ResourceType == kv.Key);
			if (res == null || res.Quantity < kv.Value)
			{
				return false;
			}
		}
		return true;
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
		return new PersistenceObjectAuxDataFabricator
		{
			TimeLeft = TimeLeft,
			ItemsInQueue = ItemsInQueue,
			CargoCompartments = list
		};
	}

	public override void SetPersistenceAuxData(PersistenceData auxData)
	{
		PersistenceObjectAuxDataFabricator data = auxData as PersistenceObjectAuxDataFabricator;
		foreach (CargoCompartmentDetails ccd in data.CargoCompartments)
		{
			CargoCompartmentData cargoCompartment = GetCompartment(ccd.ID);
			cargoCompartment.Resources = new List<CargoResourceData>(ObjectCopier.DeepCopy(ccd.Resources));
		}
		TimeLeft = data.TimeLeft;
		ItemsInQueue = data.ItemsInQueue;
	}
}
