using System.Collections.Generic;
using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Math;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity.ShipComponents;

public abstract class SubSystem : VesselComponent
{
	private float _Temperature;

	private bool _AutoTuneOperationRate;

	public abstract SubSystemType Type { get; }

	public bool AutoTuneOperationRate
	{
		get
		{
			return _AutoTuneOperationRate && !FixedConsumption;
		}
		set
		{
			_AutoTuneOperationRate = value;
		}
	}

	public float Temperature
	{
		get
		{
			return _Temperature;
		}
		set
		{
			if (_Temperature != value)
			{
				StatusChanged = true;
			}
			_Temperature = value;
		}
	}

	public abstract void SetAuxData(SystemAuxData auxData);

	public SubSystem(SpaceObjectVessel vessel, VesselObjectID id, SubSystemData ssData)
		: base(vessel, id)
	{
		SubSystemData data = ObjectCopier.DeepCopy(ssData);
		ResourceRequirements = DistributionManager.ResourceRequirementsToDictionary(data.ResourceRequirements);
		if (data.SpawnSettings != null)
		{
			SystemSpawnSettings[] spawnSettings = data.SpawnSettings;
			foreach (SystemSpawnSettings sss in spawnSettings)
			{
				if (!vessel.CheckTag(sss.Tag, sss.Case))
				{
					continue;
				}
				float reqFactor = MathHelper.Clamp(sss.ResourceRequirementMultiplier, 0f, float.MaxValue);
				foreach (KeyValuePair<DistributionSystemType, ResourceRequirement> kv in ResourceRequirements)
				{
					ResourceRequirements[kv.Key].Nominal *= reqFactor;
					ResourceRequirements[kv.Key].Standby *= reqFactor;
				}
				break;
			}
		}
		_PowerUpTime = data.PowerUpTime;
		_CoolDownTime = data.CoolDownTime;
		SetStatus(data.Status);
		_AutoReactivate = data.AutoReactivate;
		shouldAutoReactivate = Status != SystemStatus.OffLine && AutoReactivate;
		OperationRate = data.OperationRate;
		AutoTuneOperationRate = data.AutoTuneOperationRate;
		baseRadarSignature = data.RadarSignature;
		SetAuxData(data.AuxData);
	}

	public virtual IAuxDetails GetAuxDetails()
	{
		return null;
	}

	public virtual async Task SetDetails(SubSystemDetails details)
	{
		if (details.Status == SystemStatus.OnLine)
		{
			await GoOnLine();
		}
		else if (details.Status == SystemStatus.OffLine)
		{
			await GoOffLine(autoRestart: false);
		}
		SetAuxDetails(details.AuxDetails);
	}

	public virtual void SetAuxDetails(IAuxDetails auxDetails)
	{
	}

	public override bool CheckAvailableResources(float consumptionFactor, float duration, bool standby, ref Dictionary<IResourceProvider, float> reservedCapacities, ref Dictionary<ResourceContainer, float> reservedQuantities, ref string debugText)
	{
		if (_OperationRate < 0f)
		{
			return true;
		}
		if (standby || !AutoTuneOperationRate)
		{
			return base.CheckAvailableResources(consumptionFactor, duration, standby, ref reservedCapacities, ref reservedQuantities, ref debugText);
		}
		float incr = 1f;
		float opRate = 1f;
		float targetOpRate = 0f;
		Dictionary<IResourceProvider, float> targetCapacities = new Dictionary<IResourceProvider, float>(reservedCapacities);
		Dictionary<ResourceContainer, float> targetQuantities = new Dictionary<ResourceContainer, float>(reservedQuantities);
		string targetDebugText = debugText;
		string tempDebugText = null;
		for (int i = 0; i < 5; i++)
		{
			Dictionary<IResourceProvider, float> tempCapacities = new Dictionary<IResourceProvider, float>(reservedCapacities);
			Dictionary<ResourceContainer, float> tempQuantities = new Dictionary<ResourceContainer, float>(reservedQuantities);
			tempDebugText = debugText;
			incr /= 2f;
			if (base.CheckAvailableResources(opRate, duration, standby: false, ref tempCapacities, ref tempQuantities, ref tempDebugText))
			{
				if (opRate > targetOpRate)
				{
					targetOpRate = opRate;
					targetCapacities = tempCapacities;
					targetQuantities = tempQuantities;
					targetDebugText = tempDebugText;
				}
				if (targetOpRate >= 1f)
				{
					break;
				}
				opRate += incr;
			}
			else
			{
				opRate -= incr;
			}
		}
		if (targetOpRate > float.Epsilon)
		{
			OperationRate = targetOpRate;
			reservedCapacities = targetCapacities;
			reservedQuantities = targetQuantities;
			return true;
		}
		debugText = tempDebugText;
		return false;
	}
}
