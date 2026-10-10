using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenHellion.State;
using ZeroGravity.Data;
using ZeroGravity.Math;
using ZeroGravity.Network;
using ZeroGravity.ShipComponents;

namespace ZeroGravity.Objects;

public abstract class Item : IPersistantObject, IDamageable
{
	public ItemType Type;

	public float[] TierMultipliers;

	public float[] AuxValues;

	public double AttachmentChangeTime;

	public float MeleeDamage;

	public Dictionary<short, ItemSlot> Slots;

	public float ExplosionRadius;

	public float ExplosionDamage;

	public TypeOfDamage ExplosionDamageType;

	protected bool TierMultiplierApplied;

	protected List<CargoCompartmentData> CargoTemplates;

	private double _destroyAt;

	protected static SolarSystemState State => Server.Instance.SolarSystem.State;

	protected ItemId Row => DynamicObj.Row;

	public short ItemSlotID => State.Location(Row) is { Kind: LocationKind.ItemSlot } location ? location.SlotId : (short)0;

	public InventorySlot Slot
	{
		get
		{
			ItemLocation location = State.Location(Row);
			if (location.Kind != LocationKind.Inventory)
			{
				return null;
			}
			return DynamicObj.Parent switch
			{
				Player player => player.PlayerInventory.GetSlot(location.SlotId),
				Corpse corpse => corpse.CorpseInventory.GetSlot(location.SlotId),
				DynamicObject { Item: Outfit outfit } => outfit.InventorySlots.GetValueOrDefault(location.SlotId),
				_ => null
			};
		}
	}

	public VesselObjectID AttachPointID => State.Location(Row) is { Kind: LocationKind.AttachPoint } location ? new VesselObjectID(location.ParentKey, location.SlotId) : null;

	public AttachPointType AttachPointType
	{
		get
		{
			ItemLocation location = State.Location(Row);
			if (location.Kind != LocationKind.AttachPoint || Server.Instance.GetVessel(location.ParentKey) is not { } vessel)
			{
				return AttachPointType.None;
			}
			return vessel.AttachPointsTypes.GetValueOrDefault(new VesselObjectID(location.ParentKey, location.SlotId));
		}
	}

	public long GUID => DynamicObj.Guid;

	public DynamicObject DynamicObj { get; private set; }

	public int Tier
	{
		get => State.Tier(Row);
		set => State.SetTier(Row, value);
	}

	public float MaxHealth
	{
		get => State.MaxHealth(Row);
		set => State.SetMaxHealth(Row, value < 0f ? 0f : value);
	}

	public float Health
	{
		get => State.Health(Row);
		set => State.SetHealth(Row, value > MaxHealth ? MaxHealth : value < 0f ? 0f : value);
	}

	public float Armor
	{
		get => State.Armor(Row);
		set => State.SetArmor(Row, value < 0f ? 0f : value);
	}

	public bool Damageable { get; set; }

	public bool Repairable { get; set; }

	public float UsageWear { get; set; }

	public ItemCompoundType CompoundType
	{
		get
		{
			if (Type == ItemType.GenericItem)
			{
				ItemCompoundType itemCompoundType = new ItemCompoundType();
				itemCompoundType.Type = Type;
				itemCompoundType.SubType = (this as GenericItem).SubType;
				itemCompoundType.PartType = MachineryPartType.None;
				itemCompoundType.Tier = Tier;
				return itemCompoundType;
			}
			if (Type == ItemType.MachineryPart)
			{
				ItemCompoundType itemCompoundType = new ItemCompoundType();
				itemCompoundType.Type = Type;
				itemCompoundType.SubType = GenericItemSubType.None;
				itemCompoundType.PartType = (this as MachineryPart).PartType;
				itemCompoundType.Tier = Tier;
				return itemCompoundType;
			}
			return new ItemCompoundType
			{
				Type = Type,
				SubType = GenericItemSubType.None,
				PartType = MachineryPartType.None,
				Tier = Tier
			};
		}
	}

	public string TypeName
	{
		get
		{
			if (this is GenericItem)
			{
				return (this as GenericItem).SubType.ToString();
			}
			if (this is MachineryPart)
			{
				return (this as MachineryPart).PartType.ToString();
			}
			return Type.ToString();
		}
	}

