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
			Transform cameraTransform = MyPlayer.Instance.FpsController.MainCamera.transform;
			return Vector3.Angle(Position - cameraTransform.position, cameraTransform.forward);
		}
	}
}
