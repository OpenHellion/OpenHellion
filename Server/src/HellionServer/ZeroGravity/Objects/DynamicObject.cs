using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenHellion.Net;
using OpenHellion.Net.Message;
using OpenHellion.State;
using ZeroGravity.Data;
using ZeroGravity.Math;
using ZeroGravity.Network;
using ZeroGravity.ShipComponents;
using ZeroGravity.Spawn;

namespace ZeroGravity.Objects;

public class DynamicObject : SpaceObjectTransferable, IPersistantObject
{
	public short ItemID;

	public ItemType ItemType;

	private DateTime lastSenderTime;

	private bool pickedUp;

	public DynamicObjectSceneData DynamicObjectSceneData;

	public AttachPointDetails APDetails;

	public Item Item;

	public float RespawnTime = -1f;

	public float SpawnMaxHealth = -1f;

	public float SpawnMinHealth = -1f;

	public float SpawnWearMultiplier = 1f;

	public Vector3D AngularVelocity;

	public override SpaceObjectType ObjectType => SpaceObjectType.DynamicObject;

	public long MasterClientID { get; private set; }

	public ItemId Row { get; private set; }

	private static SolarSystemState State => Server.Instance.SolarSystem.State;

	public bool IsAttached => State.Location(Row).IsSlot;

	public short InvSlotID => State.Location(Row) is { Kind: LocationKind.Inventory } location ? location.SlotId : InventorySlot.NoneSlotID;

	public double LastChangeTime { get; private set; }

	public override SpaceObject Parent
	{
		get
		{
			if (!State.IsAlive(Row))
			{
				return null;
			}
			ItemLocation location = State.Location(Row);
			return location.Kind switch
			{
				LocationKind.None => null,
				LocationKind.Floating => Server.Instance.GetSpaceObject(Guid),
				_ => Server.Instance.ResolveKey(location.ParentKey)
			};
		}
		set => throw new InvalidOperationException("Items are moved with DynamicObject.MoveTo.");
	}

	/// <summary>
	/// 	The only way an item changes location. Leaving a pivot, a machinery slot or a corpse is handled here.
	/// </summary>
	public bool MoveTo(ItemLocation location)
	{
		ItemLocation previous = State.Location(Row);
		SpaceObject previousParent = Parent;
		if (previous.Equals(location))
		{
			return true;
		}
		if (!State.CanMoveItem(Row, location))
		{
			return false;
		}
		if (previous is { Kind: LocationKind.Inventory, SlotId: InventorySlot.OutfitSlotID } && Item is Outfit)
		{
			((previousParent as Player)?.PlayerInventory ?? (previousParent as Corpse)?.CorpseInventory)?.ReleaseOutfit();
		}
		if (!State.TryMoveItem(Row, location))
		{
			return false;
		}

		if (previous.Kind == LocationKind.AttachPoint && Item is MachineryPart && Server.Instance.GetVessel(previous.ParentKey) is { } vessel)
		{
			vessel.RemoveMachineryPart(new VesselObjectID(previous.ParentKey, previous.SlotId));
		}
		if (previousParent is Pivot pivot && location.Kind != LocationKind.Floating)
		{
			Server.Instance.SolarSystem.RemoveArtificialBody(pivot);
		}
		LastChangeTime = Server.SolarSystemTime;
		if (location.Kind == LocationKind.Floating)
		{
			Server.Instance.SubscribeToTimer(UpdateTimer.TimerStep.Step_1_0_min, SelfDestructCheck);
		}
		else
		{
			Server.Instance.UnsubscribeFromTimer(UpdateTimer.TimerStep.Step_1_0_min, SelfDestructCheck);
		}
		if (Item != null)
		{
			Item.AttachmentChangeTime = Server.SolarSystemTime;
		}
		if (previousParent is Corpse corpse && previousParent != Parent)
		{
			corpse.CheckInventoryDestroy();
		}
		return true;
	}

