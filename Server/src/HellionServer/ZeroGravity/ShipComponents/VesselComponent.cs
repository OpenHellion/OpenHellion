using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Math;
using ZeroGravity.Network;
using ZeroGravity.Objects;
using static ZeroGravity.ShipComponents.DistributionManager;

namespace ZeroGravity.ShipComponents;

public abstract class VesselComponent : IResourceConsumer, IResourceUser, IPersistantObject
{
	public Dictionary<VesselObjectID, MachineryPart> MachineryParts = new Dictionary<VesselObjectID, MachineryPart>();

	public Dictionary<VesselObjectID, MachineryPartSlotData> MachineryPartSlots = new Dictionary<VesselObjectID, MachineryPartSlotData>();

	protected float _PowerUpTime;

	protected float _CoolDownTime;

	protected bool _AutoReactivate;

	protected float _InputFactor = 1f;

	protected float _PowerInputFactor = 1f;

	protected float _PartsWearFactor = 1f;

	private bool _Defective;

	protected float powerUpFactor = 1f;

	protected float cooldownFactor = 1f;

	protected SystemStatus _Status = SystemStatus.None;

	public string _DebugInfo;

	protected SystemSecondaryStatus _SecondaryStatus = SystemSecondaryStatus.None;

	protected float _OperationRate;

	protected float baseRadarSignature = 0f;

	protected float radarSignatureMultiplier = 1f;

	private Dictionary<DistributionSystemType, SortedSet<IResourceProvider>> _ConnectedProviders = new Dictionary<DistributionSystemType, SortedSet<IResourceProvider>>();

	private Dictionary<DistributionSystemType, ResourceRequirement> _ResourcesConsumption = new Dictionary<DistributionSystemType, ResourceRequirement>();

	private Dictionary<DistributionSystemType, HashSet<ResourceContainer>> _ResourceContainers = new Dictionary<DistributionSystemType, HashSet<ResourceContainer>>();

	protected bool shouldAutoReactivate;

	public SpaceObjectVessel ParentVessel;

	private float statusChangeCountdown;

	public bool StatusChanged = true;

	public bool IsPowerConsumer;

	public VesselObjectID ID { get; set; }

	public Room Room { get; set; }

	public virtual bool AutoReactivate => _AutoReactivate;

	public bool CanReactivate => _AutoReactivate && shouldAutoReactivate;

	public virtual float PowerUpTime => _PowerUpTime * powerUpFactor;

	public virtual bool FixedConsumption => true;

	public virtual bool CanBlueprintForceState => true;

	public float RadarSignature
	{
		get
		{
			if (_Status is SystemStatus.OnLine or SystemStatus.CoolDown)
			{
				return baseRadarSignature * radarSignatureMultiplier;
			}
			return 0f;
		}
	}

	public bool Defective
	{
		get
		{
			return _Defective;
		}
	}

	public virtual float CoolDownTime => _CoolDownTime * cooldownFactor;

	public virtual float InputFactor
	{
		get
		{
			return _InputFactor;
		}
		set
		{
			if (_InputFactor != value)
			{
				_InputFactor = value;
				StatusChanged = true;
			}
		}
	}

	public virtual float PowerInputFactor
	{
		get
		{
			return _PowerInputFactor;
		}
		set
		{
			if (_PowerInputFactor != value)
			{
				_PowerInputFactor = value;
				StatusChanged = true;
			}
		}
	}

	public virtual float PartsWearFactor
	{
		get
		{
			return _PartsWearFactor;
		}
		set
		{
			if (_PartsWearFactor != value)
			{
				_PartsWearFactor = value;
				StatusChanged = true;
			}
		}
	}

	public string DebugInfo
	{
		get
		{
			return _DebugInfo;
		}
		set
		{
			if (_DebugInfo != value)
			{
				_DebugInfo = value;
				StatusChanged = true;
			}
		}
	}

	public virtual SystemStatus Status
	{
		get
		{
			return _Status;
		}
	}

	public virtual SystemSecondaryStatus SecondaryStatus
	{
		get
		{
			return _SecondaryStatus;
		}
		protected set
		{
			if (_SecondaryStatus != value)
			{
				_SecondaryStatus = value;
				StatusChanged = true;
			}
		}
	}

