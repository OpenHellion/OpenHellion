using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenHellion.Net.Message;
using ZeroGravity.Math;
using ZeroGravity.Network;

namespace ZeroGravity.Objects;

public class Corpse : SpaceObjectTransferable
{
	public double DestroyTime = 120000.0;

	public static readonly double ArenaTimer = TimeSpan.FromMinutes(30.0).TotalMilliseconds;

	public static readonly double EmptyCorpseTimer = TimeSpan.FromMinutes(5.0).TotalMilliseconds;

	public static readonly double OutsideTimer = TimeSpan.FromHours(3.0).TotalMilliseconds;

	public static readonly double InsideModuleTimer = TimeSpan.FromHours(24.0).TotalMilliseconds;

	public Vector3D AngularVelocity;

	public double LastChangeTime;

	private double _destroyAt;

	public Gender Gender;

	private SpaceObject _parent;

	public override SpaceObjectType ObjectType => SpaceObjectType.Corpse;

	public Inventory CorpseInventory { get; private set; }

	public override SpaceObject Parent
	{
		get
		{
			return _parent;
		}
		set
		{
			if (_parent != null)
			{
				Parent.Corpses.Remove(Guid);
			}
			_parent = value;
			if (_parent != null)
			{
				Parent.Corpses.Add(Guid);
			}
		}
	}

	public Corpse(Player player)
		: base(GUIDFactory.NextObjectGUID(), player.LocalPosition, player.LocalRotation)
	{
		if (player.Parent is Pivot parent)
		{
			Parent = new Pivot(this, parent);
		}
		else
		{
			Parent = player.Parent;
		}
		LocalPosition = player.LocalPosition;
		LocalRotation = player.LocalRotation;
		CorpseInventory = player.PlayerInventory;
		Server.Instance.Add(this);
		CorpseInventory.ChangeParent(this);
		LastChangeTime = Server.SolarSystemTime;
		if (Parent is SpaceObjectVessel)
		{
			DestroyTime = (Parent as SpaceObjectVessel).IsPrefabStationVessel ? ArenaTimer : InsideModuleTimer;
		}
		else
		{
			DestroyTime = OutsideTimer;
		}
		bool isCorpseEmpty = CorpseInventory.HandsSlot.Item == null;
		if (CorpseInventory.CurrOutfit != null)
		{
			foreach (KeyValuePair<short, InventorySlot> inventorySlot in CorpseInventory.CurrOutfit.InventorySlots)
			{
				if (inventorySlot.Value.Item != null)
				{
					isCorpseEmpty = false;
				}
			}
		}
		if (isCorpseEmpty)
		{
			DestroyTime = EmptyCorpseTimer;
		}
		if (DestroyTime > -1.0)
		{
			_destroyAt = Server.SolarSystemTime + DestroyTime / 1000.0;
			Server.Instance.SubscribeToTimer(UpdateTimer.TimerStep.Step_1_0_sec, DestroyWhenDue);
		}
		Gender = player.Gender;
	}

	private async Task DestroyWhenDue(double deltaTime)
	{
		if (Server.SolarSystemTime >= _destroyAt)
		{
			await Destroy();
		}
	}

	internal void CheckInventoryDestroy()
	{
		if (CorpseInventory.HandsSlot.Item == null && (CorpseInventory.CurrOutfit == null || CorpseInventory.CurrOutfit.InventorySlots.Values.All(m => m.Item == null)))
		{
			_destroyAt = Server.SolarSystemTime + EmptyCorpseTimer / 1000.0;
		}
	}

	public ObjectsInfoResponse.CorpseData GetCorpseData(Player pl)
	{
		return new ObjectsInfoResponse.CorpseData
		{
			Guid = Guid,
			Position = LocalPosition.ToFloatArray(),
			Rotation = LocalRotation.ToFloatArray(),
			ParentGUID = Parent == null ? -1 : Parent.Guid,
			Gender = Gender,
			DynamicObjects = DynamicObject.GetCarriedDetails(this, pl)
		};
	}

	public override async Task Destroy()
	{
		Server.Instance.UnsubscribeFromTimer(UpdateTimer.TimerStep.Step_1_0_sec, DestroyWhenDue);
		await base.Destroy();
	}
}
