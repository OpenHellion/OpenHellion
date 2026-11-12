using System;
using OpenHellion;
using UnityEngine;
using ZeroGravity.Math;

namespace ZeroGravity.Objects
{
	public class CelestialBody : IMapMainObject
	{
		public GameObject PlanetsSpaceGameObject;

		public GameObject PlanetsSpaceGameObjectVisual;

		public string PrefabPath;

		public float AsteroidGasBurstDmgMultiplier;

		public double NoiseRadius;

		private Vector3 _targetPosition;

		public OrbitParameters Orbit { get; set; }

		public long Guid { get; set; }

		public string Name { get; set; }

		public double Radius { get; set; }

		public RadarVisibilityType RadarVisibilityType => RadarVisibilityType.Visible;

		public Vector3D Position => Orbit.Position;

		public CelestialBody ParentCelesitalBody
		{
			get
			{
				if (Orbit.Parent != null && Orbit.Parent.CelestialBody != null)
				{
					return Orbit.Parent.CelestialBody;
				}

				return null;
			}
		}

		public double MaxDistanceFromSun
		{
			get
			{
				double num = Orbit.ApoapsisDistance;
				OrbitParameters parent = Orbit.Parent;
				while ((parent != null ? parent.Parent : null) != null)
				{
					num = parent.ApoapsisDistance + num;
					parent = parent.Parent;
				}

				return num;
			}
		}

		public CelestialBody(long guid)
		{
			Guid = guid;
			Orbit = new OrbitParameters();
			Orbit.SetCelestialBody(this);
		}

		public bool Set(CelestialBody parent, string name, double mass, double radius, double rotPeriod,
			double eccentricity, double semiMajorAxis, double inclination, double argumentOfPeriapsis,
			double longitudeOfAscendingNode, string prefabPath, double solarSystemTime)
		{
			PrefabPath = prefabPath;
			Name = name;
			Radius = radius;
			if (parent != null)
			{
				Orbit.InitFromElements(parent.Orbit, mass, radius, rotPeriod, eccentricity, semiMajorAxis, inclination,
					argumentOfPeriapsis, longitudeOfAscendingNode, 0.0, 0.0);
			}
			else
			{
				Orbit.InitFromElements(null, mass, radius, rotPeriod, eccentricity, semiMajorAxis, inclination,
					argumentOfPeriapsis, longitudeOfAscendingNode, 0.0, 0.0);
			}

			return Orbit.IsOrbitValid;
		}

		/// <summary>
		/// 	Updates the position of the celestial body based on its orbit parameters and the given time.
		/// 	If resetTime is false, time will be delta time since last update, otherwise it will be solar system time.
		/// </summary>
		/// <param name="solarSystem">The system this happens in.</param>
		/// <param name="time">Delta time if resetTime is false, absolute system time if true.</param>
		/// <param name="resetTime">Should orbit be reset?</param>
		public void UpdatePosition(SolarSystem solarSystem, double time, bool resetTime = false)
		{
			if (Orbit.IsOrbitValid)
			{
				if (resetTime)
				{
					Orbit.ResetOrbit(time);
				}
				else
				{
					Orbit.UpdateOrbit(time);
				}

				if (PlanetsSpaceGameObjectVisual != null)
				{
					PlanetsSpaceGameObjectVisual.transform.localRotation =
						Quaternion.AngleAxis((float)Orbit.GetRotationAngle(solarSystem.CurrentTime),
							Vector3.up);
				}
			}
		}

		public void CreateSunSpaceGameObject(Transform sunRoot)
		{
			try
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(Resources.Load(PrefabPath) as GameObject);
				gameObject.transform.SetParent(sunRoot);
				gameObject.SetLayerRecursively("Sun");
				gameObject.name = Name;
				float num = (float)(Orbit.Radius / 149597870.7);
				gameObject.transform.localScale = new Vector3(num, num, num);
			}
			catch (ArgumentException)
			{
				Debug.LogErrorFormat("Could not load {0} with path: {1}", Name, PrefabPath);
			}
		}

		public void CreatePlanetsSpaceGameObject(Transform planetsRoot)
		{
			try
			{
				GameObject parent = new GameObject(Name);
				GameObject child = UnityEngine.Object.Instantiate(Resources.Load(PrefabPath) as GameObject);
				child.transform.parent = parent.transform;
				parent.SetLayerRecursively("Planets");
				parent.transform.SetParent(planetsRoot);
				child.name = Name + "_Visual";
				float num = (float)(Orbit.Radius * 2.0 / 1000000.0);
				child.transform.localScale = new Vector3(num, num, num);
				PlanetsSpaceGameObject = parent;
				PlanetsSpaceGameObjectVisual = child;
			}
			catch (ArgumentException)
			{
				Debug.LogErrorFormat("Could not load planet {0} with path: {1}", Name, PrefabPath);
			}
		}

		public void DestroyPlanetsSpaceGameObject()
		{
			UnityEngine.Object.Destroy(PlanetsSpaceGameObject);
			PlanetsSpaceGameObject = null;
			PlanetsSpaceGameObjectVisual = null;
		}

		public void SetPlanetSpacePosition(Vector3 position, bool forceChange)
		{
			if (PlanetsSpaceGameObject != null)
			{
				if (forceChange)
				{
					PlanetsSpaceGameObject.transform.localPosition = position;
					_targetPosition = position;
				}
				else
				{
					_targetPosition = position;
				}
			}
		}

		public void UpdatePlanetSpacePosition(float deltaTime)
		{
			if (PlanetsSpaceGameObject != null)
			{
				PlanetsSpaceGameObject.transform.localPosition =
					Vector3.Lerp(PlanetsSpaceGameObject.transform.localPosition, _targetPosition, deltaTime);
			}
		}
	}
}
