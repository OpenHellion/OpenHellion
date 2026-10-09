using System;
using System.Collections.Generic;
using OpenHellion;
using UnityEngine;
using UnityEngine.Serialization;
using ZeroGravity.Data;
using ZeroGravity.Math;
using ZeroGravity.Objects;

namespace ZeroGravity.ShipComponents
{
	public abstract class MapObject : MonoBehaviour
	{
		public CelestialBodyData CelestialBodyData;

		public IMapMainObject MainObject;

		public Transform Position;

		public Transform Visual;

		public Transform Orbits;

		public LineRenderer MyOrbitRenderer;

		public int NumberOfOrbitPositions = 750;

		public AnimationCurve OrbitFadeCurve;

		public AnimationCurve SelectedOrbitFadeCurve;

		[FormerlySerializedAs("orbitAlpha")] public float OrbitAlpha;

		public Transform OrbitPlane;

		public float OrbitFadeStart = 2f;

		public float OrbitFadeEnd = 2f;

		[FormerlySerializedAs("VisulFadeMultiplier")] public float VisualFadeMultiplier = 0.5f;

		public bool IsDragging;

		public Transform DraggingParameter;

		public bool CanBeDragged;

		public GameObject DistressVisual;

		[HideInInspector] public SphereCollider PositionCollider;

		public Renderer ObjectVisibilityBackground;

		public GameObject NewObjectVisibility;

		public float NewObjectVisibilityDuration;

		protected Color NewObjectColorFadeIncrement;

		protected float CreationTime;

		protected World World;

		public Map Map => World.Map;

		public virtual string Name
		{
			get => MainObject.Name;
			set => throw new NotImplementedException();
		}

		protected double ObjectScale => Map.Scale / 149597870700.0;

		public virtual Vector3D TruePosition => MainObject.Position;

		public virtual float Radius => (float)MainObject.Radius;

		protected Vector3 ObjectPosition => ((TruePosition - Map.Focus) * ObjectScale).ToVector3();

		public virtual OrbitParameters Orbit
		{
			get
			{
				if (MainObject != null)
				{
					return MainObject.Orbit;
				}

				return null;
			}
		}

		public abstract Sprite Icon { get; set; }

		public abstract string Description { get; set; }

		public RadarVisibilityType RadarVisibilityType
		{
			get
			{
				if (MainObject == null)
				{
					return RadarVisibilityType.AlwaysVisible;
				}

				return MainObject.RadarVisibilityType;
			}
		}

		protected virtual bool IsVisibleOnMap => true;

		protected virtual Color OrbitColor
		{
			get
			{
				if (Map != null)
				{
					if (Map.SelectedObject == this)
					{
						return Colors.Cyan;
					}

					return Colors.White;
				}

				return Colors.White;
			}
		}

		public float VesselExposureDamage
		{
			get
			{
				if (MainObject != null)
				{
					return World.GetVesselExposureDamage(MainObject.Orbit.Position.Magnitude);
				}

				return 0f;
			}
		}

		public void OnCreate(World world, IMapMainObject mainObject)
		{
			World = world;
			MainObject = mainObject;
		}

		private void OnEnable()
		{
			SetOrbit();
			if (DistressVisual != null)
			{
				DistressVisual.SetActive(RadarVisibilityType == RadarVisibilityType.Distress);
			}

			if (this == Map.MyShip)
			{
				Position.GetComponent<SphereCollider>().radius = 0.15f;
			}
		}

		protected virtual void Start()
		{
			CreationTime = Time.time;
			CreateVisual();
			UpdateVisibility();
			PositionCollider = Position.GetComponent<SphereCollider>();
		}

