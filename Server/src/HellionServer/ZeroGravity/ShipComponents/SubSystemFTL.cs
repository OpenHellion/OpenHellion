using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Math;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity.ShipComponents;

public class SubSystemFTL : SubSystem
{
	public float BaseTowingCapacity;

	public WarpData[] WarpsData;

	public Dictionary<int, VesselObjectID> WarpCellSlots = new Dictionary<int, VesselObjectID>();

	private bool canGoOnline = true;

	private int _MaxWarp;

	private float _TowingCapacity;

	public override SubSystemType Type => SubSystemType.FTL;

	public override bool AutoReactivate => false;

	public int MaxWarp
	{
		get
		{
			return _MaxWarp;
		}
		protected set
		{
			if (_MaxWarp != value)
			{
				StatusChanged = true;
				_MaxWarp = value;
			}
		}
	}

	public float TowingCapacity
	{
		get
		{
			return _TowingCapacity;
		}
		protected set
		{
			if (_TowingCapacity != value)
			{
				StatusChanged = true;
				_TowingCapacity = value;
			}
		}
	}

	public override SystemStatus Status
	{
		get
		{
			return base.Status;
		}
	}

	public SubSystemFTL(SpaceObjectVessel vessel, VesselObjectID id, SubSystemData ssData)
		: base(vessel, id, ssData)
	{
		OperationRate = 0f;
		_PowerUpTime = 0f;
	}

	public override void FitPartToSlot(VesselObjectID slotKey, MachineryPart part)
	{
		base.FitPartToSlot(slotKey, part);
		StatusChanged = true;
	}

	public override void RemovePartFromSlot(VesselObjectID slotKey)
	{
		base.RemovePartFromSlot(slotKey);
		StatusChanged = true;
	}

	public override void InitMachineryPartSlot(VesselObjectID slotKey, MachineryPart part, MachineryPartSlotData partSlotData)
	{
		base.InitMachineryPartSlot(slotKey, part, partSlotData);
		if (partSlotData.MachineryPartTypes.FirstOrDefault((MachineryPartType m) => m == MachineryPartType.WarpCell) != 0)
		{
			WarpCellSlots[partSlotData.SlotIndex] = slotKey;
		}
	}

	public override void SetAuxData(SystemAuxData auxData)
	{
		SubSystemFTLAuxData aux = auxData as SubSystemFTLAuxData;
		BaseTowingCapacity = aux.BaseTowingCapacity * 1000f;
		WarpsData = aux.WarpsData;
	}

	public override IAuxDetails GetAuxDetails()
	{
		return new FTLAuxDetails
		{
			WarpCellsFuel = GetWarpCellsFuel(),
			MaxWarp = MaxWarp,
			TowingCapacity = TowingCapacity
		};
	}

	protected override async Task SetStatusAsync(SystemStatus status)
	{
		bool setPrimary = false;
		if (Status == SystemStatus.OnLine && status != SystemStatus.OnLine && ParentVessel.CurrentCourse != null)
		{
			await ParentVessel.CurrentCourse.Invalidate();
		}
		else if (Status != SystemStatus.OnLine && status == SystemStatus.OnLine)
		{
			setPrimary = true;
		}
		await base.SetStatusAsync(status);
		if (setPrimary)
		{
			await (ParentVessel as Ship).CheckMainPropulsionVessel();
		}
	}

	public override async Task Update(double duration)
	{
		await base.Update(duration);
		OperationRate = ParentVessel.CurrentCourse is { IsInProgress: true } ? 1 : 0;
		MachineryPart mp = MachineryParts.Values.FirstOrDefault((MachineryPart m) => m is { PartType: MachineryPartType.SingularityCellDetonator });
		if (mp == null)
		{
			MaxWarp = 0;
			TowingCapacity = 0f;
		}
		else
		{
			MaxWarp = MathHelper.Clamp(mp.Tier, 0, WarpsData.Length - 1);
			TowingCapacity = BaseTowingCapacity * mp.TierMultiplier;
		}
	}

	public Dictionary<int, float?> GetWarpCellsFuel()
	{
		Dictionary<int, float?> dict = new Dictionary<int, float?>();
		foreach (KeyValuePair<int, VesselObjectID> kv in WarpCellSlots)
		{
			MachineryPart warpCell = null;
			MachineryParts.TryGetValue(kv.Value, out warpCell);
			if (warpCell != null)
			{
				dict[kv.Key] = warpCell.Health;
			}
			else
			{
				dict[kv.Key] = null;
			}
		}
		return dict;
	}

	public async Task ConsumeWarpResources(List<int> slots, float warpFuel, float warpPower)
	{
		ParentVessel.Capacitor.Capacity = MathHelper.Clamp(ParentVessel.Capacitor.Capacity - warpPower, 0f, ParentVessel.Capacitor.MaxCapacity);
		List<MachineryPart> list = new List<MachineryPart>();
		if (slots is { Count: > 0 } && warpFuel > 0f)
		{
			foreach (int slotIndex in slots)
			{
				VesselObjectID id = null;
				if (WarpCellSlots.TryGetValue(slotIndex, out id))
				{
					MachineryPart warpCell2 = null;
					MachineryParts.TryGetValue(id, out warpCell2);
					if (warpCell2 != null)
					{
						list.Add(warpCell2);
					}
				}
			}
		}
		list.Sort((MachineryPart x, MachineryPart y) => x.Health != y.Health ? x.Health > y.Health ? 1 : -1 : 0);
		foreach (MachineryPart warpCell in list)
		{
			if (warpCell.Health < warpFuel)
			{
				warpFuel -= warpCell.Health;
				warpCell.Health = 0f;
			}
			else
			{
				warpCell.Health -= warpFuel;
				warpFuel = 0f;
			}
		}
	}

	public override bool CheckAvailableResources(float consumptionFactor, float duration, bool standby, ref Dictionary<IResourceProvider, float> reservedCapacities, ref Dictionary<ResourceContainer, float> reservedQuantities, ref string debugText)
	{
		if (!canGoOnline || Defective)
		{
			return false;
		}
		return base.CheckAvailableResources(consumptionFactor, duration, standby, ref reservedCapacities, ref reservedQuantities, ref debugText);
	}
}
