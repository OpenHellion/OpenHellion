using System;
using System.Collections.Generic;
using System.Linq;
using OpenHellion;
using UnityEngine;
using ZeroGravity.Data;
using ZeroGravity.LevelDesign;
using ZeroGravity.Math;
using ZeroGravity.Network;
using ZeroGravity.ShipComponents;

namespace ZeroGravity.Objects
{
	public class ManeuverCourse : MonoBehaviour
	{
		public enum FeasibilityErrorType
		{
			None,
			AccelerationLow,
			AccelerationHigh,
			FtlManeuverIndex,
			FtlCellFuel,
			FtlCapacity,
			FtlOnline,
			CourseImpossible,
			ToManyDockedVessels
		}

		public OrbitParameters MyShipOrbit;

		public OrbitParameters StartOrbit = new OrbitParameters();

		public ManeuverTransfer Transfer = new ManeuverTransfer();

		public OrbitParameters EndOrbit;

		public long TargetGuid;

		public SpaceObjectType TargetType;

		public LineRenderer ManeuverLine;

		public Color WarpFeasibleColor = Colors.Green;

		public Color WarpNotFeasibleColor = Colors.Red;

		public Transform StartPosition;

		public Transform EndPosition;

		private long _nextCourseGuid = 100L;

		public long GUID;

		public bool Initialized;

		private bool _courseLocked;

		public bool IsDragging;

		public double StartTime = 60.0;

		public double EndTime = 240.0;

		private double _startTimeSincePeriapsis;

		private double _endTimeSincePeriapsis;

		private double _timeToStart;

		private double _timeToTarget;

		private bool _isPossible;

		private bool _isFeasible;

		public bool ManeuverError;

		public bool WarpCellError;

		public bool PowerError;

		public bool DockingStructureError;

		public double ManeuverDistance;

		public float FuelAmountRequired;

		public Transform DraggingParameter;

		private Vector3 _periapsisPosition;

		private double _oldRayAngle;

		private double _oldTime;

		private Map Map => _world.Map;

		private double ObjectScale => Map.Scale / 149597870700.0;

		private Ship MyShip => Map.ParentShip;

		private bool IsPossibleAndFeasible => _isPossible && _isFeasible;

		public FeasibilityErrorType FeasibilityError { get; private set; }

		private World _world;

		public ManeuverCourse()
		{
			GUID = _nextCourseGuid++;
		}

		private void Update()
		{
			UpdateManeuver();
		}

		public void Initialize(World world)
		{
			_world = world;
			StartOrbit.InitFromPeriapisAndApoapsis(MyShipOrbit.Parent, MyShipOrbit.PeriapsisDistance,
				MyShipOrbit.ApoapsisDistance, MyShipOrbit.Inclination, MyShipOrbit.ArgumentOfPeriapsis,
				MyShipOrbit.LongitudeOfAscendingNode, MyShipOrbit.TrueAnomalyAtZeroTime() * (180.0 / Mathf.PI), 0.0);
			StartOrbit.ResetOrbit(_world.SolarSystem.CurrentTime);
			_startTimeSincePeriapsis = StartOrbit.TimeSincePeriapsis;
			_endTimeSincePeriapsis = EndOrbit.TimeSincePeriapsis;
			StartTime = 60.0;
			EndTime = 300.0;
		}

