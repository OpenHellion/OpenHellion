using OpenHellion;
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
		public const double TransmitterEfficiency = 0.3;

		public const double ActiveThreshold = 1.717e-18;

		public const double FuzzyThreshold = ActiveThreshold / 81.0;

		public const double ReferenceEmission = 225e3;

		public const double ReferenceCrossSection = 340.0;

		public const double WarpingReferenceEmission = 20225e3;

		[SerializeField] private ResourceRequirement[] _ResourceRequirements = new ResourceRequirement[1]
		{
			new ResourceRequirement
			{
				ResourceType = DistributionSystemType.Power
			}
		};

		public float ActiveScanDuration = 3f;

		[Tooltip("Weakest emission flux the passive sensor picks up, in W/m2")]
		public double PassiveThreshold = 4.476e-9;

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

		public double TransmitPower =>
			_ResourceRequirements[0].Nominal * 1000.0 * TransmitterEfficiency * SignalAmplification;

		public double GetNoise()
		{
			Vector3D position = World.LocalToWorldPosition(ParentVessel.transform.position);
			double noise = 1.0;
			foreach (CelestialBody body in World.SolarSystem.GetCelestialBodies())
			{
				noise += body.NoiseRadius * body.NoiseRadius / (position - body.Position).SqrMagnitude;
			}

			foreach (DebrisField debrisField in World.DebrisFields)
			{
				if (debrisField.CheckObject(ParentVessel))
				{
					noise /= debrisField.ScanningSensitivityMultiplier;
				}
			}

			return noise;
		}

		public double GetPassiveRange(double emission, double noise)
		{
			return System.Math.Sqrt(emission * SignalAmplification / (4.0 * System.Math.PI * PassiveThreshold * noise));
		}

		public double GetActiveRange(double crossSection, float coneAngle, double threshold, double noise)
		{
			double coneSolidAngle = 2.0 * System.Math.PI * (1.0 - System.Math.Cos(coneAngle * (System.Math.PI / 180.0) / 2.0));
			return System.Math.Pow(TransmitPower * crossSection / (coneSolidAngle * coneSolidAngle * threshold * noise), 0.25);
		}

		public override SystemAuxData GetAuxData()
		{
			return new RadarAuxData
			{
				ActiveScanDuration = ActiveScanDuration,
				PassiveThreshold = PassiveThreshold
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
