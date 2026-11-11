using UnityEngine;
using OpenHellion;
using OpenHellion.Map;
using ZeroGravity;
using ZeroGravity.Math;
using ZeroGravity.Objects;
using ZeroGravity.UI;

public class TargetObject
{
	public long Guid;

	public ArtificialBody ArtificialBody;

	public MapItemData KnownVessel;

	public string Name
	{
		get
		{
			if (ArtificialBody == null)
			{
				return KnownVessel.Name;
			}

			if (ArtificialBody is Asteroid)
			{
				return (ArtificialBody as Asteroid).Name;
			}

			if (ArtificialBody is Pivot)
			{
				return (ArtificialBody as Pivot).GetComponentInChildren<OtherPlayer>().PlayerName;
			}

			if (ArtificialBody is Ship)
			{
				return (ArtificialBody as Ship).CommandVesselName;
			}

			return "Unknown";
		}
	}

	public Sprite Icon
	{
		get
		{
			if (ArtificialBody == null)
			{
				return SpriteManager.Instance.GetSprite(KnownVessel.Type);
			}

			if (ArtificialBody is Asteroid)
			{
				return SpriteManager.Instance.GetSprite((ArtificialBody as Asteroid).Type);
			}

			if (ArtificialBody is Pivot)
			{
				return SpriteManager.Instance.GetSprite((ArtificialBody as Pivot).Type);
			}

			if (ArtificialBody is SpaceObjectVessel)
			{
				return SpriteManager.Instance.GetSprite(ArtificialBody as SpaceObjectVessel, true);
			}

			return SpriteManager.Instance.GetSprite(ArtificialBody.Type);
		}
	}

	public Vector3 Position => ArtificialBody != null
		? ArtificialBody.transform.position
		: (KnownVessel.Position - MyPlayer.Instance.World.OriginWorldPosition).ToVector3();

	public float Distance => (MyPlayer.Instance.Parent.transform.position - Position).magnitude;

	public float AngleFromCameraForward
	{
		get
		{
			Vector3 to = MyPlayer.Instance.transform.rotation * MyPlayer.Instance.FpsController.MainCamera.transform.forward;
			return Vector3.Angle(
				(Position - MyPlayer.Instance.transform.rotation * MyPlayer.Instance.FpsController.MainCamera.transform.position).normalized, to);
		}
	}
}
