using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Math;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity.ShipComponents;

public class VesselRepairPoint : IPersistantObject
{
	public enum Priority
	{
		None,
		External,
		Internal,
		Fire,
		Breach,
		System
	}

	public VesselObjectID ID;

	private float _MaxHealth;

	private float _Health;

	public SpaceObjectVessel ParentVessel;

	public bool StatusChanged = true;

	public RepairPointDamageType DamageType;

	public VesselComponent AffectedSystem;

	public IAirConsumer AirCousumer;

	public float MalfunctionThreshold;

	public float RepairThreshold;

	public bool External;

	private bool takingDamage;

	public Room Room { get; set; }

	public float MaxHealth
	{
		get
		{
			return _MaxHealth;
		}
	}

	public float Health
	{
		get
		{
			return _Health;
		}
	}

	private VesselRepairPoint()
	{
	}

	public static async Task<VesselRepairPoint> CreateVesselRepairPointAsync(SpaceObjectVessel vessel, VesselRepairPointData data, float maxHealth)
	{
		VesselRepairPoint repairPoint = new();
		repairPoint.ID = new VesselObjectID(vessel.Guid, data.InSceneID);
		repairPoint.ParentVessel = vessel;
		repairPoint.Room = vessel.Rooms.Find((Room m) => m.ID.InSceneID == data.RoomID);
		repairPoint._MaxHealth = maxHealth;
		repairPoint._Health = maxHealth;
		repairPoint.DamageType = data.DamageType;
		repairPoint.External = data.External;
		repairPoint.AffectedSystem = vessel.Systems.Find((VesselComponent m) => m.ID.InSceneID == data.AffectedSystemID);
		repairPoint.MalfunctionThreshold = data.MalfunctionThreshold;
		repairPoint.RepairThreshold = data.RepairThreshold;
		await repairPoint.Update();

		return repairPoint;
	}

	public async Task Update()
	{
		if (DamageType == RepairPointDamageType.None)
		{
			return;
		}
		float hPerc = _MaxHealth > 0f ? _Health / _MaxHealth : 0f;
		if (DamageType == RepairPointDamageType.System && AffectedSystem != null)
		{
			if (hPerc <= MalfunctionThreshold && !AffectedSystem.Defective && AffectedSystem.Status == SystemStatus.OnLine && takingDamage)
			{
				await AffectedSystem.SetDefectiveAsync(true);
			}
			else if (hPerc >= RepairThreshold && AffectedSystem.Defective)
			{
				await AffectedSystem.SetDefectiveAsync(false);
			}
		}
		else if (DamageType == RepairPointDamageType.Gravity && Room != null)
		{
			if (hPerc <= MalfunctionThreshold && !Room.GravityMalfunction)
			{
				Room.GravityMalfunction = true;
			}
			else if (hPerc >= RepairThreshold && Room.GravityMalfunction)
			{
				Room.GravityMalfunction = false;
			}
		}
		else
		{
			if ((DamageType != RepairPointDamageType.Breach && DamageType != RepairPointDamageType.Fire) || Room == null)
			{
				return;
			}
			if (hPerc <= MalfunctionThreshold && AirCousumer == null)
			{
				if (DamageType == RepairPointDamageType.Breach)
				{
					AirCousumer = new AirConsumerBreach(BreachType.Small);
				}
				else
				{
					AirCousumer = new AirConsumerFire(FireType.Small)
					{
						Persistent = true
					};
				}
				if (AirCousumer != null)
				{
					Room.AddAirConsumer(AirCousumer);
					Room.StatusChanged = true;
				}
			}
			else if (hPerc >= RepairThreshold && AirCousumer != null)
			{
				Room.RemoveAirConsumer(AirCousumer);
				AirCousumer = null;
				Room.StatusChanged = true;
			}
		}
	}

	public async Task SetHealthAsync(float health)
	{
		float newHealth = MathHelper.Clamp(MaxHealth - health < 1f ? MaxHealth : health, 0f, MaxHealth);
		if (_Health != newHealth)
		{
			StatusChanged = true;
			takingDamage = newHealth < _Health;
		}
		_Health = newHealth;
		await Update();
	}

	public PersistenceObjectData GetPersistenceData()
	{
		return new PersistenceObjectDataRepairPoint
		{
			GUID = ParentVessel.Guid,
			InSceneID = ID.InSceneID,
			MaxHealth = MaxHealth,
			Health = Health
		};
	}

	public async Task LoadPersistenceData(PersistenceObjectData persistenceData)
	{
		if (persistenceData is not PersistenceObjectDataRepairPoint data)
		{
			Debug.LogWarning("PersistenceObjectDataRoom data is null");
			return;
		}
		_MaxHealth = data.MaxHealth;
		_Health = data.Health;
		await Update();
	}

	public VesselRepairPointDetails GetDetails()
	{
		bool dmgSpecActive = (DamageType == RepairPointDamageType.System && AffectedSystem is { Defective: true }) || (DamageType is RepairPointDamageType.Fire or RepairPointDamageType.Breach && AirCousumer != null);
		return new VesselRepairPointDetails
		{
			InSceneID = ID.InSceneID,
			MaxHealth = MaxHealth,
			Health = Health,
			SecondaryDamageActive = dmgSpecActive
		};
	}
}