		private void UpdateManeuver()
		{
			StartOrbit.ResetOrbit(_world.SolarSystem.CurrentTime);
			Transfer.StartOrbitAngle =
				(float)(StartOrbit.TrueAnomalyAtZeroTimePlusTime(_startTimeSincePeriapsis + StartTime) *
				        (180.0 / Mathf.PI));
			Transfer.EndOrbitAngle = (float)(EndOrbit.TrueAnomalyAtZeroTimePlusTime(_endTimeSincePeriapsis + EndTime) *
			                                 (180.0 / Mathf.PI));
			_timeToStart = _startTimeSincePeriapsis + StartTime - StartOrbit.TimeSincePeriapsis;
			_timeToTarget = _endTimeSincePeriapsis + EndTime - EndOrbit.TimeSincePeriapsis;
			Transfer.StartSolarSystemTime = _world.SolarSystem.CurrentTime + _timeToStart;
			Transfer.EndSolarSystemTime = _world.SolarSystem.CurrentTime + _timeToTarget;
			Vector3 position =
				((StartOrbit.PositionAtTrueAnomaly(Transfer.StartOrbitAngle * (Mathf.PI / 180.0),
					getRelativePosition: true) + (StartOrbit.Parent.Position - Map.Focus)) * ObjectScale).ToVector3();
			Vector3 position2 =
				((EndOrbit.PositionAtTrueAnomaly(Transfer.EndOrbitAngle * (Mathf.PI / 180.0),
					getRelativePosition: true) + (EndOrbit.Parent.Position - Map.Focus)) * ObjectScale).ToVector3();
			ManeuverLine.SetPosition(0, position);
			ManeuverLine.SetPosition(1, position2);
			StartPosition.position = position;
			EndPosition.position = position2;
			FuelAmountRequired = 0f;
			_isPossible = true;
			_isFeasible = true;
			FeasibilityError = FeasibilityErrorType.None;
			ManeuverError = false;
			WarpCellError = false;
			PowerError = false;
			DockingStructureError = false;
			float num = (float)(EndTime - StartTime);
			_timeToStart = _startTimeSincePeriapsis + StartTime - StartOrbit.TimeSincePeriapsis;
			_timeToTarget = _timeToStart + num;
			if (_timeToStart < 0.0)
			{
				_timeToStart += StartOrbit.OrbitalPeriod;
			}

			if (_timeToTarget < 0.0)
			{
				_timeToTarget += EndOrbit.OrbitalPeriod;
			}

			Transfer.StartEta = (float)_timeToStart;
			Transfer.EndEta = (float)_timeToTarget;
			Transfer.TravelTime = num;
			Transfer.Type = ManeuverType.Warp;
			if (num <= 0f)
			{
				InvalidateTransfer();
				return;
			}

			bool getRelativePosition = StartOrbit.Parent == EndOrbit.Parent;
			Vector3D vector3D = StartOrbit.PositionAfterTime(_timeToStart, getRelativePosition);
			Vector3D vector3D2 = EndOrbit.PositionAfterTime(_timeToTarget, getRelativePosition);
			ManeuverDistance = Vector3D.Distance(vector3D, vector3D2);
			if (MyShip.GetCompoundMass() > MyShip.Mass + MyShip.FTLEngine.TowingCapacity ||
			    (Transfer.WarpIndex == 0 && MyShip.MainVessel.AllDockedVessels.Count > 0))
			{
				_isFeasible = false;
				FeasibilityError = FeasibilityErrorType.ToManyDockedVessels;
				DockingStructureError = true;
			}
			else
			{
				DockingStructureError = false;
			}

			if (MyShip.SceneId == GameScenes.SceneId.AltCorp_Shuttle_SARA)
			{
				SceneDockingPort sceneDockingPort =
					MyShip.DockingPorts.Values.FirstOrDefault((SceneDockingPort m) => m.DockingPortOrder == 1);
				if (sceneDockingPort != null && sceneDockingPort.DockedToPort != null)
				{
					_isFeasible = false;
					FeasibilityError = FeasibilityErrorType.ToManyDockedVessels;
					DockingStructureError = true;
				}
				else
				{
					DockingStructureError = false;
				}
			}

			float num2 = (float)(MyShip.GetCompoundMass() / MyShip.Mass);
			if (MyShip.FTLEngine.WarpsData.Length <= Transfer.WarpIndex)
			{
				_isFeasible = false;
				FeasibilityError = FeasibilityErrorType.FtlManeuverIndex;
				WarpCellError = true;
			}
			else
			{
				WarpCellError = false;
			}

			WarpData warpData = MyShip.FTLEngine.WarpsData[Transfer.WarpIndex];
			FuelAmountRequired = warpData.ActivationCellConsumption + num * warpData.CellConsumption * num2;
			int num3 = 0;
			foreach (KeyValuePair<int, float?> item in MyShip.FTLEngine.WarpCellsFuel)
			{
				if (item.Value.HasValue)
				{
					num3++;
				}
			}

			float num4 = 0f;
			foreach (int warpCell in Transfer.WarpCells)
			{
				num4 += !MyShip.FTLEngine.WarpCellsFuel.ContainsKey(warpCell) ||
				        !MyShip.FTLEngine.WarpCellsFuel[warpCell].HasValue
					? 0f
					: MyShip.FTLEngine.WarpCellsFuel[warpCell].Value;
			}

			if (num4 < FuelAmountRequired)
			{
				_isFeasible = false;
				FeasibilityError = FeasibilityErrorType.FtlCellFuel;
				WarpCellError = true;
			}
			else
			{
				WarpCellError = false;
			}

			if (warpData.PowerConsumption > MyShip.Capacitor.Capacity)
			{
				_isFeasible = false;
				FeasibilityError = FeasibilityErrorType.FtlCapacity;
				PowerError = true;
			}
			else
			{
				PowerError = false;
			}

			double num5 = 4.0 * (vector3D - vector3D2).Magnitude / (num * num);
			if (num5 > warpData.MaxAcceleration)
			{
				_isFeasible = false;
				FeasibilityError = FeasibilityErrorType.AccelerationLow;
				ManeuverError = true;
			}
			else if (num5 < warpData.MinAcceleration)
			{
				_isFeasible = false;
				FeasibilityError = FeasibilityErrorType.AccelerationHigh;
				ManeuverError = true;
			}
			else
			{
				ManeuverError = false;
			}

			if (!_isFeasible)
			{
			}

			if (IsPossibleAndFeasible)
			{
				ManeuverLine.startColor = WarpFeasibleColor;
				ManeuverLine.endColor = WarpFeasibleColor;
			}
			else
			{
				ManeuverLine.startColor = WarpNotFeasibleColor;
				ManeuverLine.endColor = WarpNotFeasibleColor;
			}
		}

