using System;
using UnityEngine;
using ZeroGravity.Math;
using ZeroGravity.Objects;
using ZeroGravity.UI;

namespace ZeroGravity.ShipComponents
{
	public class MapObjectCustomOrbit : MapObject
	{
		public ManeuverCoordinate CustomOrbitCoordinate = new ManeuverCoordinate();

		private OrbitParameters orbit = new OrbitParameters();

		public Transform PeriapsisIndicator;

		public Transform ApoapsisIndicator;

		public Transform LongitudeOfAscendingNodePlane;

		public Transform LongitudeOfAscendingNodeIndicator;

		public Transform InclinationIndicator;

		public float IndicatorsScale = 10f;

		private double oldPeriapsis;

		private double oldApoapsis;

		private double oldInclination;

		private double oldArgumentOfPeriapsis;

		private double oldLongitudeOfAscendingNode;

		private double oldTrueAnomalyAtZeroTime;

		private float oldPeriapsisRayDistance;

		private float oldApoapsisRayDistance;

		private float oldInclinationRayAngle;

		private float oldArgumentOfPeriapsisRayAngle;

		private float oldLongitudeOfAscendingNodeRayAngle;

		private double oldTrueAnomalyAtZeroTimeRayAngle;

		private bool dragTrueAnomaly;

		private Vector3 oldRayIntersection = Vector3.zero;

		public override string Name => gameObject.name;

		public override OrbitParameters Orbit => orbit;

		public override Vector3D TruePosition => Orbit.Position;

		public override float Radius => 100f;

		protected override Color OrbitColor
		{
			get
			{
				if (Map != null)
				{
					if (Map.SelectedObject == this)
					{
						return Colors.Cyan;
					}

					return Colors.Green;
				}

				return Colors.Green;
			}
		}

		public override Sprite Icon
		{
			get => SpriteManager.Instance.CustomOrbitSprite;
			set { }
		}

		public override string Description
		{
			get => Localization.CustomOrbitDescription;
			set { }
		}

		public override void CreateVisual()
		{
			gameObject.SetLayerRecursively("Map");
		}

		public override void UpdateObject()
		{
			Orbit.UpdateOrbit(World.SolarSystem.CurrentTime);
			Position.position = ObjectPosition;
			Orbits.localScale = Vector3.one * (float)ObjectScale;
			LongitudeOfAscendingNodePlane.localScale = Vector3.one * ((float)(Orbit.Parent.Radius * ObjectScale) *
																	  IndicatorsScale *
																	  Mathf.Clamp(
				                                                          (float)(orbit.PeriapsisDistance /
																			  orbit.Parent.Radius) / 2f, 1f, 100f));
			UpdateOrbitIndicators();
		}

		public void CreateOrbit(MapObject parent)
		{
			orbit.InitFromPeriapisAndApoapsis(parent.Orbit, parent.Orbit.Radius * 2.0, parent.Orbit.Radius * 4.0, 0.0,
				0.0, 0.0, 90.0, World.SolarSystem.CurrentTime);
			orbit.UpdateOrbit(World.SolarSystem.CurrentTime);
			SetOrbit();
			UpdateOrbitPlane();
			UpdateOrbitIndicators();
		}

		public void UpdateOrbitIndicators()
		{
			if (this == Map.SelectedObject)
			{
				PeriapsisIndicator.gameObject.SetActive(true);
				ApoapsisIndicator.gameObject.SetActive(true);
				LongitudeOfAscendingNodePlane.gameObject.SetActive(true);
				PeriapsisIndicator.position =
					((Orbit.PositionAtTimeAfterPeriapsis(0.0, false) - Map.Focus) * ObjectScale).ToVector3();
				ApoapsisIndicator.position =
					((Orbit.PositionAtTimeAfterPeriapsis(Orbit.OrbitalPeriod / 2.0, false) - Map.Focus) *
					 ObjectScale).ToVector3();
				LongitudeOfAscendingNodePlane.position =
					((Orbit.Parent.Position - Map.Focus) * ObjectScale).ToVector3();
				LongitudeOfAscendingNodePlane.localRotation =
					Quaternion.Euler(0f, 0f - (float)Orbit.LongitudeOfAscendingNode, 0f);
				InclinationIndicator.localRotation = Quaternion.Euler((float)Orbit.Inclination + 90f, 0f, 0f);
			}
			else
			{
				PeriapsisIndicator.gameObject.SetActive(false);
				ApoapsisIndicator.gameObject.SetActive(false);
				LongitudeOfAscendingNodePlane.gameObject.SetActive(false);
			}
		}