	public float TierMultiplier
	{
		get
		{
			if (Tier < 1 || TierMultipliers == null || Tier > TierMultipliers.Length)
			{
				return 1f;
			}
			return TierMultipliers[Tier - 1];
		}
	}

	public float AuxValue
	{
		get
		{
			if (Tier < 1 || AuxValues == null || Tier > AuxValues.Length)
			{
				return 0f;
			}
			return AuxValues[Tier - 1];
		}
	}

	internal async Task<Item> GetCopy()
	{
		return (await DynamicObj.GetCopy()).Item;
	}

	internal void AutoTransferResources()
	{
		ICargo cargo = this as ICargo;
		CargoCompartmentData comp = cargo.GetCompartment();
		Ship ship = DynamicObj.Parent as Ship;
		foreach (ResourceContainer rc in ship.DistributionManager.GetResourceContainers())
		{
			foreach (CargoCompartmentData ccd in rc.Compartments)
			{
				if (ccd.Type != CargoCompartmentType.RCS && ccd.Type != CargoCompartmentType.AirGeneratorOxygen && ccd.Type != CargoCompartmentType.AirGeneratorNitrogen && ccd.Type != CargoCompartmentType.PowerGenerator && ccd.Type != CargoCompartmentType.Engine)
				{
					continue;
				}
				List<CargoResourceData> resources = new List<CargoResourceData>(comp.Resources);
				foreach (CargoResourceData res in resources)
				{
					Server.Instance.TransferResources(cargo, comp.ID, rc, ccd.ID, res.ResourceType, res.Quantity);
				}
			}
		}
	}

	public static async Task<Item> Create(DynamicObject dobj, ItemType type, DynamicObjectAuxData data)
	{
		Item it = null;
		if (ItemTypeRange.IsHelmet(type))
		{
			it = new Helmet();
		}
		else if (ItemTypeRange.IsJetpack(type))
		{
			it = new Jetpack();
		}
		else if (ItemTypeRange.IsWeapon(type))
		{
			it = new Weapon();
		}
		else if (ItemTypeRange.IsOutfit(type))
		{
			it = new Outfit();
		}
		else if (ItemTypeRange.IsAmmo(type))
		{
			it = new Magazine();
		}
		else if (ItemTypeRange.IsMachineryPart(type))
		{
			it = new MachineryPart();
		}
		else if (ItemTypeRange.IsBattery(type))
		{
			it = new Battery();
		}
		else if (ItemTypeRange.IsCanister(type))
		{
			it = new Canister();
		}
		else if (ItemTypeRange.IsDrill(type))
		{
			it = new HandDrill();
		}
		else if (ItemTypeRange.IsMelee(type))
		{
			it = new MeleeWeapon();
		}
		else if (ItemTypeRange.IsGlowStick(type))
		{
			it = new GlowStick();
		}
		else if (ItemTypeRange.IsMedpack(type))
		{
			it = new Medpack();
		}
		else if (ItemTypeRange.IsHackingTool(type))
		{
			it = new DisposableHackingTool();
		}
		else if (ItemTypeRange.IsAsteroidScanningTool(type))
		{
			it = new HandheldAsteroidScanner();
		}
		else if (ItemTypeRange.IsLogItem(type))
		{
			it = new LogItem();
		}
		else if (ItemTypeRange.IsGenericItem(type))
		{
			it = new GenericItem();
		}
		else if (ItemTypeRange.IsGrenade(type))
		{
			it = new Grenade();
		}
		else if (ItemTypeRange.IsPortableTurret(type))
		{
			it = new PortableTurret();
		}
		else if (ItemTypeRange.IsRepairTool(type))
		{
			it = new RepairTool();
		}
		if (it == null)
		{
			return null;
		}

		it.Type = type;
		it.DynamicObj = dobj;
		data ??= ObjectCopier.DeepCopy(StaticData.DynamicObjectsDataList[dobj.ItemID].DefaultAuxData);
		if (data == null)
		{
			return it;
		}

		await it.SetData(data);
		if (it is Helmet helmet)
		{
			helmet.IsVisorActive = true;
		}
		foreach (ItemSlotData isd in data.Slots ?? [])
		{
			if (it.Slots.TryGetValue(isd.ID, out var isl) && (isd.SpawnItem.Type != 0 || isd.SpawnItem.SubType != 0 || isd.SpawnItem.PartType != 0))
			{
				await DynamicObject.SpawnDynamicObject(isd.SpawnItem.Type, isd.SpawnItem.SubType, isd.SpawnItem.PartType, it.DynamicObj, -1, null, null, null, itemSlot: isl, tier: isd.SpawnItem.Tier);
			}
		}
		return it;
	}

