using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZeroGravity.Data;
using ZeroGravity.Math;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity.ShipComponents;

public class SubSystemRadar : SubSystem
{
	public const double TransmitterEfficiency = 0.3;

	public const double ActiveThreshold = 1.717e-18;

	public const double FuzzyThreshold = ActiveThreshold / 81.0;

	public float ActiveScanDuration;

	public double PassiveThreshold;

	private double stopActiveScanTime;

	public override SubSystemType Type => SubSystemType.Radar;

	public double Amplification => GetScopeMultiplier(MachineryPartSlotScope.RadarSignalAmplifier);

	public double TransmitPower => ResourceRequirements.TryGetValue(DistributionSystemType.Power, out ResourceRequirement power)
		? power.Nominal * 1000.0 * TransmitterEfficiency * Amplification
		: 0.0;

	public SubSystemRadar(SpaceObjectVessel vessel, VesselObjectID id, SubSystemData ssData)
		: base(vessel, id, ssData)
	{
		_AutoReactivate = false;
	}

	public static SubSystemRadar FindUsable(SpaceObject parent)
	{
		SpaceObjectVessel vessel = (parent as SpaceObjectVessel)?.MainVessel;
		return vessel is { IsWarping: false }
			? vessel.MainDistributionManager?.GetSubSystems().OfType<SubSystemRadar>()
				.FirstOrDefault((SubSystemRadar m) => m.ParentVessel.VesselBaseSystem?.Status == SystemStatus.OnLine)
			: null;
	}

	public override void SetAuxData(SystemAuxData auxData)
	{
		if (auxData is RadarAuxData rad)
		{
			ActiveScanDuration = rad.ActiveScanDuration;
			PassiveThreshold = rad.PassiveThreshold;
		}
	}

	protected override async Task SetStatusAsync(SystemStatus status)
	{
		if (base.Status != SystemStatus.OnLine && status == SystemStatus.OnLine)
		{
			stopActiveScanTime = Server.SolarSystemTime + ActiveScanDuration;
		}
		await base.SetStatusAsync(status);
	}

	public override async Task Update(double duration)
	{
		await base.Update(duration);
		PowerInputFactor *= (float)Amplification;
		if (stopActiveScanTime > 0.0 && stopActiveScanTime <= Server.SolarSystemTime && Status == SystemStatus.OnLine)
		{
			await GoOffLine(autoRestart: false);
		}
	}

	public double GetNoise()
	{
		Vector3D position = ParentVessel.Position;
		double noise = 1.0;
		foreach (CelestialBody body in Server.Instance.SolarSystem.GetCelestialBodies())
		{
			noise += body.NoiseRadius * body.NoiseRadius / (position - body.Position).SqrMagnitude;
		}
		foreach (DebrisField field in Server.Instance.DebrisFields)
		{
			if (field.Contains(position))
			{
				noise /= field.Data.ScanningSensitivityMultiplier;
			}
		}
		return noise;
	}

	public bool DetectsPassively(SpaceObjectVessel target, double noise)
	{
		double distanceSquared = (target.Position - ParentVessel.Position).SqrMagnitude;
		return target.GetCompoundEmission() * GetDebrisMultiplier(target.Position) * Amplification >= 4.0 * System.Math.PI * distanceSquared * PassiveThreshold * noise
			&& IsInLineOfSight(target.Position);
	}

	/// <summary>
	/// 	Sweeps a cone of full opening <paramref name="angle"/> degrees. The radar's power is concentrated into the
	/// 	cone's solid angle on the way out and gathered from it on the way back, so a narrower cone reaches further.
	/// 	Echoes too weak to place but above the fuzzy threshold go into <paramref name="fuzzy"/>.
	/// </summary>
	public List<long> ActiveScan(Vector3D direction, float angle, List<long> fuzzy)
	{
		double noise = GetNoise();
		double coneSolidAngle = 2.0 * System.Math.PI * (1.0 - System.Math.Cos(angle * (System.Math.PI / 180.0) / 2.0));
		double strength = TransmitPower / (coneSolidAngle * coneSolidAngle);
		List<long> detected = [];
		foreach (SpaceObjectVessel vessel in Server.Instance.AllVessels)
		{
			if (!vessel.IsMainVessel || vessel == ParentVessel.MainVessel)
			{
				continue;
			}
			Vector3D offset = vessel.Position - ParentVessel.Position;
			if (Vector3D.Angle(direction, offset) > angle / 2f || !IsInLineOfSight(vessel.Position))
			{
				continue;
			}
			double distanceSquared = offset.SqrMagnitude;
			double received = strength * vessel.GetCompoundCrossSection() * GetDebrisMultiplier(vessel.Position) / (distanceSquared * distanceSquared);
			if (received >= ActiveThreshold * noise)
			{
				detected.Add(vessel.Guid);
			}
			else if (received >= FuzzyThreshold * noise)
			{
				fuzzy.Add(vessel.Guid);
			}
		}
		return detected;
	}

	private bool IsInLineOfSight(Vector3D target)
	{
		Vector3D origin = ParentVessel.Position;
		Vector3D segment = target - origin;
		double lengthSquared = segment.SqrMagnitude;
		foreach (CelestialBody body in Server.Instance.SolarSystem.GetCelestialBodies())
		{
			double t = MathHelper.Clamp(Vector3D.Dot(body.Position - origin, segment) / lengthSquared, 0.0, 1.0);
			if ((origin + segment * t - body.Position).SqrMagnitude < body.Radius * body.Radius)
			{
				return false;
			}
		}
		return true;
	}

	private static double GetDebrisMultiplier(Vector3D position)
	{
		double multiplier = 1.0;
		foreach (DebrisField field in Server.Instance.DebrisFields)
		{
			if (field.Contains(position))
			{
				multiplier *= field.Data.RadarSignatureMultiplier;
			}
		}
		return multiplier;
	}
}
