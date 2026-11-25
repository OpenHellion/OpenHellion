using OpenHellion;
using OpenHellion.State;
using UnityEngine;
using ZeroGravity.Data;
using ZeroGravity.LevelDesign;
using ZeroGravity.Math;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity.ShipComponents
{
	public class SubSystemRadar : SubSystem
	{
		public const double ScanningConeReferenceSignature = 110.0;

		[SerializeField] private ResourceRequirement[] _ResourceRequirements = new ResourceRequirement[1]
		{
			new ResourceRequirement
			{
				ResourceType = DistributionSystemType.Power
			}
		};

		public float ActiveScanDuration = 3f;

		[Tooltip("Metres of passive range per square metre of signature")]
		public double PassiveRangePerSquareMetre = 4140.0;

		[System.NonSerialized] public bool ActiveScanPending;

		private MachineryPart SignalAmplifier;

		public override SubSystemType Type => SubSystemType.Radar;

		public override ResourceRequirement[] ResourceRequirements => _ResourceRequirements;

		public float SignalAmplification
		{
			get
			{
				if (SignalAmplifier == null || SignalAmplifier.Health <= float.Epsilon)
				{
					return 1f;
				}

				return SignalAmplifier.TierMultiplier;
			}
		}

		public double Kilowatts => _ResourceRequirements[0].Nominal;

		public double GetSensitivityMultiplier()
		{
			Vector3D position = World.LocalToWorldPosition(ParentVessel.transform.position);
			double planetZoneSensitivity = 1.0;
			foreach (CelestialBody body in World.SolarSystem.GetCelestialBodies())
			{
				if ((position - body.Position).SqrMagnitude < body.Orbit.GravityInfluenceRadiusSquared)
				{
					planetZoneSensitivity *= body.RadarSensitivityInSphereOfInfluence;
				}
			}

			double debrisFieldSensitivity = 1.0;
			foreach (DebrisField debrisField in World.DebrisFields)
			{
				if (debrisField.CheckObject(ParentVessel))
				{
					debrisFieldSensitivity *= debrisField.ScanningSensitivityMultiplier;
				}
			}

			return RadarDetection.GetSensitivityMultiplier(SignalAmplification, World.GetVesselExposureDamage(position.Magnitude),
				planetZoneSensitivity, debrisFieldSensitivity);
		}

		public override SystemAuxData GetAuxData()
		{
			return new RadarAuxData
			{
				ActiveScanDuration = ActiveScanDuration,
				PassiveRangePerSquareMetre = PassiveRangePerSquareMetre
			};
		}

		public override void SetDetails(SubSystemDetails details, bool instant = false)
		{
			if (Status != details.Status && ActiveScanPending)
			{
				if (details.Status == SystemStatus.Online)
				{
					World.Map.ShowScanningEffect();
				}
				else
				{
					ActiveScanPending = false;
					World.Map.HideScanningEffect();
				}
			}

			base.SetDetails(details, instant);
		}

		public override void MachineryPartAttached(SceneMachineryPartSlot slot)
		{
			base.MachineryPartAttached(slot);
			if (slot.Scope == MachineryPartSlotScope.RadarSignalAmplifier)
			{
				SignalAmplifier = slot.Item as MachineryPart;
			}
		}

		public override void MachineryPartDetached(SceneMachineryPartSlot slot)
		{
			base.MachineryPartDetached(slot);
			if (slot.Scope == MachineryPartSlotScope.RadarSignalAmplifier)
			{
				SignalAmplifier = null;
			}
		}
	}
}
