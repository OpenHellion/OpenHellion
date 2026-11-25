using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using ZeroGravity.Data;
using ZeroGravity.LevelDesign;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity.ShipComponents
{
	public abstract class Generator : VesselSystem
	{
		[CompilerGenerated]
		private sealed class _003CGetResourceRequirement_003Ec__AnonStorey0
		{
			internal DistributionSystemType resourceType;

			internal bool? working;

			internal Generator _0024this;

			internal bool _003C_003Em__0(ResourceRequirement m)
			{
				return m.ResourceType == resourceType;
			}

			internal float _003C_003Em__1(ResourceRequirement m)
			{
				return (_0024this.SecondaryStatus != SystemSecondaryStatus.Idle) ? m.Nominal : m.Standby;
			}

			internal bool _003C_003Em__2(ResourceRequirement m)
			{
				return m.ResourceType == resourceType;
			}

			internal float _003C_003Em__3(ResourceRequirement m)
			{
				return (!working.Value) ? m.Standby : m.Nominal;
			}
		}

		[Space(5f)] public float OutputRate = 1f;

		[NonSerialized] public float Output;

		[NonSerialized] public float MaxOutput;

		public float NominalOutput;

		[SerializeField] protected SceneTriggerAnimation triggerAnimtion;

		[Space(5f)] [Multiline] public string DebugInfo;

		[NonSerialized] public float InputFactorStandby = 1f;

		public SystemSpawnSettings[] SpawnSettings = new SystemSpawnSettings[0];

		public abstract GeneratorType Type { get; }

		public abstract ResourceRequirement[] ResourceRequirements { get; }

		public override VesselComponentType ComponentType
		{
			get { return VesselComponentType.Generator; }
		}

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
			_003CGetResourceRequirement_003Ec__AnonStorey0 _003CGetResourceRequirement_003Ec__AnonStorey =
				new _003CGetResourceRequirement_003Ec__AnonStorey0();
			_003CGetResourceRequirement_003Ec__AnonStorey.resourceType = resourceType;
			_003CGetResourceRequirement_003Ec__AnonStorey.working = working;
			_003CGetResourceRequirement_003Ec__AnonStorey._0024this = this;
			float num;
			if (!_003CGetResourceRequirement_003Ec__AnonStorey.working.HasValue)
			{
				if (Status != SystemStatus.Online)
				{
					return 0f;
				}

				num = ResourceRequirements.Where(_003CGetResourceRequirement_003Ec__AnonStorey._003C_003Em__0)
					.Sum(_003CGetResourceRequirement_003Ec__AnonStorey
						._003C_003Em__1);
			}
			else
			{
				num = ResourceRequirements.Where(_003CGetResourceRequirement_003Ec__AnonStorey._003C_003Em__2)
					.Sum(_003CGetResourceRequirement_003Ec__AnonStorey
						._003C_003Em__3);
			}

			if (_003CGetResourceRequirement_003Ec__AnonStorey.resourceType == DistributionSystemType.Power)
			{
				return num * PowerInputFactor;
			}

			return num * InputFactor;
		}

		private void Awake()
		{
			if (triggerAnimtion == null)
			{
				triggerAnimtion = GetComponent<SceneTriggerAnimation>();
			}
		}

		protected GeneratorDetails getDetails(bool? isSwitchedOn = null)
		{
			GeneratorDetails generatorDetails = new GeneratorDetails();
			generatorDetails.InSceneID = InSceneID;
			generatorDetails.Status = ((!isSwitchedOn.HasValue)
				? Status
				: ((!isSwitchedOn.Value) ? SystemStatus.Offline : SystemStatus.Online));
			generatorDetails.OutputRate = OutputRate;
			return generatorDetails;
		}

		public virtual bool IsEventFinished()
		{
			if (triggerAnimtion != null)
			{
				return triggerAnimtion.IsEventFinished;
			}

			return true;
		}

		public void SetParentVessel(SpaceObjectVessel vessel)
		{
			_ParentVessel = vessel;
		}

		private void SetDetails(SystemStatus status, SystemSecondaryStatus secondaryStatus, bool instant = false)
		{
			Status = status;
			SecondaryStatus = secondaryStatus;
			if (triggerAnimtion != null)
			{
				triggerAnimtion.ChangeState(Status == SystemStatus.Online, instant);
			}
		}

		public virtual void SetDetails(GeneratorDetails details, bool instant = false)
		{
			Status = details.Status;
			SecondaryStatus = details.SecondaryStatus;
			Output = details.Output;
			MaxOutput = details.MaxOutput;
			OutputRate = details.OutputRate;
			InputFactor = details.InputFactor;
			PowerInputFactor = details.PowerInputFactor;
			AutoRestart = details.AutoRestart;
			if (details.AuxDetails != null)
			{
				SetAuxDetails(details.AuxDetails);
			}

			DebugInfo = details.DebugInfo;
			if (triggerAnimtion != null)
			{
				triggerAnimtion.ChangeState(Status == SystemStatus.Online, instant);
			}
		}

		public virtual void SetAuxDetails(IAuxDetails auxDetails)
		{
		}

		public override void SwitchOn()
		{
			SpaceObjectVessel parentVessel = _ParentVessel;
			GeneratorDetails details = getDetails(true);
			parentVessel.ChangeStats(null, null, null, null, null, details);
		}

		public override void SwitchOff()
		{
			SpaceObjectVessel parentVessel = _ParentVessel;
			GeneratorDetails details = getDetails(false);
			parentVessel.ChangeStats(null, null, null, null, null, details);
		}

		public override void Toggle()
		{
			SpaceObjectVessel parentVessel = _ParentVessel;
			GeneratorDetails details = getDetails(!IsSwitchedOn());
			parentVessel.ChangeStats(null, null, null, null, null, details);
		}

		public virtual GeneratorData GetData()
		{
			if (ResourceContainers.Length == 0)
			{
				if (GetComponentInChildren<ResourceContainer>() != null)
				{
					throw new Exception("Loose Resource Containers found. Check Resource Containers list.");
				}
			}
			else
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
					if (sceneMachineryPartSlot == null)
					{
						throw new Exception("Machinery part slot not set");
					}

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

			GeneratorData generatorData = new GeneratorData();
			generatorData.InSceneID = InSceneID;
			generatorData.Type = Type;
			generatorData.PowerUpTime = PowerUpTime;
			generatorData.CoolDownTime = CoolDownTime;
			generatorData.AutoReactivate = AutoReactivate;
			generatorData.Output = Output;
			generatorData.NominalOutput = NominalOutput;
			generatorData.Status = Status;
			generatorData.ResourceRequirements = ResourceRequirements;
			generatorData.SpawnSettings = SpawnSettings;
			generatorData.MachineryPartSlots = list;
			generatorData.ResourceContainers = list2;
			generatorData.OutputRate = OutputRate;
			generatorData.RoomID = ((!(Room == null)) ? Room.InSceneID : (-1));
			generatorData.AuxData = GetAuxData();
			return generatorData;
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
					return resourceRequirement.Nominal * OutputRate * InputFactor +
					       resourceRequirement.Standby * InputFactorStandby;
				}
			}

			return 0f;
		}
	}
}
