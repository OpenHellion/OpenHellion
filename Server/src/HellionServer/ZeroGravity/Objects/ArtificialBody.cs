using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZeroGravity.Math;
using ZeroGravity.Network;
using ZeroGravity.ShipComponents;

namespace ZeroGravity.Objects;

public class ArtificialBody : SpaceObject
{
	private double _prevUpdatePositionTime = Server.SolarSystemTime;

	public OrbitParameters Orbit = new OrbitParameters();

	private ManeuverCourse _currentCourse;

	public double LastOrientationChangeTime;

	private QuaternionD _rotation = QuaternionD.Identity;

	public double Radius;

	/// <summary>
	/// 	True angular velocity in the body's own frame, in degrees per second.
	/// </summary>
	public Vector3D AngularVelocity;

	protected Vector3D PhysicsVelocityDifference;

	protected Vector3D PhysicsRotationDifference;

	private bool _markForDestruction;

	private const double StabilizeToTargetMaxVelocityDiff = 2.0;

	private const double StabilizeToTargetMaxPositionDiff = 100.0;

	public readonly List<SpaceObjectVessel> StabilizedToTargetChildren = new List<SpaceObjectVessel>();

	private double? _stabilizationDisabledTime;

	private bool? _stabilizationDisableAfterUpdate;

	private Vector3D? _stabilizationDisableRelativePositionExtra;

	private Vector3D? _stabilizationDisableRelativeVelocityExtra;

	public ManeuverCourse CurrentCourse
	{
		get
		{
			if (_currentCourse == null && this is Ship)
			{
				SpaceObjectVessel mainVessel = (this as SpaceObjectVessel).MainVessel;
				if (mainVessel._currentCourse != null && mainVessel.MainVessel.FTL != null && mainVessel.FTL.Status == SystemStatus.OnLine)
				{
					return (this as SpaceObjectVessel).MainVessel._currentCourse;
				}
				SpaceObjectVessel courseVessel = (this as SpaceObjectVessel).MainVessel.AllDockedVessels.FirstOrDefault((SpaceObjectVessel m) => m._currentCourse != null && m.FTL is
				{
					Status: SystemStatus.OnLine
				});
				if (courseVessel != null)
				{
					return courseVessel._currentCourse;
				}
			}
			return _currentCourse;
		}
		set
		{
			_currentCourse = value;
		}
	}

	public override Vector3D Position => Orbit.Position;

	public override Vector3D Velocity => Orbit.Velocity;

	public override QuaternionD Rotation
	{
		get
		{
			return _rotation;
		}
		set
		{
			if (_rotation != value)
			{
				LastOrientationChangeTime = Server.Instance.SolarSystem.CurrentTime;
			}
			_rotation = value;
		}
	}

	public bool MarkForDestruction
	{
		get
		{
			return _markForDestruction;
		}
		set
		{
			_markForDestruction = value;
			Server.Instance.SolarSystem.CheckDestroyMarkedBodies = true;
		}
	}

	public SpaceObjectVessel StabilizeToTargetObj { get; private set; }

	public Vector3D StabilizeToTargetRelPosition { get; protected set; }

	public double StabilizeToTargetTime { get; private set; }

	public ArtificialBody(long guid, bool initializeOrbit, Vector3D position, Vector3D velocity, QuaternionD rotation)
		: base(guid)
	{
		if (initializeOrbit)
		{
			InitializeOrbit(position, velocity, rotation);
		}
	}

	public void InitializeOrbit(Vector3D position, Vector3D velocity, QuaternionD rotation, OrbitParameters orbit = null)
	{
		Orbit.SetArtificialBody(this);
		InitializeFromStateVectors(position, velocity, orbit);
		Rotation = rotation;
		Server.Instance.SolarSystem.AddArtificialBody(this);
		RefreshSunlightExposure();
	}