	public void Float(ArtificialBody reference)
	{
		if (State.Location(Row).Kind != LocationKind.Floating)
		{
			_ = new Pivot(this, reference is SpaceObjectVessel vessel ? vessel.MainVessel : reference);
			MoveTo(new ItemLocation(LocationKind.Floating, 0, 0));
		}
	}

	/// <summary>
	/// 	Puts the item loose into a vessel or afloat in space, where the sender left it.
	/// </summary>
	public void Place(SpaceObject target, Player sender, float[] position, float[] rotation, float[] velocity, float[] torque, float[] throwForce)
	{
		if (target is SpaceObjectVessel vessel)
		{
			if (!MoveTo(new ItemLocation(LocationKind.Loose, vessel.Guid, 0)))
			{
				return;
			}
		}
		else if ((GetParent<ArtificialBody>(Parent) ?? GetParent<ArtificialBody>(sender)) is { } reference)
		{
			Float(reference);
		}
		else
		{
			return;
		}

		if (position != null && rotation != null)
		{
			LocalPosition = position.ToVector3D();
			LocalRotation = rotation.ToQuaternionD();
		}
		if (velocity != null || torque != null || throwForce != null)
		{
			State.SetImpulse(Row, ToVector3(velocity), ToVector3(torque), ToVector3(throwForce));
		}
		if (Parent is not SpaceObjectVessel || sender.Parent == Parent)
		{
			MasterClientID = sender.Guid;
			lastSenderTime = DateTime.UtcNow;
		}
	}

	private static System.Numerics.Vector3 ToVector3(float[] values)
	{
		return values is { Length: 3 } ? new System.Numerics.Vector3(values[0], values[1], values[2]) : System.Numerics.Vector3.Zero;
	}

	public void PickedUp()
	{
		if (!pickedUp)
		{
			pickedUp = true;
			if (RespawnTime > 0f)
			{
				Server.Instance.DynamicObjectsRespawnList.Add(new Server.DynamicObjectsRespawn
				{
					Data = DynamicObjectSceneData,
					Parent = Parent,
					Timer = RespawnTime,
					RespawnTime = RespawnTime,
					MaxHealth = SpawnMaxHealth,
					MinHealth = SpawnMinHealth,
					ApDetails = APDetails
				});
			}
			if (IsPartOfSpawnSystem)
			{
				SpawnManager.RemoveSpawnSystemObject(this, checkChildren: false);
			}
		}
	}

	private DynamicObject(DynamicObjectSceneData dosd, long guid = -1L)
		: base(guid == -1 ? GUIDFactory.NextObjectGUID() : guid, dosd.Position.ToVector3D(), QuaternionD.LookRotation(dosd.Forward.ToVector3D(), dosd.Up.ToVector3D()))
	{}

	public static async Task<DynamicObject> CreateDynamicObjectAsync(DynamicObjectSceneData dosd, SpaceObject parent, long guid = -1L, bool ignoreSpawnSettings = false)
	{
		var dynamicObject = new DynamicObject(dosd, guid)
		{
			DynamicObjectSceneData = ObjectCopier.DeepCopy(dosd)
		};
		if (ignoreSpawnSettings)
		{
			dynamicObject.DynamicObjectSceneData.SpawnSettings = null;
		}
		dynamicObject.ItemID = dynamicObject.DynamicObjectSceneData.ItemID;
		dynamicObject.ItemType = StaticData.DynamicObjectsDataList[dynamicObject.ItemID].ItemType;
		dynamicObject.Row = State.CreateItem(dynamicObject.Guid, dynamicObject.ItemID);
		if (parent is SpaceObjectVessel vessel)
		{
			dynamicObject.MoveTo(new ItemLocation(LocationKind.Loose, vessel.Guid, 0));
		}
		dynamicObject.Item = await Item.Create(dynamicObject, dynamicObject.ItemType, dynamicObject.DynamicObjectSceneData.AuxData);
		if (!ignoreSpawnSettings && parent is SpaceObjectVessel spawnVessel)
		{
			dynamicObject.Item?.ApplyCargoSpawnSettings(spawnVessel);
		}
		Server.Instance.Add(dynamicObject);
		dynamicObject.LastChangeTime = Server.Instance.SolarSystem.CurrentTime;

		return dynamicObject;
	}

