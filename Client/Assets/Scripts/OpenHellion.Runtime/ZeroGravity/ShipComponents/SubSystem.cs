using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using ZeroGravity.Data;
using ZeroGravity.LevelDesign;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity.ShipComponents
{
	public abstract class SubSystem : VesselSystem
	{
		[Space(5f)] public float OperationRate = 1f;

		public bool AutoTuneOperationRate;

		[Space(5f)] [SerializeField] [FormerlySerializedAs("triggerAnimtion")]
		protected SceneTriggerAnimation triggerAnimation;

		[Space(5f)] public SystemSpawnSettings[] SpawnSettings = new SystemSpawnSettings[0];

		[Space(5f)] [Multiline] public string DebugInfo;

		[NonSerialized] public float InputFactorStandby = 1f;

		public abstract SubSystemType Type { get; }

		public abstract ResourceRequirement[] ResourceRequirements { get; }

		public override VesselComponentType ComponentType => VesselComponentType.SubSystem;

		public float GetPowerConsumption(bool? working = null)
		{
			return GetResourceRequirement(DistributionSystemType.Power, working);
		}

		public float GetResourceConsumption(DistributionSystemType type, bool? working = null)
		{
			return GetResourceRequirement(type, working);
		}

		public float GetResourceRequirement(DistributionSystemType resourceType, bool? working = null)
		{
			float num;
			if (!working.HasValue)
			{
				if (Status != SystemStatus.Online)
				{
					return 0f;
				}

				num = ResourceRequirements.Where((ResourceRequirement m) => m.ResourceType == resourceType).Sum(
					(ResourceRequirement m) => (SecondaryStatus != SystemSecondaryStatus.Idle) ? m.Nominal : m.Standby);
			}
			else
			{
				num = ResourceRequirements.Where((ResourceRequirement m) => m.ResourceType == resourceType)
					.Sum((ResourceRequirement m) => (!working.Value) ? m.Standby : m.Nominal);
			}

			if (resourceType == DistributionSystemType.Power)
			{
				return num * PowerInputFactor;
			}

			return num * InputFactor;
		}

		protected virtual void Awake()
		{
			if (triggerAnimation == null)
			{
				triggerAnimation = GetComponent<SceneTriggerAnimation>();
			}
		}

		protected SubSystemDetails GetDetails(bool? isSwitchedOn = null)
		{
			SubSystemDetails subSystemDetails = new SubSystemDetails
			{
				InSceneID = InSceneID,
				Status = ((!isSwitchedOn.HasValue)
					? Status
					: ((!isSwitchedOn.Value) ? SystemStatus.Offline : SystemStatus.Online)),
				AuxDetails = GetAuxDetails()
			};
			return subSystemDetails;
		}

		public void SendDetails()
		{
			SpaceObjectVessel parentVessel = _ParentVessel;
			SubSystemDetails details = GetDetails();
			parentVessel.ChangeStats(null, null, null, null, details);
		}

		public bool IsEventFinished()
		{
			if (triggerAnimation != null)
			{
				return triggerAnimation.IsEventFinished;
			}

			return true;
		}

		public void SetParentVessel(SpaceObjectVessel vessel)
		{
			_ParentVessel = vessel;
		}

		public virtual void SetDetails(SystemStatus status, SystemSecondaryStatus secondaryStatus, float operationRate,
			bool instant = false)
		{
			Status = status;
			SecondaryStatus = secondaryStatus;
			OperationRate = operationRate;
			if (triggerAnimation != null)
			{
				triggerAnimation.ChangeState(Status == SystemStatus.Online, instant);
			}
		}

		public virtual void SetDetails(SubSystemDetails details, bool instant = false)
		{
			SetDetails(details.Status, details.SecondaryStatus, details.OperationRate, instant);
			SetAuxDetails(details.AuxDetails);
			AutoRestart = details.AutoRestart;
			InputFactor = details.InputFactor;
			PowerInputFactor = details.PowerInputFactor;
			DebugInfo = details.DebugInfo;
		}

		public virtual void SetAuxDetails(IAuxDetails auxDetails)
		{
		}

		public override void SwitchOn()
		{
			if (Status == SystemStatus.Offline || Status == SystemStatus.Cooldown)
			{
				SpaceObjectVessel parentVessel = _ParentVessel;
				SubSystemDetails details = GetDetails(true);
				parentVessel.ChangeStats(null, null, null, null, details);
			}
		}

		public override void SwitchOff()
		{
			if (Status == SystemStatus.Online || Status == SystemStatus.Powerup)
			{
				SpaceObjectVessel parentVessel = _ParentVessel;
				SubSystemDetails details = GetDetails(false);
				parentVessel.ChangeStats(null, null, null, null, details);
			}
		}

		public override void Toggle()
		{
			SpaceObjectVessel parentVessel = _ParentVessel;
			SubSystemDetails details = GetDetails(!IsSwitchedOn());
			parentVessel.ChangeStats(null, null, null, null, details);
		}

		public virtual SubSystemData GetData()
		{
			if (ResourceContainers.Length > 0)
			{
				ResourceContainer[] resourceContainers = ResourceContainers;
				foreach (ResourceContainer resourceContainer in resourceContainers)
				{
					if (resourceContainer == null)
					{
						throw new Exception("Reference(s) missing in Resource Containers list.");
					}
				}
			}

			List<int> list = new List<int>();
			if (MachineryPartSlots != null)
			{
				SceneMachineryPartSlot[] machineryPartSlots = MachineryPartSlots;
				foreach (SceneMachineryPartSlot sceneMachineryPartSlot in machineryPartSlots)
				{
					list.Add(sceneMachineryPartSlot.InSceneID);
				}
			}

			List<int> list2 = new List<int>();
			if (ResourceContainers != null)
			{
				ResourceContainer[] resourceContainers2 = ResourceContainers;
				foreach (ResourceContainer resourceContainer2 in resourceContainers2)
				{
					list2.Add(resourceContainer2.InSceneID);
				}
			}

			SubSystemData subSystemData = new SubSystemData
			{
				InSceneID = InSceneID,
				Type = Type,
				ResourceRequirements = ResourceRequirements,
				SpawnSettings = SpawnSettings,
				Status = Status,
				MachineryPartSlots = list,
				ResourceContainers = list2,
				OperationRate = OperationRate,
				AutoTuneOperationRate = AutoTuneOperationRate,
				AutoReactivate = AutoReactivate,
				CoolDownTime = CoolDownTime,
				PowerUpTime = PowerUpTime,
				AuxData = GetAuxData(),
				RoomID = (!(Room == null)) ? Room.InSceneID : (-1)
			};
			return subSystemData;
		}

		public virtual float GetConsumption(DistributionSystemType resourceType)
		{
			if (Status != SystemStatus.Online)
			{
				return 0f;
			}

			ResourceRequirement[] resourceRequirements = ResourceRequirements;
			foreach (ResourceRequirement resourceRequirement in resourceRequirements)
			{
				if (resourceRequirement.ResourceType == resourceType)
				{
					return resourceRequirement.Nominal * OperationRate * InputFactor +
					       resourceRequirement.Standby * InputFactorStandby;
				}
			}

			return 0f;
		}
	}
}