	public override async Task Destroy()
	{
		await base.Destroy();
		await DisableStabilization(disableForChildren: true, updateBeforeDisable: false);
		foreach (Player pl in Server.Instance.AllPlayers)
		{
			if (pl.IsSubscribedTo(Guid))
			{
				pl.UnsubscribeFrom(this);
			}
			if (pl.CurrentSpawnPoint != null && pl.CurrentSpawnPoint.Ship == this)
			{
				pl.SetSpawnPoint(null);
			}
			if (pl.AuthorizedSpawnPoint != null && pl.AuthorizedSpawnPoint.Ship == this)
			{
				pl.ClearAuthorizedSpawnPoint();
			}
		}
		Server.Instance.SolarSystem.RemoveArtificialBody(this);
	}

	public void InitializeFromStateVectors(Vector3D position, Vector3D velocity, OrbitParameters orbit = null)
	{
		CelestialBody cbParent = Server.Instance.SolarSystem.FindCelestialBodyParent(position);
		if (velocity.SqrMagnitude < double.Epsilon)
		{
			velocity = Rotation * Vector3D.Forward * cbParent.Orbit.CircularOrbitVelocityMagnitudeAtDistance(Vector3D.Distance(cbParent.Position, position));
		}
		if (orbit != null)
		{
			Orbit.InitFromPeriapisAndApoapsis(cbParent.Orbit, orbit.PeriapsisDistance, orbit.ApoapsisDistance, orbit.Inclination, orbit.ArgumentOfPeriapsis, orbit.LongitudeOfAscendingNode, orbit.TrueAnomalyAtZeroTime() * (180.0 / System.Math.PI), 0.0);
		}
		else
		{
			Orbit.InitFromStateVectors(cbParent.Orbit, position, velocity, Server.Instance.SolarSystem.CurrentTime, areValuesRelative: false);
		}
		Orbit.SetArtificialBody(this);
	}

	private void ApplyRotation(double deltaTime)
	{
		if (ObjectType is not (SpaceObjectType.Player or SpaceObjectType.Ship or SpaceObjectType.Asteroid))
		{
			return;
		}

		double speed = AngularVelocity.Magnitude;
		if (speed > Server.MaxAngularVelocity)
		{
			AngularVelocity = AngularVelocity / speed * Server.MaxAngularVelocity;
			speed = Server.MaxAngularVelocity;
		}

		if (speed > 1E-09)
		{
			Rotation = Rotation * QuaternionD.AngleAxis(speed * deltaTime, AngularVelocity / speed);
		}
	}

	private async Task UpdatePosition()
	{
		double deltaTime = Server.SolarSystemTime - _prevUpdatePositionTime;
		_prevUpdatePositionTime = Server.SolarSystemTime;
		if (deltaTime <= double.Epsilon)
		{
			return;
		}
		if (this is Ship && !(this as Ship).IsMainVessel)
		{
			((this as Ship).MainVessel as Ship).AddDockedVesselsThrust(this as Ship, deltaTime);
			return;
		}
		if (_stabilizationDisabledTime.HasValue)
		{
			if (_stabilizationDisabledTime.Value.IsEpsilonEqualD(Server.Instance.SolarSystem.CurrentTime))
			{
				_stabilizationDisabledTime = null;
				return;
			}
			_stabilizationDisabledTime = null;
		}
		if (await CheckCurrentCourse())
		{
			Orbit.InitFromCurrentStateVectors(Server.Instance.SolarSystem.CurrentTime);
		}
		else
		{
			Orbit.UpdateOrbit();
		}
		if (await CheckThrustAndRotation(deltaTime))
		{
			if (CurrentCourse != null)
			{
				await CurrentCourse.Invalidate();
			}
			Orbit.InitFromCurrentStateVectors(Server.Instance.SolarSystem.CurrentTime);
		}
		if (this is SpaceObjectVessel)
		{
			(this as SpaceObjectVessel).SetPhysicsParameters();
		}
		if (CheckGravityInfluenceRadius())
		{
			if (CurrentCourse != null)
			{
				await CurrentCourse.OrbitParentChanged();
			}
			Orbit.InitFromCurrentStateVectors(Server.Instance.SolarSystem.CurrentTime);
		}
		if (CheckPlanetDeath())
		{
			MarkForDestruction = true;
		}
	}