	public float OperationRate
	{
		get
		{
			return MathHelper.Clamp(_OperationRate, 0f, 1f);
		}
		set
		{
			if (_OperationRate == value)
			{
				return;
			}
			_OperationRate = value;
			if (Status == SystemStatus.OnLine)
			{
				if (OperationRate > float.Epsilon)
				{
					_SecondaryStatus = SystemSecondaryStatus.None;
				}
				else
				{
					_SecondaryStatus = SystemSecondaryStatus.Idle;
				}
			}
			StatusChanged = true;
		}
	}

	public Dictionary<DistributionSystemType, SortedSet<IResourceProvider>> ConnectedProviders => _ConnectedProviders;

	public virtual Dictionary<DistributionSystemType, ResourceRequirement> ResourceRequirements
	{
		get
		{
			return _ResourcesConsumption;
		}
		set
		{
			_ResourcesConsumption = value;
			IsPowerConsumer = _ResourcesConsumption.ContainsKey(DistributionSystemType.Power);
		}
	}

	public Dictionary<DistributionSystemType, HashSet<ResourceContainer>> ResourceContainers => _ResourceContainers;

	public float GetScopeMultiplier(MachineryPartSlotScope scope)
	{
		int slots;
		int parts;
		int workingParts;
		return GetScopeMultiplier(scope, out slots, out parts, out workingParts);
	}

	public float GetScopeMultiplier(MachineryPartSlotScope scope, out int slots, out int parts, out int workingParts)
	{
		float value = 1f;
		parts = 0;
		workingParts = 0;
		slots = 0;
		foreach (KeyValuePair<VesselObjectID, MachineryPartSlotData> kv in MachineryPartSlots.Where((KeyValuePair<VesselObjectID, MachineryPartSlotData> m) => m.Value.Scope == scope))
		{
			slots++;
			MachineryPart mp = MachineryParts[kv.Key];
			if (mp != null)
			{
				if (mp.Health > float.Epsilon)
				{
					value = !(mp.TierMultiplier > 1f) ? value - (1f - mp.TierMultiplier) : value + (mp.TierMultiplier - 1f);
					workingParts++;
				}
				parts++;
			}
		}
		if (value < float.Epsilon)
		{
			return 0f;
		}
		return value;
	}

	public VesselComponent(SpaceObjectVessel vessel, VesselObjectID id)
	{
		ID = id;
		ParentVessel = vessel;
	}

	public virtual void FitPartToSlot(VesselObjectID slotKey, MachineryPart part)
	{
		MachineryParts[slotKey] = part;
	}

	public virtual void RemovePartFromSlot(VesselObjectID slotKey)
	{
		MachineryParts[slotKey] = null;
	}

	public virtual void InitMachineryPartSlot(VesselObjectID slotKey, MachineryPart part, MachineryPartSlotData partSlotData)
	{
		FitPartToSlot(slotKey, part);
		MachineryPartSlots[slotKey] = ObjectCopier.DeepCopy(partSlotData);
	}

	public void SetMachineryPartSlotActive(VesselObjectID slotKey, bool state)
	{
		MachineryPartSlots[slotKey].IsActive = state;
	}

	public virtual async Task GoOffLine(bool autoRestart, bool malfunction = false)
	{
		shouldAutoReactivate = autoRestart && AutoReactivate;
		if (Status is SystemStatus.OnLine or SystemStatus.PowerUp)
		{
			if (CoolDownTime > 0f)
			{
				statusChangeCountdown = CoolDownTime;
				await SetStatusAsync(SystemStatus.CoolDown);
			}
			else
			{
				await SetStatusAsync(SystemStatus.OffLine);
			}
		}
		if (Defective)
		{
			SecondaryStatus = SystemSecondaryStatus.Defective;
			return;
		}
		if (shouldAutoReactivate && malfunction)
		{
			SecondaryStatus = SystemSecondaryStatus.Malfunction;
			return;
		}
		SecondaryStatus = SystemSecondaryStatus.None;
		DebugInfo = "";
	}

	public virtual async Task GoOnLine()
	{
		if (Status != SystemStatus.OffLine)
		{
			return;
		}
		Dictionary<IResourceProvider, float> reservedCapacities = new Dictionary<IResourceProvider, float>();
		Dictionary<ResourceContainer, float> reservedQuantities = new Dictionary<ResourceContainer, float>();
		string debugText = null;
		if (CheckAvailableResources(OperationRate, 1f, this is Generator, ref reservedCapacities, ref reservedQuantities, ref debugText))
		{
			if (PowerUpTime > 0f)
			{
				statusChangeCountdown = PowerUpTime;
				await SetStatusAsync(SystemStatus.PowerUp);
			}
			else
			{
				await SetStatusAsync(SystemStatus.OnLine);
			}
		}
		else
		{
			shouldAutoReactivate = AutoReactivate;
			if (Defective)
			{
				SecondaryStatus = SystemSecondaryStatus.Defective;
			}
			else if (shouldAutoReactivate)
			{
				SecondaryStatus = SystemSecondaryStatus.Malfunction;
			}
		}
		DebugInfo = debugText;
	}

