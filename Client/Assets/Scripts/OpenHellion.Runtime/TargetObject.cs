using UnityEngine;
using ZeroGravity.Objects;

public class TargetObject
{
	public long Guid;

	public Vector3 Position;

	public Vector3 Velocity;

	public string Name;

	public Sprite Icon;

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
