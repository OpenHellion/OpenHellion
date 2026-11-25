using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenHellion.State;
using ZeroGravity.Data;
using ZeroGravity.Math;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity.ShipComponents;

public class SubSystemRadar : SubSystem
{
	public float ActiveScanDuration;

	public double PassiveRangePerSquareMetre;

	public double UnidentifiedSphereSize;

	private double stopActiveScanTime;

	public override SubSystemType Type => SubSystemType.Radar;

	public double Amplification => GetScopeMultiplier(MachineryPartSlotScope.RadarSignalAmplifier);

	public double Kilowatts => ResourceRequirements.TryGetValue(DistributionSystemType.Power, out ResourceRequirement power) ? power.Nominal : 0.0;

	public SubSystemRadar(SpaceObjectVessel vessel, VesselObjectID id, SubSystemData ssData)
		: base(vessel, id, ssData)
	{
		_AutoReactivate = false;
	}

	/// <summary>
	/// 	The radar a player aboard <paramref name="parent"/> listens with. Passive listening needs only the vessel's
	/// 	base system to be online; nothing is heard while warping.
	/// </summary>
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
			PassiveRangePerSquareMetre = rad.PassiveRangePerSquareMetre;
			UnidentifiedSphereSize = rad.UnidentifiedSphereSize;
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

	public double GetSensitivityMultiplier()
	{
		Vector3D position = ParentVessel.Position;
		double planetZoneSensitivity = 1.0;
		foreach (CelestialBody body in Server.Instance.SolarSystem.GetCelestialBodies())
		{
			if ((position - body.Position).SqrMagnitude < body.Orbit.GravityInfluenceRadiusSquared)
			{
				planetZoneSensitivity *= body.RadarSensitivityInSphereOfInfluence;
			}
		}
		double debrisFieldSensitivity = 1.0;
		foreach (DebrisField field in Server.Instance.DebrisFields)
		{
			if (field.Contains(position))
			{
				debrisFieldSensitivity *= field.Data.ScanningSensitivityMultiplier;
			}
		}
		return RadarDetection.GetSensitivityMultiplier(Amplification, ParentVessel.MainVessel.ExposureDamage, planetZoneSensitivity, debrisFieldSensitivity);
	}

	public double GetPassiveRange(double signature, double sensitivityMultiplier)
	{
		return RadarDetection.GetPassiveRange(PassiveRangePerSquareMetre, signature, sensitivityMultiplier);
	}

	public bool DetectsPassively(SpaceObjectVessel target, double sensitivityMultiplier)
	{
		double range = GetPassiveRange(GetSignatureAsHeard(target), sensitivityMultiplier);
		return (target.Position - ParentVessel.Position).SqrMagnitude <= range * range && IsInLineOfSight(target.Position);
	}

	/// <summary>
	/// 	Sweeps a cone of full opening <paramref name="angle"/> degrees. Vessels too faint to locate but within fuzzy
	/// 	range go into <paramref name="fuzzy"/>.
	/// </summary>
	public void ActiveScan(Vector3D direction, float angle, double sensitivityMultiplier, List<SpaceObjectVessel> detected, List<SpaceObjectVessel> fuzzy)
	{
		Vector3D origin = ParentVessel.Position;
		foreach (SpaceObjectVessel vessel in Server.Instance.AllVessels)
		{
			if (!vessel.IsMainVessel || vessel.IsDebrisFragment || vessel == ParentVessel.MainVessel)
			{
				continue;
			}
			Vector3D offset = vessel.Position - origin;
			if (Vector3D.Angle(direction, offset) > angle / 2f || !IsInLineOfSight(vessel.Position))
			{
				continue;
			}
			double signature = GetSignatureAsHeard(vessel);
			double distanceSquared = offset.SqrMagnitude;
			double activeRange = RadarDetection.GetActiveRange(Kilowatts, angle, signature, sensitivityMultiplier);
			double fuzzyRange = RadarDetection.GetFuzzyRange(Kilowatts, angle, signature, sensitivityMultiplier);
			if (distanceSquared <= activeRange * activeRange)
			{
				detected.Add(vessel);
			}
			else if (distanceSquared <= fuzzyRange * fuzzyRange)
			{
				fuzzy.Add(vessel);
			}
		}
	}

	private static double GetSignatureAsHeard(SpaceObjectVessel target)
	{
		double signature = target.GetCompoundSignature();
		foreach (DebrisField field in Server.Instance.DebrisFields)
		{
			if (field.Contains(target.Position))
			{
				signature *= field.Data.RadarSignatureMultiplier;
			}
		}
		return signature;
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
}