	private async Task SelfDestructCheck(double dbl)
	{
		if (Parent is Pivot && (DateTime.UtcNow - lastSenderTime).TotalSeconds >= 300.0)
		{
			Server.Instance.UnsubscribeFromTimer(UpdateTimer.TimerStep.Step_1_0_min, SelfDestructCheck);
			await Destroy();
		}
	}

	public DynamicObjectAttachData GetCurrAttachData()
	{
		ItemLocation location = State.Location(Row);
		bool attached = location.IsSlot;
		return new DynamicObjectAttachData
		{
			ParentGUID = location.Kind == LocationKind.Floating ? Guid : location.ParentKey,
			ParentType = Parent?.ObjectType ?? SpaceObjectType.None,
			IsAttached = attached,
			ItemSlotID = location.Kind == LocationKind.ItemSlot ? location.SlotId : (short)0,
			InventorySlotID = InvSlotID,
			APDetails = location.Kind == LocationKind.AttachPoint ? new AttachPointDetails
			{
				InSceneID = location.SlotId
			} : null,
			LocalPosition = attached ? null : LocalPosition.ToFloatArray(),
			LocalRotation = attached ? null : LocalRotation.ToFloatArray()
		};
	}

	public DynamicObjectDetails GetDetails()
	{
		DynamicObjectStats stats = Item?.NewStats();
		Item?.FillStats(stats, ItemChanges.All);
		return new DynamicObjectDetails
		{
			GUID = Guid,
			ItemID = ItemID,
			StatsData = stats,
			AttachData = GetCurrAttachData(),
			LocalPosition = LocalPosition.ToFloatArray(),
			LocalRotation = LocalRotation.ToFloatArray(),
			Velocity = Velocity.ToFloatArray(),
			AngularVelocity = AngularVelocity.ToFloatArray()
		};
	}

	public bool IsVisibleTo(Player viewer)
	{
		return Parent is not Player owner || owner == viewer || InvSlotID is InventorySlot.HandsSlotID or InventorySlot.OutfitSlotID || Item?.Slot?.SlotType == InventorySlot.Type.Equip;
	}

	/// <summary>
	/// 	What a player or corpse carries, as far as the viewer may see it, parents before their contents and
	/// 	the outfit before the items in its slots.
	/// </summary>
	public static DynamicObjectDetails[] GetCarriedDetails(SpaceObject owner, Player viewer)
	{
		List<DynamicObject> carried = [];
		for (ItemId child = State.FirstChild(owner.Key); child.IsValid; child = State.NextSibling(child))
		{
			if (Server.Instance.GetDynamicObject(State.Guid(child)) is { } dynamicObject && dynamicObject.IsVisibleTo(viewer))
			{
				carried.Add(dynamicObject);
			}
		}

		List<ItemId> items = [];
		foreach (DynamicObject dynamicObject in carried.OrderBy(m => m.InvSlotID == InventorySlot.OutfitSlotID ? 0 : 1))
		{
			items.Add(dynamicObject.Row);
			State.AddDescendants(dynamicObject.Guid, items);
		}
		return [.. items.Select(m => Server.Instance.GetDynamicObject(State.Guid(m)).GetDetails())];
	}

	public override async Task Destroy()
	{
		if (!State.IsAlive(Row))
		{
			return;
		}

		List<ItemId> items = [Row];
		State.AddDescendants(Guid, items);
		for (int i = items.Count - 1; i >= 0; i--)
		{
			await Server.Instance.GetDynamicObject(State.Guid(items[i])).Release();
		}
	}