		public override void StartDragging(GameObject dragObject, Ray ray)
		{
			base.StartDragging(dragObject, ray);
			Vector3 vector = MathHelper.RayPlaneIntersect(ray, OrbitPlane.position, OrbitPlane.up);
			Vector3 vector2 = ((Orbit.Parent.Position - Map.Focus) * ObjectScale).ToVector3();
			oldRayIntersection = PeriapsisIndicator.position;
			Debug.DrawLine(vector2, vector);
			if (dragObject.transform == PeriapsisIndicator)
			{
				oldPeriapsis = Orbit.PeriapsisDistance;
				oldArgumentOfPeriapsis = Orbit.ArgumentOfPeriapsis;
				oldPeriapsisRayDistance = (vector2 - vector).magnitude;
				oldArgumentOfPeriapsisRayAngle = MathHelper.AngleSigned((vector2 - vector).normalized,
					(vector2 - oldRayIntersection).normalized, OrbitPlane.up);
				oldTrueAnomalyAtZeroTime = Orbit.TrueAnomalyAtCurrentTime() * (180.0 / System.Math.PI);
			}
			else if (dragObject.transform == ApoapsisIndicator)
			{
				oldApoapsis = Orbit.ApoapsisDistance;
				oldArgumentOfPeriapsis = Orbit.ArgumentOfPeriapsis;
				oldApoapsisRayDistance = (vector2 - vector).magnitude;
				oldArgumentOfPeriapsisRayAngle = MathHelper.AngleSigned((vector2 - vector).normalized,
					(vector2 - oldRayIntersection).normalized, OrbitPlane.up);
				oldTrueAnomalyAtZeroTime = Orbit.TrueAnomalyAtCurrentTime() * (180.0 / System.Math.PI);
			}
			else if (dragObject.transform == LongitudeOfAscendingNodeIndicator)
			{
				vector = MathHelper.RayPlaneIntersect(ray, vector2, Vector3.up);
				oldLongitudeOfAscendingNode = Orbit.LongitudeOfAscendingNode;
				oldRayIntersection = vector2 + Vector3.right;
				oldLongitudeOfAscendingNodeRayAngle = MathHelper.AngleSigned((vector2 - vector).normalized,
					(vector2 - oldRayIntersection).normalized, Vector3.up);
			}
			else if (dragObject.transform == InclinationIndicator)
			{
				vector = MathHelper.RayPlaneIntersect(ray, vector2, LongitudeOfAscendingNodePlane.right);
				oldRayIntersection = vector2 + LongitudeOfAscendingNodePlane.forward;
				oldInclination = Orbit.Inclination;
				oldInclinationRayAngle = MathHelper.AngleSigned((vector2 - vector).normalized,
					(vector2 - oldRayIntersection).normalized, LongitudeOfAscendingNodePlane.right);
			}
			else if (dragObject.transform == Position)
			{
				oldTrueAnomalyAtZeroTime = Orbit.TrueAnomalyAtCurrentTime() * (180.0 / System.Math.PI);
				oldTrueAnomalyAtZeroTimeRayAngle = MathHelper.AngleSigned((vector2 - vector).normalized,
					(vector2 - oldRayIntersection).normalized, OrbitPlane.up);
				dragTrueAnomaly = true;
			}
		}

