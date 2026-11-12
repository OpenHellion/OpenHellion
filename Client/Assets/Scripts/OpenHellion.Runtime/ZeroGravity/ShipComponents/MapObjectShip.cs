using System;
using System.Collections.Generic;
using OpenHellion.Map;
using UnityEngine;
using ZeroGravity.Math;
using ZeroGravity.Objects;
using ZeroGravity.UI;

namespace ZeroGravity.ShipComponents
{
	public class MapObjectShip : MapObjectVessel
	{
		public GameObject ScanningCone;

		public GameObject ScanningEffectCone;

		public float ScanningEffectConeScaleMultiplier;

		public GameObject Pitch;

		public GameObject Yaw;

		public float PitchYawScale;

		public MeshRenderer IconRenderer;

		private bool _scanningConeActive;

		[NonSerialized] public float ScanningConeAngle = 45f;

		private SubSystemRadar MyRadar =>
			(Map != null && Map.MyShip == this) ? Map.ParentShip != null ? Map.ParentShip.RadarSystem : null : null;

		public override string Name => MainObject.Name;

		public override Sprite Icon
		{
			get => SpriteManager.Instance.GetSprite((MainObject as MapItemData)?.Type ?? SpaceObjectType.Ship);
			set { }
		}

		public override string Description
		{
			get => (MainObject as MapItemData)?.Description;
			set { }
		}

		public override void CreateVisual()
		{
			if (MainObject is not null)
			{
				gameObject.SetLayerRecursively("Map");
				SetOrbit();
				UpdateOrbitPlane();
				SetIcon();
			}

			int renderQueue = MathHelper.RandomRange(0, 2000);
			if ((IconRenderer != null ? IconRenderer.material : null) != null)
			{
				IconRenderer.material.renderQueue = renderQueue;
			}

			if ((ObjectVisibilityBackground != null ? ObjectVisibilityBackground.material : null) != null)
			{
				ObjectVisibilityBackground.material.renderQueue = renderQueue;
			}
		}

		public override void UpdateObject()
		{
			Position.position = ObjectPosition;
			Orbits.localScale = Vector3.one * (float)ObjectScale;
			DistressVisual.SetActive(RadarVisibilityType == RadarVisibilityType.Distress);
			if (Map == null || Map.SelectedObject != this)
			{
				ToggleCone(false);
			}

			if (ScanningCone == null)
			{
				return;
			}

			ScanningCone.Activate(_scanningConeActive && !ScanningEffectCone.activeSelf);
			if (ScanningCone.activeSelf && MyRadar != null)
			{
				if (ScanningConeAngle < 1f)
				{
					ScanningConeAngle = 1f;
				}

				float num2 = (float)MyRadar.GetActiveRange(SubSystemRadar.ReferenceCrossSection, ScanningConeAngle,
					SubSystemRadar.FuzzyThreshold, MyRadar.GetNoise());
				float num3 = (float)(2.0 * System.Math.Tan(ScanningConeAngle * (System.Math.PI / 180.0) / 2.0));
				ScanningCone.transform.localScale =
					new Vector3(num3 * num2, num3 * num2, num2) * (float)ObjectScale;
				ScanningEffectCone.transform.localScale =
					ScanningCone.transform.localScale * ScanningEffectConeScaleMultiplier;
				Yaw.transform.localScale =
					new Vector3(PitchYawScale, PitchYawScale, PitchYawScale) * (float)ObjectScale;
			}
		}

		public override void SetOrbit()
		{
			if (Orbit == null)
			{
				return;
			}

			List<Vector3D> flightPathPositions =
				base.Orbit.GetFlightPathPositions(World, NumberOfOrbitPositions, 60.0, out var parentChanged);
			MyOrbitRenderer.positionCount = flightPathPositions.Count;
			if (flightPathPositions.Count > 0)
			{
				for (int i = 0; i < flightPathPositions.Count; i++)
				{
					MyOrbitRenderer.SetPosition(i, flightPathPositions[i].ToVector3());
				}
			}

			MyOrbitRenderer.transform.SetParent(Orbits);
			MyOrbitRenderer.startColor = OrbitColor;
			MyOrbitRenderer.endColor = OrbitColor;
			bool loop = Orbit.Eccentricity < 1.0 && Orbit.Eccentricity > 0.0 && !parentChanged;

			MyOrbitRenderer.loop = loop;
			if (Map != null && Map.MyShip == this)
			{
				if (Orbit.PeriapsisDistance < Orbit.Parent.Radius)
				{
					Map.NavPanel.ShowWarning(Localization.GetLocalizedField("UnstableOrbit").ToUpper());
				}
				else
				{
					Map.NavPanel.HideWarning();
				}
			}
		}

		public void SetIcon()
		{
			if (Icon != null)
			{
				IconRenderer.material.SetTexture("_MainTex", Icon.texture);
				float x = Icon.rect.width / Icon.texture.width;
				float y = Icon.rect.height / Icon.texture.height;
				float x2 = 1f - (Icon.rect.x + Icon.rect.width) / Icon.texture.width;
				float y2 = 1f - (Icon.rect.y + Icon.rect.height) / Icon.texture.height;
				IconRenderer.material.SetTextureScale("_MainTex", new Vector2(x, y));
				IconRenderer.material.SetTextureOffset("_MainTex", new Vector2(x2, y2));
			}
		}

		public void ToggleCone(bool val)
		{
			if (ScanningCone != null)
			{
				_scanningConeActive = val;
				Pitch.Activate(val);
				Yaw.Activate(val);
			}
		}
	}
}