		protected virtual void Update()
		{
			if (!Map.gameObject.activeInHierarchy)
			{
				return;
			}

			if (Orbit?.Parent != null)
			{
				transform.position = ((Orbit.Parent.Position - Map.Focus) * ObjectScale).ToVector3();
			}

			UpdateObject();
			UpdateOrbitColors();
			UpdateOrbitPlane();
			if (NewObjectVisibility != null)
			{
				Renderer component = NewObjectVisibility.GetComponent<Renderer>();
				if (component != null)
				{
					Color color = component.material.GetColor("_Tint");
					component.material.SetColor("_Tint", color - NewObjectColorFadeIncrement * Time.deltaTime);
				}
			}
		}

		public abstract void UpdateObject();

		public abstract void CreateVisual();

		public virtual void UpdateOrbitColors()
		{
			if (Orbit != null)
			{
				float num = 8.45228E+09f / (float)Orbit.DistanceAtTrueAnomaly * Map.ClosestSunScale / OrbitFadeEnd;
				float num2 = 8.45228E+09f / (float)Orbit.DistanceAtTrueAnomaly * Map.ClosestSunScale / OrbitFadeStart;
				Color orbitColor = OrbitColor;
				OrbitAlpha = Map.SelectedObject != this
					? OrbitFadeCurve.Evaluate(((float)Map.Scale - num2) * 1f / (num - num2))
					: SelectedOrbitFadeCurve.Evaluate(((float)Map.Scale - num2) * 1f / (num - num2));

				orbitColor.a = OrbitAlpha;
				MyOrbitRenderer.startColor = orbitColor;
				MyOrbitRenderer.endColor = orbitColor;
				if (Map.Scale < num2 * VisualFadeMultiplier && Visual.gameObject.activeInHierarchy)
				{
					Visual.gameObject.SetActive(false);
					Position.GetComponent<SphereCollider>().enabled = false;
				}
				else if (Map.Scale > num2 * VisualFadeMultiplier && !Visual.gameObject.activeInHierarchy)
				{
					Visual.gameObject.SetActive(true);
					Position.GetComponent<SphereCollider>().enabled = true;
				}
			}
		}

		public virtual void SetOrbit()
		{
			var list = Orbit.GetOrbitPositions(NumberOfOrbitPositions, 60.0);
			MyOrbitRenderer.positionCount = list.Count;
			if (list is { Count: > 0 })
			{
				for (int i = 0; i < list.Count; i++)
				{
					MyOrbitRenderer.SetPosition(i, list[i].ToVector3());
				}

				MyOrbitRenderer.SetPosition(NumberOfOrbitPositions - 1, list[0].ToVector3());
			}

			MyOrbitRenderer.transform.SetParent(Orbits);
			MyOrbitRenderer.startColor = OrbitColor;
			MyOrbitRenderer.endColor = OrbitColor;
		}

		public void UpdateOrbitPlane()
		{
			if (!(OrbitPlane == null))
			{
				Orbit.GetOrbitPlaneData(out var rotation, out var centerPosition);
				OrbitPlane.localPosition = (centerPosition * ObjectScale).ToVector3();
				OrbitPlane.localRotation = rotation.ToQuaternion();
				OrbitPlane.localScale =
					(new Vector3D(Orbit.SemiMinorAxis * 2.0, Orbit.SemiMinorAxis * 2.0, Orbit.SemiMajorAxis * 2.0) *
					 ObjectScale).ToVector3();
			}
		}

		public virtual void StartDragging(GameObject dragObject, Ray ray)
		{
			IsDragging = true;
			DraggingParameter = dragObject.transform;
		}

		public virtual void Dragging(Ray ray)
		{
		}

		public virtual void StopDragging()
		{
			IsDragging = false;
		}

		public virtual void UpdateVisibility()
		{
			gameObject.Activate(IsVisibleOnMap);
			if (!IsVisibleOnMap && Map.SelectedObject == this)
			{
				Map.SelectedObject = null;
			}

			if (DistressVisual != null)
			{
				DistressVisual.Activate(RadarVisibilityType == RadarVisibilityType.Distress);
			}
		}

		private void OnDestroy()
		{
			if (Map.SelectedObject == this)
			{
				Map.SelectMapObject(null);
				Map.NavPanel.HideHoverInfo();
			}
		}
	}
}