	public async Task UpdateStabilization()
	{
		if (StabilizeToTargetObj == null || this is not Ship || (this as Ship).IsDocked)
		{
			return;
		}
		if (CurrentCourse != null)
		{
			await CurrentCourse.Invalidate();
		}
		Orbit.CopyDataFrom(StabilizeToTargetObj.Orbit, Server.Instance.SolarSystem.CurrentTime, exactCopy: true);
		Orbit.RelativePosition += StabilizeToTargetRelPosition;
		if (_stabilizationDisableRelativePositionExtra.HasValue)
		{
			Orbit.RelativePosition += _stabilizationDisableRelativePositionExtra.Value;
			_stabilizationDisableRelativePositionExtra = null;
		}
		if (_stabilizationDisableRelativeVelocityExtra.HasValue)
		{
			Orbit.RelativeVelocity += _stabilizationDisableRelativeVelocityExtra.Value;
			_stabilizationDisableRelativeVelocityExtra = null;
		}
		Orbit.InitFromCurrentStateVectors(Server.Instance.SolarSystem.CurrentTime);
		if (this is SpaceObjectVessel)
		{
			(this as SpaceObjectVessel).SetPhysicsParameters();
		}
		if (StabilizedToTargetChildren.Count <= 0)
		{
			return;
		}
		foreach (SpaceObjectVessel ab in StabilizedToTargetChildren)
		{
			await ab.UpdateStabilization();
		}
	}

	private async Task<bool> CheckCurrentCourse()
	{
		if (CurrentCourse is not { IsValid: true })
		{
			return false;
		}
		if (CurrentCourse.IsActivated && !CurrentCourse.IsStartingSoonSent && CurrentCourse.StartSolarSystemTime > Server.Instance.SolarSystem.CurrentTime && Server.Instance.SolarSystem.CurrentTime >= CurrentCourse.StartSolarSystemTime - ManeuverCourse.StartingSoonTime)
		{
			await CurrentCourse.SendCourseStartingSoonResponse();
		}
		else if (CurrentCourse.StartSolarSystemTime <= Server.Instance.SolarSystem.CurrentTime)
		{
			if (!CurrentCourse.IsActivated)
			{
				await CurrentCourse.Invalidate();
				return false;
			}
			if (!CurrentCourse.IsInProgress)
			{
				if (!await CurrentCourse.StartManeuver())
				{
					return false;
				}
				if (!CurrentCourse.IsStartingSoonSent)
				{
					await CurrentCourse.SendCourseStartingSoonResponse();
				}
			}
			Vector3D relativePosition = Vector3D.Zero;
			Vector3D relativeVelocity = Vector3D.Zero;
			CurrentCourse.FillPositionAndVelocityAtCurrentTime(ref relativePosition, ref relativeVelocity);
			Orbit.RelativePosition = relativePosition;
			Orbit.RelativeVelocity = relativeVelocity;
			if (CurrentCourse.Type is ManeuverType.Engine or ManeuverType.Transfer)
			{
				Vector3D forward = Rotation * Vector3D.Forward;
				Vector3D up = Rotation * Vector3D.Up;
				Vector3D right = Vector3D.Cross(forward, up);
				forward = Vector3D.Lerp(forward, Orbit.RelativeVelocity.Normalized, Server.Instance.DeltaTime).Normalized;
				up = Vector3D.Cross(right, forward);
				Rotation = QuaternionD.LookRotation(forward, up);
			}
			if (CurrentCourse.EndSolarSystemTime <= Server.Instance.SolarSystem.CurrentTime)
			{
				CurrentCourse.SetFinalPosition();
				await CurrentCourse.ReadNextManeuverCourse();
			}
			return true;
		}
		return false;
	}

	private void ApplyThrust(double timeDelta, ref Vector3D thrust)
	{
		if ((StabilizeToTargetObj != null || StabilizedToTargetChildren.Count > 0) && thrust.IsNotEpsilonZero(0.001))
		{
			if (StabilizeToTargetObj != null)
			{
				DisableStabilizationAfterUpdate(thrust * timeDelta, thrust);
			}
			else
			{
				foreach (SpaceObjectVessel ab in StabilizedToTargetChildren)
				{
					ab.DisableStabilizationAfterUpdate(-thrust * timeDelta, -thrust);
				}
			}
		}
		Orbit.RelativePosition += thrust * timeDelta;
		Orbit.RelativeVelocity += thrust;
		thrust = Vector3D.Zero;
	}

