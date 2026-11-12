using OpenHellion.Map;
using UnityEngine;
using ZeroGravity.Objects;

namespace ZeroGravity.ShipComponents
{
	public abstract class MapObjectVessel : MapObject
	{
		protected override bool IsVisibleOnMap
		{
			get
			{
				return (RadarVisibilityType != 0 && RadarVisibilityType != RadarVisibilityType.Warp) ||
				       this == Map.MyShip || Map.AllObjectsVisible;
			}
		}

		public virtual double CrossSection
		{
			get { return (MainObject as MapItemData)?.CrossSection ?? 0.0; }
		}

		public override void UpdateVisibility()
		{
			base.UpdateVisibility();
			if (MainObject is not MapItemData)
			{
				return;
			}

			if (ObjectVisibilityBackground != null)
			{
				ObjectVisibilityBackground.material.color = Colors.RadarVisibility[RadarVisibilityType];
			}

			if (!gameObject.activeSelf || !(NewObjectVisibility != null) || NewObjectVisibility.activeSelf)
			{
				return;
			}

			long num = (MainObject as MapItemData).SpawnRuleId;

			if (Map.IsInitializing || (num != 0 && Map.KnownSpawnRuleIDs.Contains(num)))
			{
				Destroy(NewObjectVisibility);
			}
			else
			{
				NewObjectVisibility.Activate(true);
				if (NewObjectVisibility.TryGetComponent<Renderer>(out var component))
				{
					Color color = component.material.GetColor("_Tint");
					NewObjectColorFadeIncrement = color / NewObjectVisibilityDuration;
				}

				Destroy(NewObjectVisibility, NewObjectVisibilityDuration);
			}

			if (num != 0)
			{
				Map.KnownSpawnRuleIDs.Add(num);
			}
		}
	}
}