	public virtual Task SetData(DynamicObjectAuxData data)
	{
		Tier = data.Tier;
		TierMultipliers = data.TierMultipliers;
		AuxValues = data.AuxValues;
		MaxHealth = data.MaxHealth;
		Health = data.Health;
		Armor = data.Armor;
		Damageable = data.Damageable;
		Repairable = data.Repairable;
		UsageWear = data.UsageWear;
		MeleeDamage = data.MeleeDamage;
		ExplosionDamage = data.ExplosionDamage;
		ExplosionDamageType = data.ExplosionDamageType;
		ExplosionRadius = data.ExplosionRadius;
		if (Slots == null && data.Slots != null)
		{
			Slots = data.Slots.ToDictionary((ItemSlotData k) => k.ID, (ItemSlotData v) => new ItemSlot(v)
			{
				Parent = DynamicObj
			});
		}

		return Task.CompletedTask;
	}

	/// <summary>
	/// 	A new, empty stats message of this item's own type, so the client can read it as such.
	/// </summary>
	public virtual DynamicObjectStats NewStats()
	{
		return new DynamicObjectStats();
	}

	/// <summary>
	/// 	Writes the requested fields of this item's state into a stats message. ItemChanges.All also writes
	/// 	the values that never change after creation.
	/// </summary>
	public virtual void FillStats(DynamicObjectStats stats, ItemChanges fields)
	{
		if ((fields & ItemChanges.Health) != 0)
		{
			stats.Health = Health;
		}
		if ((fields & ItemChanges.MaxHealth) != 0)
		{
			stats.MaxHealth = MaxHealth;
		}
		if ((fields & ItemChanges.Armor) != 0)
		{
			stats.Armor = Armor;
		}
		if ((fields & ItemChanges.Tier) != 0)
		{
			stats.Tier = Tier;
		}
	}

	protected virtual bool KeepsEmptyResource(CargoCompartmentData compartment)
	{
		return compartment.AllowOnlyOneType;
	}

	protected void LoadCargo(List<CargoCompartmentData> templates)
	{
		CargoTemplates = templates;
		for (int resource = State.FirstResource(Row); resource >= 0; resource = State.FirstResource(Row))
		{
			State.RemoveResource(Row, State.ResourceCompartment(resource), State.ResourceType(resource));
		}
		foreach (CargoCompartmentData template in templates)
		{
			foreach (CargoResourceData resource in template.Resources ?? [])
			{
				if (KeepsEmptyResource(template) || resource.Quantity > float.Epsilon)
				{
					State.SetResource(Row, template.ID, (short)resource.ResourceType, resource.Quantity);
				}
			}
		}
	}

	public List<CargoCompartmentData> Compartments => CargoTemplates?.Select(CargoView).ToList();

	public CargoCompartmentData GetCompartment(int? id = null)
	{
		CargoCompartmentData template = id.HasValue ? CargoTemplates.Find(m => m.ID == id.Value) : CargoTemplates[0];
		return template == null ? null : CargoView(template);
	}

	private CargoCompartmentData CargoView(CargoCompartmentData template)
	{
		List<CargoResourceData> resources = [];
		for (int resource = State.FirstResource(Row); resource >= 0; resource = State.NextResource(resource))
		{
			if (State.ResourceCompartment(resource) == template.ID)
			{
				resources.Add(new CargoResourceData
				{
					ResourceType = (ResourceType)State.ResourceType(resource),
					Quantity = State.ResourceQuantity(resource)
				});
			}
		}
		return new CargoCompartmentData
		{
			ID = template.ID,
			AllowedResources = template.AllowedResources,
			AllowOnlyOneType = template.AllowOnlyOneType,
			Capacity = template.Capacity,
			Type = template.Type,
			Resources = resources
		};
	}