	public bool CanWork(float operationRate, float duration, bool standby)
	{
		Dictionary<IResourceProvider, float> reservedCapacities = new Dictionary<IResourceProvider, float>();
		Dictionary<ResourceContainer, float> reservedQuantities = new Dictionary<ResourceContainer, float>();
		string debugText = null;
		return CheckAvailableResources(operationRate, duration, standby, ref reservedCapacities, ref reservedQuantities, ref debugText);
	}

	public virtual async Task Update(double duration)
	{
		statusChangeCountdown -= (float)duration;
		if (statusChangeCountdown <= 0f)
		{
			statusChangeCountdown = 0f;
			if (Status == SystemStatus.PowerUp)
			{
				await SetStatusAsync(SystemStatus.OnLine);
			}
			else if (Status == SystemStatus.CoolDown)
			{
				await SetStatusAsync(SystemStatus.OffLine);
			}
		}
		if (Status == SystemStatus.OnLine)
		{
			foreach (KeyValuePair<VesselObjectID, MachineryPartSlotData> kv in MachineryPartSlots)
			{
				float decay = kv.Value.PartDecay;
				MachineryPart mp = null;
				MachineryParts.TryGetValue(kv.Key, out mp);
				if (kv.Value.IsActive && mp is { Health: > 0f })
				{
					mp.Health = MathHelper.Clamp((float)(mp.Health - PartsWearFactor * mp.WearMultiplier * decay / 3600f * duration * OperationRate), 0f, mp.MaxHealth);
				}
			}
		}
		else if (Status == SystemStatus.OffLine && shouldAutoReactivate)
		{
			await GoOnLine();
		}
		InputFactor = GetScopeMultiplier(MachineryPartSlotScope.ResourcesConsumption);
		PowerInputFactor = GetScopeMultiplier(MachineryPartSlotScope.PowerConsumption);
		powerUpFactor = GetScopeMultiplier(MachineryPartSlotScope.PowerUpTime);
		cooldownFactor = GetScopeMultiplier(MachineryPartSlotScope.CoolDownTime);
	}