		private void InvalidateTransfer()
		{
			_isPossible = false;
			ManeuverLine.startColor = WarpNotFeasibleColor;
			ManeuverLine.endColor = WarpNotFeasibleColor;
			FeasibilityError = FeasibilityErrorType.CourseImpossible;
		}

		public void AddStartTime(float time)
		{
			StartTime += time;
		}

		public void AddEndTime(float time)
		{
			EndTime += time;
		}

		public void StopDragging()
		{
			IsDragging = false;
		}

		public void StartDragging(GameObject dragObject, Ray ray)
		{
			DraggingParameter = dragObject.transform;
			Vector3 zero = Vector3.zero;
			Vector3D centerPosition = Vector3D.Zero;
			QuaternionD rotation = QuaternionD.Identity;
			Vector3 zero2 = Vector3.zero;
			if (dragObject == StartPosition.gameObject)
			{
				StartOrbit.GetOrbitPlaneData(out rotation, out centerPosition);
				zero2 = ((StartOrbit.Parent.Position - Map.Focus) * ObjectScale).ToVector3();
				_periapsisPosition =
					((StartOrbit.PositionAtTrueAnomaly(0.0, getRelativePosition: false) - Map.Focus) * ObjectScale)
					.ToVector3();
				zero = MathHelper.RayPlaneIntersect(ray, zero2, rotation.ToQuaternion() * Vector3.up);
				_oldRayAngle = MathHelper.AngleSigned((zero2 - zero).normalized,
					(zero2 - _periapsisPosition).normalized, rotation.ToQuaternion() * Vector3.up);
				if (_oldRayAngle < 0.0)
				{
					_oldRayAngle += 360.0;
				}

				if (_oldRayAngle > 360.0)
				{
					_oldRayAngle -= 360.0;
				}

				_oldRayAngle = StartOrbit.CalculatesMeanAnomalyFromTrueAnomaly(_oldRayAngle * (Mathf.PI / 180.0)) *
				               (180.0 / Mathf.PI);
				_oldTime = _oldRayAngle * StartOrbit.OrbitalPeriod / 360.0 - _startTimeSincePeriapsis;
			}
			else if (dragObject == EndPosition.gameObject)
			{
				EndOrbit.GetOrbitPlaneData(out rotation, out centerPosition);
				zero2 = ((EndOrbit.Parent.Position - Map.Focus) * ObjectScale).ToVector3();
				_periapsisPosition =
					((EndOrbit.PositionAtTrueAnomaly(0.0, getRelativePosition: false) - Map.Focus) * ObjectScale)
					.ToVector3();
				zero = MathHelper.RayPlaneIntersect(ray, zero2, rotation.ToQuaternion() * Vector3.up);
				_oldRayAngle = MathHelper.AngleSigned((zero2 - zero).normalized,
					(zero2 - _periapsisPosition).normalized, rotation.ToQuaternion() * Vector3.up);
				if (_oldRayAngle < 0.0)
				{
					_oldRayAngle += 360.0;
				}

				if (_oldRayAngle > 360.0)
				{
					_oldRayAngle -= 360.0;
				}

				_oldRayAngle = EndOrbit.CalculatesMeanAnomalyFromTrueAnomaly(_oldRayAngle * (Mathf.PI / 180.0)) *
				               (180.0 / Mathf.PI);
				_oldTime = _oldRayAngle * EndOrbit.OrbitalPeriod / 360.0 - _endTimeSincePeriapsis;
			}

			IsDragging = true;
		}