	protected float ChangeCargoQuantity(int compartmentId, ResourceType resourceType, float quantity)
	{
		CargoCompartmentData template = CargoTemplates.Find(m => m.ID == compartmentId);
		if (template == null)
		{
			return 0f;
		}

		float stored = 0f;
		for (int resource = State.FirstResource(Row); resource >= 0; resource = State.NextResource(resource))
		{
			if (State.ResourceCompartment(resource) == template.ID)
			{
				stored += State.ResourceQuantity(resource);
			}
		}
		float available = State.ResourceQuantity(Row, template.ID, (short)resourceType);
		float qty = quantity > 0f ? MathHelper.Clamp(quantity, 0f, template.Capacity - stored) : -MathHelper.Clamp(-quantity, 0f, available);
		float result = available + qty;
		if (result <= float.Epsilon && !KeepsEmptyResource(template))
		{
			State.RemoveResource(Row, template.ID, (short)resourceType);
		}
		else
		{
			State.SetResource(Row, template.ID, (short)resourceType, result);
		}
		return qty;
	}

	public void ApplyCargoSpawnSettings(SpaceObjectVessel vessel)
	{
		foreach (CargoCompartmentData template in CargoTemplates ?? [])
		{
			foreach (CargoResourceData resource in (template.Resources ?? []).Where(m => m.SpawnSettings != null))
			{
				ResourcesSpawnSettings settings = resource.SpawnSettings.FirstOrDefault(m => vessel.CheckTag(m.Tag, m.Case));
				if (settings == null)
				{
					continue;
				}
				ChangeCargoQuantity(template.ID, resource.ResourceType, -State.ResourceQuantity(Row, template.ID, (short)resource.ResourceType));
				ChangeCargoQuantity(template.ID, resource.ResourceType, MathHelper.RandomRange(settings.MinQuantity, settings.MaxQuantity));
			}
		}
	}

	public void FillPersistenceData(PersistenceObjectDataItem data)
	{
		DynamicObj.FillPersistenceData(data);
		data.GUID = GUID;
		data.Health = Health;
		data.Armor = Armor;
		data.Tier = Tier;
		if (AttachPointType != 0)
		{
			data.AttachPointType = AttachPointType;
			data.AttachPointID = AttachPointID.InSceneID;
		}
		if (Slot != null)
		{
			data.SlotID = Slot.SlotID;
		}
		if (ItemSlotID > 0)
		{
			data.ItemSlotID = ItemSlotID;
		}
	}

	public virtual PersistenceObjectData GetPersistenceData()
	{
		PersistenceObjectDataItem data = new PersistenceObjectDataItem();
		FillPersistenceData(data);
		return data;
	}

	public virtual async Task DestroyItem()
	{
		await DynamicObj.Destroy();
	}

	public bool IsDestroyScheduled => _destroyAt > 0.0;

	public void DestroyAfter(double seconds)
	{
		if (!IsDestroyScheduled)
		{
			Server.Instance.SubscribeToTimer(UpdateTimer.TimerStep.Step_0_1_sec, DestroyWhenDue);
		}
		_destroyAt = Server.SolarSystemTime + seconds;
	}

	private async Task DestroyWhenDue(double deltaTime)
	{
		bool alive = DynamicObj != null && State.IsAlive(Row);
		if (alive && Server.SolarSystemTime < _destroyAt)
		{
			return;
		}
		Server.Instance.UnsubscribeFromTimer(UpdateTimer.TimerStep.Step_0_1_sec, DestroyWhenDue);
		if (alive)
		{
			await DestroyItem();
		}
	}

	public virtual async Task LoadPersistenceData(PersistenceObjectData persistenceData)
	{
		PersistenceObjectDataItem data = persistenceData as PersistenceObjectDataItem;
		await DynamicObj.LoadPersistenceData(data);
		Health = data.Health;
		Armor = data.Armor;
	}

	public virtual async Task TakeDamage(TypeOfDamage type, float damage, bool forceTakeDamage = false)
	{
		await TakeDamage(new Dictionary<TypeOfDamage, float> { { type, damage } }, forceTakeDamage);
	}