	private async Task Release()
	{
		ItemLocation location = State.Location(Row);
		Pivot pivot = Parent as Pivot;
		if (location.Kind == LocationKind.AttachPoint && Item is MachineryPart && Server.Instance.GetVessel(location.ParentKey) is { } vessel)
		{
			vessel.RemoveMachineryPart(new VesselObjectID(location.ParentKey, location.SlotId));
		}
		Server.Instance.UnsubscribeFromTimer(UpdateTimer.TimerStep.Step_1_0_min, SelfDestructCheck);
		await NetworkController.SendToClientsSubscribedToParents(new DestroyObjectMessage
		{
			ID = Guid,
			ObjectType = ObjectType
		}, this, -1L);
		if (pivot != null)
		{
			pivot.Child = null;
			Server.Instance.SolarSystem.RemoveArtificialBody(pivot);
		}
		Server.Instance.Remove(this);
		if (IsPartOfSpawnSystem)
		{
			SpawnManager.RemoveSpawnSystemObject(this, checkChildren: false);
		}
		State.DestroyItem(Row);
		Row = ItemId.None;
	}

	public void FillPersistenceData(PersistenceObjectDataDynamicObject data)
	{
		data.GUID = Guid;
		data.ItemID = ItemID;
		data.LocalPosition = LocalPosition.ToFloatArray();
		data.LocalRotation = LocalRotation.ToFloatArray();
		if (!pickedUp && RespawnTime > 0f)
		{
			data.RespawnTime = RespawnTime;
			data.MaxHealth = SpawnMaxHealth;
			data.MinHealth = SpawnMinHealth;
			data.WearMultiplier = SpawnWearMultiplier;
			data.RespawnPosition = DynamicObjectSceneData.Position;
			data.RespawnRotation = QuaternionD.LookRotation(DynamicObjectSceneData.Forward.ToVector3D(), DynamicObjectSceneData.Up.ToVector3D()).ToFloatArray();
			data.RespawnAuxData = DynamicObjectSceneData.AuxData;
		}
		data.ChildObjects = [];
		for (ItemId child = State.FirstChild(Guid); child.IsValid; child = State.NextSibling(child))
		{
			if (Server.Instance.GetDynamicObject(State.Guid(child)) is { } dobj)
			{
				data.ChildObjects.Add(dobj.Item != null ? dobj.Item.GetPersistenceData() : dobj.GetPersistenceData());
			}
		}
	}

	public PersistenceObjectData GetPersistenceData()
	{
		PersistenceObjectDataDynamicObject data = new PersistenceObjectDataDynamicObject();
		FillPersistenceData(data);
		return data;
	}

	public Task LoadPersistenceData(PersistenceObjectData persistenceData)
	{
		PersistenceObjectDataDynamicObject data = persistenceData as PersistenceObjectDataDynamicObject;
		ItemID = data.ItemID;
		LocalPosition = data.LocalPosition.ToVector3D();
		LocalRotation = data.LocalRotation.ToQuaternionD();
		pickedUp = false;
		RespawnTime = -1f;
		SpawnMaxHealth = -1f;
		SpawnMinHealth = -1f;
		SpawnWearMultiplier = 1f;
		if (data.RespawnTime.HasValue)
		{
			RespawnTime = data.RespawnTime.Value;
		}
		if (data.MaxHealth.HasValue)
		{
			SpawnMaxHealth = data.MaxHealth.Value;
		}
		if (data.MinHealth.HasValue)
		{
			SpawnMinHealth = data.MinHealth.Value;
		}
		if (data.WearMultiplier.HasValue)
		{
			SpawnWearMultiplier = data.WearMultiplier.Value;
		}

		return Task.CompletedTask;
	}