	public virtual bool CheckAvailableResources(float consumptionFactor, float duration, bool standby, ref Dictionary<IResourceProvider, float> reservedCapacities, ref Dictionary<ResourceContainer, float> reservedQuantities, ref string debugText)
	{
		if (Defective)
		{
			string[] name = GetType().ToString().Split('.');
			debugText = debugText + name[name.Length - 1] + ": MALFUNCTION\n";
			return false;
		}
		if (this is not VesselBaseSystem && IsPowerConsumer && this is not GeneratorCapacitor && ParentVessel.VesselBaseSystem.Status != SystemStatus.OnLine)
		{
			debugText += "Vessel power turned off\n";
			return false;
		}
		if (FixedConsumption && consumptionFactor > float.Epsilon)
		{
			consumptionFactor = 1f;
		}
		bool hasEnoughResourceCapacity = true;
		Dictionary<IResourceProvider, float> reservedCapacitiesBackup1 = new Dictionary<IResourceProvider, float>(reservedCapacities);
		Dictionary<ResourceContainer, float> reservedQuantitiesBackup1 = new Dictionary<ResourceContainer, float>(reservedQuantities);
		foreach (DistributionSystemType resourceType in ResourceRequirements.Keys)
		{
			ResourceRequirement req = ResourceRequirements[resourceType];
			float resourceCapacityNeeded = !standby ? req.Nominal * consumptionFactor * (req.ResourceType == DistributionSystemType.Power ? _PowerInputFactor : _InputFactor) : req.Standby * (req.ResourceType == DistributionSystemType.Power ? _PowerInputFactor : _InputFactor);
			if (ConnectedProviders.ContainsKey(resourceType) && resourceCapacityNeeded > 0f)
			{
				foreach (IResourceProvider rp in ConnectedProviders[resourceType])
				{
					bool canProvide = true;
					if (rp is Generator gen)
					{
						if ((gen.Status == SystemStatus.OnLine || gen is GeneratorCapacitor) && gen.MaxOutput > float.Epsilon)
						{
							Dictionary<IResourceProvider, float> reservedCapacitiesBackup2 = new Dictionary<IResourceProvider, float>(reservedCapacities);
							Dictionary<ResourceContainer, float> reservedQuantitiesBackup2 = new Dictionary<ResourceContainer, float>(reservedQuantities);
							if (gen.FixedConsumption && reservedCapacities.ContainsKey(gen))
							{
								canProvide = true;
							}
							else if (gen is GeneratorCapacitor cap)
							{
								reservedCapacities.TryGetValue(cap, out var alreadyReserved);
								canProvide = cap.Capacity >= alreadyReserved + resourceCapacityNeeded * duration;
							}
							else
							{
								canProvide = gen.CheckAvailableResources(gen.MaxOutput > 0f ? MathHelper.Clamp(resourceCapacityNeeded / gen.MaxOutput, 0f, 1f) : 1f, duration, standby, ref reservedCapacitiesBackup2, ref reservedQuantitiesBackup2, ref debugText);
							}
							if (canProvide)
							{
								reservedCapacities = reservedCapacitiesBackup2;
								reservedQuantities = reservedQuantitiesBackup2;
							}
						}
						else
						{
							canProvide = false;
						}
					}
					if (!canProvide)
					{
						continue;
					}
					float alreadyReservedCap = 0f;
					if (reservedCapacities != null && reservedCapacities.TryGetValue(rp, out var capacity))
					{
						alreadyReservedCap = capacity;
					}
					float resourceCapacityLeftInProvider = rp.MaxOutput - alreadyReservedCap;
					float alreadyReservedQty = 0f;
					if (rp is ResourceContainer container)
					{
						reservedQuantities.TryGetValue(container, out alreadyReservedQty);
						float available = container.GetCompartment().Resources[0].Quantity - alreadyReservedQty;
						if (resourceCapacityLeftInProvider * duration > available)
						{
							resourceCapacityLeftInProvider = available / duration;
						}
					}
					if (resourceCapacityLeftInProvider >= resourceCapacityNeeded)
					{
						if (rp is ResourceContainer resourceContainer)
						{
							reservedQuantities[resourceContainer] = resourceCapacityNeeded * duration + alreadyReservedQty;
						}
						reservedCapacities[rp] = alreadyReservedCap + resourceCapacityNeeded;
						resourceCapacityNeeded = 0f;
						break;
					}
					if (rp is ResourceContainer key && reservedCapacities.TryGetValue(key, out var reservedCapacity))
					{
						reservedQuantities[key] = (rp.MaxOutput - reservedCapacity) * duration + alreadyReservedQty;
					}
					reservedCapacities[rp] = rp.MaxOutput;
					resourceCapacityNeeded -= resourceCapacityLeftInProvider;
				}
			}
			if (resourceCapacityNeeded > 0f)
			{
				hasEnoughResourceCapacity = false;
				if (debugText.IsNullOrEmpty())
				{
					debugText = "";
				}
				string[] name2 = GetType().ToString().Split('.');
				debugText = debugText + name2[name2.Length - 1] + ": " + resourceCapacityNeeded + " of " + resourceType.ToString() + " short\n";
			}
		}
		if (this is GeneratorCapacitor)
		{
			return true;
		}
		if (!hasEnoughResourceCapacity)
		{
			reservedCapacities = reservedCapacitiesBackup1;
			reservedQuantities = reservedQuantitiesBackup1;
		}
		return hasEnoughResourceCapacity;
	}