		public void Dragging(Ray ray)
		{
			Vector3 zero = Vector3.zero;
			Vector3D centerPosition = Vector3D.Zero;
			QuaternionD rotation = QuaternionD.Identity;
			Vector3 zero2 = Vector3.zero;
			if (DraggingParameter.gameObject == StartPosition.gameObject)
			{
				StartOrbit.GetOrbitPlaneData(out rotation, out centerPosition);
				zero2 = ((StartOrbit.Parent.Position - Map.Focus) * ObjectScale).ToVector3();
				_periapsisPosition =
					((StartOrbit.PositionAtTrueAnomaly(0.0, getRelativePosition: false) - Map.Focus) * ObjectScale)
					.ToVector3();
				zero = MathHelper.RayPlaneIntersect(ray, zero2, rotation.ToQuaternion() * Vector3.up);
				double num = MathHelper.AngleSigned((zero2 - zero).normalized, (zero2 - _periapsisPosition).normalized,
					rotation.ToQuaternion() * Vector3.up);
				if (num < 0.0)
				{
					num += 360.0;
				}

				if (num > 360.0)
				{
					num -= 360.0;
				}

				num = StartOrbit.CalculatesMeanAnomalyFromTrueAnomaly(num * (Mathf.PI / 180.0)) * (180.0 / Mathf.PI) -
				      _oldRayAngle;
				StartTime = num * StartOrbit.OrbitalPeriod / 360.0 + _oldTime;
			}
			else if (DraggingParameter.gameObject == EndPosition.gameObject)
			{
				EndOrbit.GetOrbitPlaneData(out rotation, out centerPosition);
				zero2 = ((EndOrbit.Parent.Position - Map.Focus) * ObjectScale).ToVector3();
				_periapsisPosition =
					((EndOrbit.PositionAtTrueAnomaly(0.0, getRelativePosition: false) - Map.Focus) * ObjectScale)
					.ToVector3();
				zero = MathHelper.RayPlaneIntersect(ray, zero2, rotation.ToQuaternion() * Vector3.up);
				double num2 = MathHelper.AngleSigned((zero2 - zero).normalized, (zero2 - _periapsisPosition).normalized,
					rotation.ToQuaternion() * Vector3.up);
				if (num2 < 0.0)
				{
					num2 += 360.0;
				}

				if (num2 > 360.0)
				{
					num2 -= 360.0;
				}

				num2 = EndOrbit.CalculatesMeanAnomalyFromTrueAnomaly(num2 * (Mathf.PI / 180.0)) * (180.0 / Mathf.PI) -
				       _oldRayAngle;
				EndTime = num2 * EndOrbit.OrbitalPeriod / 360.0 + _oldTime;
			}
		}
	}
}