		public override void Dragging(Ray ray)
		{
			Vector3 vector = MathHelper.RayPlaneIntersect(ray, OrbitPlane.position, OrbitPlane.up);
			Vector3 vector2 = ((Orbit.Parent.Position - Map.Focus) * ObjectScale).ToVector3();
			double num = Orbit.PeriapsisDistance;
			double num2 = Orbit.ApoapsisDistance;
			double num3 = Orbit.Inclination;
			double num4 = Orbit.ArgumentOfPeriapsis;
			double num5 = Orbit.LongitudeOfAscendingNode;
			double num6 = Orbit.TrueAnomalyAtZeroTime() * (180.0 / System.Math.PI);
			double solarSystemTime = 0.0;
			if (DraggingParameter == PeriapsisIndicator)
			{
				num = oldPeriapsis * (vector2 - vector).magnitude / oldPeriapsisRayDistance;
				if (num > Orbit.ApoapsisDistance)
				{
					num = Orbit.ApoapsisDistance;
				}

				num4 = MathHelper.AngleSigned((vector2 - vector).normalized,
					       (vector2 - oldRayIntersection).normalized, OrbitPlane.up) - oldArgumentOfPeriapsisRayAngle +
				       oldArgumentOfPeriapsis;
				num6 = oldTrueAnomalyAtZeroTime;
				dragTrueAnomaly = true;
			}
			else if (DraggingParameter == ApoapsisIndicator)
			{
				num2 = oldApoapsis * (vector2 - vector).magnitude / oldApoapsisRayDistance;
				if (num2 < Orbit.PeriapsisDistance)
				{
					num2 = Orbit.PeriapsisDistance;
				}
				else if (num2 > Orbit.Parent.GravityInfluenceRadius)
				{
					num2 = Orbit.Parent.GravityInfluenceRadius;
				}

				num4 = MathHelper.AngleSigned((vector2 - vector).normalized,
					       (vector2 - oldRayIntersection).normalized, OrbitPlane.up) - oldArgumentOfPeriapsisRayAngle +
				       oldArgumentOfPeriapsis;
				num6 = oldTrueAnomalyAtZeroTime;
				dragTrueAnomaly = true;
			}
			else if (DraggingParameter == LongitudeOfAscendingNodeIndicator)
			{
				vector = MathHelper.RayPlaneIntersect(ray, vector2, Vector3.up);
				num5 = MathHelper.AngleSigned((vector2 - vector).normalized,
					       (vector2 - oldRayIntersection).normalized, Vector3.up) -
				       oldLongitudeOfAscendingNodeRayAngle +
				       oldLongitudeOfAscendingNode;
			}
			else if (DraggingParameter == InclinationIndicator)
			{
				vector = MathHelper.RayPlaneIntersect(ray, vector2, LongitudeOfAscendingNodePlane.right);
				num3 = 0f - (MathHelper.AngleSigned((vector2 - vector).normalized,
					             (vector2 - oldRayIntersection).normalized,
					             LongitudeOfAscendingNodePlane.right) -
				             oldInclinationRayAngle) + oldInclination;
			}
			else if (DraggingParameter == Position)
			{
				num6 = MathHelper.AngleSigned((vector2 - vector).normalized,
					       (vector2 - oldRayIntersection).normalized, OrbitPlane.up) -
				       oldTrueAnomalyAtZeroTimeRayAngle +
				       oldTrueAnomalyAtZeroTime;
				dragTrueAnomaly = true;
			}

			Debug.DrawLine(vector2, vector, Color.red);
			Debug.DrawLine(vector2, oldRayIntersection, Color.green);
			if (num4 < 0.0)
			{
				num4 += 360.0;
			}

			if (num4 > 360.0)
			{
				num4 -= 360.0;
			}

			if (num6 < 0.0)
			{
				num6 += 360.0;
			}

			if (num6 > 360.0)
			{
				num6 -= 360.0;
			}

			if (num5 < 0.0)
			{
				num5 += 360.0;
			}

			if (num5 > 360.0)
			{
				num5 -= 360.0;
			}

			if (num3 < 0.0)
			{
				num3 += 360.0;
			}

			if (num3 > 360.0)
			{
				num3 -= 360.0;
			}

			if (dragTrueAnomaly)
			{
				solarSystemTime = World.SolarSystem.CurrentTime;
			}

			Orbit.InitFromPeriapisAndApoapsis(Orbit.Parent, num, num2, num3, num4, num5, num6, solarSystemTime);
			Orbit.UpdateOrbit(World.SolarSystem.CurrentTime);
			dragTrueAnomaly = false;
			SetOrbit();
			UpdateOrbitPlane();
		}
	}
}