	private async Task<bool> CheckThrustAndRotation(double timeDelta)
	{
		bool recalculateOrbit = false;
		if (this is Ship)
		{
			Ship sh = this as Ship;
			if (sh.CalculateEngineThrust(timeDelta))
			{
				recalculateOrbit = true;
				ApplyThrust(timeDelta, ref sh.EngineThrustVelocityDifference);
			}
			if (sh.CalculateRcsThrust(timeDelta))
			{
				recalculateOrbit = true;
				ApplyThrust(timeDelta, ref sh.RcsThrustVelocityDifference);
			}
			if (PhysicsVelocityDifference.IsNotEpsilonZero(0.001))
			{
				recalculateOrbit = true;
				ApplyThrust(timeDelta, ref PhysicsVelocityDifference);
			}
			if (sh.CalculateRotationThrust(timeDelta))
			{
				AngularVelocity += sh.RotationThrustVelocityDifference;
				sh.RotationThrustVelocityDifference = Vector3D.Zero;
			}
			if (!sh.CalculateRotationDampen(timeDelta))
			{
				await sh.CalculateAutoStabilizeRotation(timeDelta);
			}
			if (PhysicsRotationDifference.IsNotEpsilonZero(0.001))
			{
				AngularVelocity += PhysicsRotationDifference;
				PhysicsRotationDifference = Vector3D.Zero;
			}
			if (AngularVelocity.IsNotEpsilonZero())
			{
				ApplyRotation(timeDelta);
			}
		}
		else if (this is Asteroid)
		{
			if (PhysicsVelocityDifference.IsNotEpsilonZero(0.001))
			{
				recalculateOrbit = true;
				ApplyThrust(timeDelta, ref PhysicsVelocityDifference);
			}
			if (PhysicsRotationDifference.IsNotEpsilonZero(0.001))
			{
				AngularVelocity += PhysicsRotationDifference;
				PhysicsRotationDifference = Vector3D.Zero;
			}
			if (AngularVelocity.IsNotEpsilonZero())
			{
				ApplyRotation(timeDelta);
			}
		}
		return recalculateOrbit;
	}

	private bool CheckGravityInfluenceRadius()
	{
		if (!double.IsInfinity(Orbit.Parent.GravityInfluenceRadiusSquared) && Orbit.RelativePosition.SqrMagnitude > Orbit.Parent.GravityInfluenceRadiusSquared)
		{
			Orbit.ChangeOrbitParent(Orbit.Parent.Parent);
			return true;
		}
		if (Orbit.Parent.CelestialBody.ChildBodies.Count > 0)
		{
			foreach (CelestialBody cb in Orbit.Parent.CelestialBody.ChildBodies)
			{
				if (Orbit.Position.DistanceSquared(cb.Position) < cb.Orbit.GravityInfluenceRadiusSquared)
				{
					Orbit.ChangeOrbitParent(cb.Orbit);
					return true;
				}
			}
		}
		return false;
	}

	protected virtual bool CheckPlanetDeath()
	{
		if (Orbit.RelativePosition.SqrMagnitude < System.Math.Pow(Orbit.Parent.Radius + Server.CelestialBodyDeathDistance, 2.0))
		{
			return true;
		}
		return false;
	}

	public async Task Update()
	{
		if (this is Ship ship)
		{
			if (!ship.IsMainVessel)
			{
				return;
			}
			ship.ExtraRcsThrustVelocityDifference = Vector3D.Zero;
			ship.ExtraRotationThrustVelocityDifference = Vector3D.Zero;
			foreach (Ship docked in ship.AllDockedVessels.Cast<Ship>())
			{
				await docked.UpdatePosition();
			}
			await UpdatePosition();
		}
		else
		{
			await UpdatePosition();
		}
	}

	public async Task AfterUpdate()
	{
		if (_stabilizationDisableAfterUpdate.HasValue && _stabilizationDisableAfterUpdate.Value)
		{
			await DisableStabilization(disableForChildren: true, updateBeforeDisable: true);
		}
		else if (StabilizeToTargetObj != null)
		{
			await UpdateStabilization();
		}

	}