	public static async Task<bool> SpawnDynamicObject(ItemType itemType, GenericItemSubType subType, MachineryPartType mpType, SpaceObject parent, int apId = -1, Vector3D? position = null, Vector3D? forward = null, Vector3D? up = null, int tier = 1, InventorySlot inventorySlot = null, ItemSlot itemSlot = null, bool refill = false)
	{
		DynamicObjectData dod = null;
		dod = itemType == ItemType.GenericItem
			? ObjectCopier.DeepCopy(StaticData.DynamicObjectsDataList.Values.First((DynamicObjectData m) => m.ItemType == itemType && m.DefaultAuxData is GenericItemData data && data.SubType == subType))
			: itemType != ItemType.MachineryPart
				? ObjectCopier.DeepCopy(StaticData.DynamicObjectsDataList.Values.First((DynamicObjectData m) => m.ItemType == itemType))
				: ObjectCopier.DeepCopy(StaticData.DynamicObjectsDataList.Values.First((DynamicObjectData m) => m.ItemType == itemType
					&& m.DefaultAuxData is MachineryPartData data
					&& data.PartType == mpType));
		if (dod == null)
		{
			return false;
		}
		return await SpawnDynamicObject(dod, parent, apId, position, forward, up, tier, inventorySlot, itemSlot, refill);
	}

	public static async Task<bool> SpawnDynamicObject(DynamicObjectData data, SpaceObject parent, int apId = -1, Vector3D? position = null, Vector3D? forward = null, Vector3D? up = null, int tier = 1, InventorySlot inventorySlot = null, ItemSlot itemSlot = null, bool refill = false)
	{
		DynamicObjectSceneData sceneData = new DynamicObjectSceneData
		{
			ItemID = data.ItemID,
			Position = position.HasValue ? position.Value.ToFloatArray() : Vector3D.Zero.ToFloatArray(),
			Forward = forward.HasValue ? forward.Value.ToFloatArray() : Vector3D.Forward.ToFloatArray(),
			Up = up.HasValue ? up.Value.ToFloatArray() : Vector3D.Up.ToFloatArray(),
			AttachPointInSceneId = apId,
			AuxData = ObjectCopier.DeepCopy(data.DefaultAuxData)
		};
		if (sceneData?.AuxData != null)
		{
			sceneData.AuxData.Tier = tier;
		}
		DynamicObject dobj = await CreateDynamicObjectAsync(sceneData, parent, -1L, ignoreSpawnSettings: true);
		if (dobj.Item == null)
		{
			return true;
		}
		if (dobj.Item.Tier != tier)
		{
			dobj.Item.Tier = tier;
		}
		if (apId > 0 && parent is SpaceObjectVessel vessel)
		{
			vessel.AttachItem(dobj.Item, (short)apId);
			dobj.APDetails = new AttachPointDetails
			{
				InSceneID = apId
			};
		}
		if (inventorySlot != null)
		{
			if (inventorySlot.Item == null)
			{
				inventorySlot.Inventory.AddItemToInventory(dobj.Item, inventorySlot.SlotID);
			}
		}
		else
		{
			itemSlot?.FitItem(dobj.Item);
		}
		if (dobj.Parent == null && parent is ArtificialBody reference)
		{
			dobj.Float(reference);
		}
		if (refill && dobj.Item is ICargo cargo)
		{
			foreach (CargoCompartmentData ccd in cargo.Compartments.Where((CargoCompartmentData m) => m.AllowOnlyOneType))
			{
				using List<CargoResourceData>.Enumerator enumerator2 = ccd.Resources.GetEnumerator();
				if (enumerator2.MoveNext())
				{
					CargoResourceData r = enumerator2.Current;
					await cargo.ChangeQuantityByAsync(ccd.ID, r.ResourceType, ccd.Capacity);
				}
			}
		}
		return true;
	}

	public async Task<DynamicObject> GetCopy()
	{
		return await CreateDynamicObjectAsync(new DynamicObjectSceneData
		{
			ItemID = ItemID,
			Position = Vector3D.Zero.ToFloatArray(),
			Forward = Vector3D.Forward.ToFloatArray(),
			Up = Vector3D.Up.ToFloatArray(),
			AuxData = StaticData.DynamicObjectsDataList[ItemID].DefaultAuxData
		}, this, -1L);;
	}
}
