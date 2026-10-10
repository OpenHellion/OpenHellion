using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity.ShipComponents;

public class GeneratorCapacitor : Generator
{
	private float _MaxCapacity;

	private float _Capacity;

	private float _CapacityChangeRate;

	public override GeneratorType Type => GeneratorType.Capacitor;

	public override DistributionSystemType OutputType => DistributionSystemType.Power;

	public float NominalCapacity { get; private set; }

	public float MaxCapacity
	{
		get
		{
			return _MaxCapacity;
		}
		set
		{
			if (_MaxCapacity != value)
			{
				StatusChanged = true;
			}
			_MaxCapacity = value;
		}
	}

	public float Capacity
	{
		get
		{
			return _Capacity;
		}
		set
		{
			if (_Capacity != value)
			{
				StatusChanged = true;
			}
			_Capacity = value;
		}
	}

	public float CapacityChangeRate
	{
		get
		{
			return _CapacityChangeRate;
		}
		set
		{
			if (_CapacityChangeRate != value)
			{
				StatusChanged = true;
			}
			_CapacityChangeRate = value;
		}
	}

	public override bool FixedConsumption => true;

	private GeneratorCapacitor(SpaceObjectVessel vessel, VesselObjectID id, GeneratorData genData)
		: base(vessel, id, genData)
	{
	}

	public static async Task<GeneratorCapacitor> CreateAsync(SpaceObjectVessel vessel, VesselObjectID id, GeneratorData genData)
	{
		GeneratorCapacitor capacitor = new(vessel, id, genData);

		await capacitor.GoOnLine();

		return capacitor;
	}

	public override IAuxDetails GetAuxDetails()
	{
		return new GeneratorCapacitorAuxDetails
		{
			MaxCapacity = MaxCapacity,
			Capacity = Capacity,
			CapacityChangeRate = CapacityChangeRate
		};
	}

	public override void SetAuxData(SystemAuxData auxData)
	{
		GeneratorCapacitorAuxData aux = auxData as GeneratorCapacitorAuxData;
		NominalCapacity = aux.NominalCapacity;
		Capacity = aux.Capacity;
	}

	public override async Task Update(double duration)
	{
		await base.Update(duration);
		MaxCapacity = NominalCapacity * GetScopeMultiplier(MachineryPartSlotScope.PowerCapacity);
		if (Capacity > MaxCapacity)
		{
			Capacity = MaxCapacity;
		}
	}

	public override PersistenceData GetPersistenceAuxData()
	{
		return new PersistenceObjectAuxDataCapacitor
		{
			MaxCapacity = MaxCapacity,
			Capacity = Capacity
		};
	}

	public override void SetPersistenceAuxData(PersistenceData auxData)
	{
		PersistenceObjectAuxDataCapacitor aux = auxData as PersistenceObjectAuxDataCapacitor;
		MaxCapacity = aux.MaxCapacity;
		Capacity = aux.Capacity;
	}

	public override Task GoOffLine(bool autoRestart, bool malfunction = false)
	{
		return base.GoOnLine();
	}
}