	public bool StabilizeToTarget(SpaceObjectVessel vessel, bool forceStabilize = false)
	{
		if (this is not SpaceObjectVessel || vessel == null)
		{
			return false;
		}
		SpaceObjectVessel mainVessel = (this as SpaceObjectVessel).MainVessel;
		SpaceObjectVessel targetMainVessel = GetStabilizationTarget(vessel);
		if (targetMainVessel == mainVessel)
		{
			return false;
		}
		if (targetMainVessel.StabilizedToTargetChildren.Contains(this as SpaceObjectVessel))
		{
			return false;
		}
		if (!forceStabilize)
		{
			if (targetMainVessel.Guid != vessel.MainVessel.Guid)
			{
				if ((targetMainVessel.Velocity - mainVessel.Velocity).Magnitude > StabilizeToTargetMaxVelocityDiff || (vessel.MainVessel.Position - mainVessel.Position).Magnitude - vessel.MainVessel.Radius > StabilizeToTargetMaxPositionDiff)
				{
					return false;
				}
			}
			else if ((targetMainVessel.Velocity - mainVessel.Velocity).Magnitude > StabilizeToTargetMaxVelocityDiff || (targetMainVessel.Position - mainVessel.Position).Magnitude - targetMainVessel.Radius > StabilizeToTargetMaxPositionDiff)
			{
				return false;
			}
		}
		StabilizeToTargetObj = targetMainVessel;
		StabilizeToTargetRelPosition = Position - targetMainVessel.Position;
		targetMainVessel.StabilizedToTargetChildren.Add(this as SpaceObjectVessel);
		StabilizeToTargetTime = Server.Instance.SolarSystem.CurrentTime;
		return true;
	}

	private SpaceObjectVessel GetStabilizationTarget(SpaceObjectVessel vessel)
	{
		SpaceObjectVessel target = vessel.MainVessel;
		if (target.StabilizeToTargetObj != null)
		{
			target = GetStabilizationTarget(target.StabilizeToTargetObj);
		}
		return target;
	}

	public async Task DisableStabilization(bool disableForChildren, bool updateBeforeDisable)
	{
		if (this is not SpaceObjectVessel)
		{
			return;
		}
		_stabilizationDisableAfterUpdate = null;
		if (StabilizeToTargetObj != null)
		{
			if (updateBeforeDisable)
			{
				await UpdateStabilization();
			}
			StabilizeToTargetObj.StabilizedToTargetChildren.Remove(this as SpaceObjectVessel);
			StabilizeToTargetObj = null;
			_stabilizationDisabledTime = Server.Instance.SolarSystem.CurrentTime;
		}
		if (!disableForChildren || StabilizedToTargetChildren.Count <= 0)
		{
			return;
		}
		List<SpaceObjectVessel> reStabilizeChildren = new List<SpaceObjectVessel>(StabilizedToTargetChildren);
		int sanityCheck = 0;
		while (StabilizedToTargetChildren.Count > 0 && sanityCheck < 1000)
		{
			await StabilizedToTargetChildren[0].DisableStabilization(disableForChildren: false, updateBeforeDisable);
			sanityCheck++;
		}
		if (sanityCheck >= 1000)
		{
			Debug.LogError("When disabling stabilization for", Guid, "children, sanity check reached", sanityCheck);
		}
		if (reStabilizeChildren.Count <= 1)
		{
			return;
		}
		SpaceObjectVessel newMain = null;
		foreach (SpaceObjectVessel ves in reStabilizeChildren)
		{
			if (newMain == null)
			{
				newMain = ves;
			}
			else
			{
				ves.StabilizeToTarget(newMain, forceStabilize: true);
			}
		}
	}

	public void DisableStabilizationAfterUpdate(Vector3D? relativePositionExtra, Vector3D? relativeVelocityExtra)
	{
		if (StabilizeToTargetObj != null)
		{
			_stabilizationDisableAfterUpdate = true;
			_stabilizationDisableRelativePositionExtra = relativePositionExtra;
			_stabilizationDisableRelativeVelocityExtra = relativeVelocityExtra;
		}
	}
}