	public virtual async Task TakeDamage(Dictionary<TypeOfDamage, float> damages, bool forceTakeDamage = false)
	{
		if (!forceTakeDamage && !Damageable)
		{
			return;
		}
		float amount = 0f;
		foreach (KeyValuePair<TypeOfDamage, float> damage in damages)
		{
			amount += damage.Value;
		}
		if (!((amount -= Armor) < float.Epsilon))
		{
			Health -= amount;
		}
	}

	public void FillBaseAuxData<T>(T data) where T : DynamicObjectAuxData
	{
		data.ItemType = Type;
		data.Tier = Tier;
		data.TierMultipliers = TierMultipliers;
		data.MaxHealth = MaxHealth;
		data.Health = Health;
		data.Armor = Armor;
		data.Damageable = Damageable;
		data.Repairable = Repairable;
		data.UsageWear = UsageWear;
		data.MeleeDamage = MeleeDamage;
		data.ExplosionRadius = ExplosionRadius;
		data.ExplosionDamage = ExplosionDamage;
		data.ExplosionDamageType = ExplosionDamageType;
		data.Slots = Slots.Values.Select((ItemSlot m) => m.GetData()).ToList();
	}

	public static Dictionary<ResourceType, float> GetRecycleResources(Item item)
	{
		if (item is GenericItem genericItem)
		{
			return GetRecycleResources(genericItem.Type, genericItem.SubType, MachineryPartType.None, genericItem.Tier);
		}
		if (item is MachineryPart part)
		{
			return GetRecycleResources(part.Type, GenericItemSubType.None, part.PartType, part.Tier);
		}
		return GetRecycleResources(item.Type, GenericItemSubType.None, MachineryPartType.None, item.Tier);
	}

	public static Dictionary<ResourceType, float> GetRecycleResources(ItemType itemType, GenericItemSubType subType, MachineryPartType partType, int tier)
	{
		ItemIngredientsData ingredientsData = StaticData.ItemsIngredients.FirstOrDefault((ItemIngredientsData m) => m.Type == itemType && m.SubType == subType && m.PartType == partType);
		if (ingredientsData != null)
		{
			KeyValuePair<int, ItemIngredientsTierData>? kv = ingredientsData.IngredientsTiers.OrderBy((KeyValuePair<int, ItemIngredientsTierData> m) => m.Key).Reverse().FirstOrDefault((KeyValuePair<int, ItemIngredientsTierData> m) => m.Key <= tier);
			if (kv.HasValue && kv.Value.Value.Recycle is { Count: > 0 })
			{
				return kv.Value.Value.Recycle;
			}
		}
		return null;
	}

	public static Dictionary<ResourceType, float> GetCraftingResources(Item item)
	{
		if (item is GenericItem genericItem)
		{
			return GetCraftingResources(genericItem.Type, genericItem.SubType, MachineryPartType.None, genericItem.Tier);
		}
		if (item is MachineryPart part)
		{
			return GetCraftingResources(part.Type, GenericItemSubType.None, part.PartType, part.Tier);
		}
		return GetCraftingResources(item.Type, GenericItemSubType.None, MachineryPartType.None, item.Tier);
	}

	public static Dictionary<ResourceType, float> GetCraftingResources(ItemCompoundType compoundType)
	{
		return GetCraftingResources(compoundType.Type, compoundType.SubType, compoundType.PartType, compoundType.Tier);
	}

	public static Dictionary<ResourceType, float> GetCraftingResources(ItemType itemType, GenericItemSubType subType, MachineryPartType partType, int tier)
	{
		ItemIngredientsData ingredientsData = StaticData.ItemsIngredients.FirstOrDefault((ItemIngredientsData m) => m.Type == itemType && m.SubType == subType && m.PartType == partType);
		if (ingredientsData != null)
		{
			KeyValuePair<int, ItemIngredientsTierData>? kv = ingredientsData.IngredientsTiers.OrderBy((KeyValuePair<int, ItemIngredientsTierData> m) => m.Key).Reverse().FirstOrDefault((KeyValuePair<int, ItemIngredientsTierData> m) => m.Key <= tier);
			if (kv.HasValue && kv.Value.Value.Craft is { Count: > 0 })
			{
				return kv.Value.Value.Craft;
			}
		}
		return null;
	}

	public virtual void ApplyTierMultiplier()
	{
		TierMultiplierApplied = true;
	}
}