	public async Task<ComsumerRoomReserved> CheckStatus(float operationRate, float duration, bool standby, Dictionary<IResourceProvider, float> reservedCapacities, Dictionary<ResourceContainer, float> reservedQuantities)
	{
		if (Status != SystemStatus.OnLine && (Status != SystemStatus.OffLine || !CanReactivate))
		{
			return new ComsumerRoomReserved
			{
				Capacities = reservedCapacities,
				Quantities = reservedQuantities
			};
		}
		Dictionary<IResourceProvider, float> reservedCapacitiesBackup = new(reservedCapacities);
		Dictionary<ResourceContainer, float> reservedQuantitiesBackup = new(reservedQuantities);
		string debugText = null;
		bool canWork = CheckAvailableResources(operationRate, duration, standby, ref reservedCapacities, ref reservedQuantities, ref debugText);
		debugText = "Vessel " + ParentVessel.Guid + "\n" + debugText;
		if (canWork && Status == SystemStatus.OffLine && CanReactivate)
		{
			await GoOnLine();
		}
		else if (!canWork)
		{
			if (Status == SystemStatus.OnLine)
			{
				DebugInfo = debugText;
				await GoOffLine(autoRestart: true, malfunction: true);
			}
			reservedCapacities = reservedCapacitiesBackup;
			reservedQuantities = reservedQuantitiesBackup;
		}

		return new ComsumerRoomReserved
		{
			Capacities = reservedCapacities,
			Quantities = reservedQuantities
		};
	}

	public virtual async Task SetDefectiveAsync(bool value)
	{
		if (_Defective != (_Defective = value))
		{
			if (value && Status is SystemStatus.OnLine or SystemStatus.PowerUp)
			{
				await GoOffLine(autoRestart: true);
			}
			else if (value)
			{
				SecondaryStatus = SystemSecondaryStatus.Defective;
			}
			else if (Status is SystemStatus.OffLine or SystemStatus.CoolDown && shouldAutoReactivate)
			{
				await GoOnLine();
			}
			else
			{
				SecondaryStatus = SystemSecondaryStatus.None;
			}
		}
	}


	protected virtual Task SetStatusAsync(SystemStatus status)
	{
		if (_Status == status)
		{
			return Task.CompletedTask;
		}
		_Status = status;
		if (Status == SystemStatus.OnLine)
		{
			if (OperationRate > float.Epsilon)
			{
				_SecondaryStatus = SystemSecondaryStatus.None;
			}
			else
			{
				_SecondaryStatus = SystemSecondaryStatus.Idle;
			}
			ParentVessel.RepairPoints.FirstOrDefault((VesselRepairPoint m) => m.AffectedSystem == this)?.Update();
			if (baseRadarSignature != 0f)
			{
				ParentVessel.MainVessel.UpdateVesselData();
			}
		}
		else if (Status == SystemStatus.OffLine && baseRadarSignature != 0f)
		{
			ParentVessel.MainVessel.UpdateVesselData();
		}
		StatusChanged = true;

		return Task.CompletedTask;
	}

	protected virtual void SetStatus(SystemStatus status)
	{
		if (_Status == status)
		{
			return;
		}
		_Status = status;
		if (Status == SystemStatus.OnLine)
		{
			if (OperationRate > float.Epsilon)
			{
				_SecondaryStatus = SystemSecondaryStatus.None;
			}
			else
			{
				_SecondaryStatus = SystemSecondaryStatus.Idle;
			}
			ParentVessel.RepairPoints.FirstOrDefault((VesselRepairPoint m) => m.AffectedSystem == this)?.Update();
			if (baseRadarSignature != 0f)
			{
				ParentVessel.MainVessel.UpdateVesselData();
			}
		}
		else if (Status == SystemStatus.OffLine && baseRadarSignature != 0f)
		{
			ParentVessel.MainVessel.UpdateVesselData();
		}
		StatusChanged = true;
	}

	public PersistenceObjectData GetPersistenceData()
	{
		return new PersistenceObjectDataVesselComponent
		{
			GUID = ParentVessel.Guid,
			InSceneID = ID.InSceneID,
			Status = Status,
			StatusChangeCountdown = statusChangeCountdown,
			ShouldAutoReactivate = shouldAutoReactivate,
			AutoReactivate = AutoReactivate,
			Defective = Defective,
			AuxData = GetPersistenceAuxData()
		};
	}

	public virtual PersistenceData GetPersistenceAuxData()
	{
		return null;
	}

	public async Task LoadPersistenceData(PersistenceObjectData persistenceData)
	{
		if (persistenceData is not PersistenceObjectDataVesselComponent data)
		{
			Debug.LogWarning("PersistenceObjectDataVesselComponent data is null");
			return;
		}
		_AutoReactivate = data.AutoReactivate;
		shouldAutoReactivate = data.ShouldAutoReactivate;
		statusChangeCountdown = data.StatusChangeCountdown;
		_Status = data.Status;
		await SetDefectiveAsync(data.Defective);
		SetPersistenceAuxData(data.AuxData);
	}

	public virtual void SetPersistenceAuxData(PersistenceData auxData)
	{
	}
}
