using UnityEngine;
using UnityEngine.UI;
using ZeroGravity.Math;
using ZeroGravity.Objects;
using ZeroGravity.ShipComponents;

namespace ZeroGravity.UI
{
	public class RadarShipElement : MonoBehaviour
	{
		public TargetObject Target;


		public PilotOverlayUI PilotP;

		public Image Icon;

		public GameObject Selected;

		private readonly float _r1 = 80f;

		private readonly float _r2 = 130f;

		private readonly float _r3 = 130f;

		private void Update()
		{
			if (Target != null)
			{
				Vector3 vector = PilotP.ParentShip.transform.rotation.Inverse() *
				                 (Target.Position - PilotP.ParentShip.transform.position);
				Vector3 vec = Vector3.ProjectOnPlane(vector, Vector3.up);
				float f = MathHelper.AngleSigned(Vector3.forward, vec, Vector3.up);
				Vector3 vec2 = Vector3.ProjectOnPlane(vector, Vector3.right);
				float f2 = MathHelper.AngleSigned(Vector3.forward, vec2, Vector3.right);
				Vector3 vector2 = Vector3.ProjectOnPlane(vector, Vector3.forward);
				float f3 = Mathf.Atan2(vector2.x, vector2.y) + (float)System.Math.PI / 2f;
				f2 = Mathf.Abs(f2);
				f = Mathf.Abs(f);
				float num2 = Mathf.Max(f, f2);
				float num3 = MyPlayer.Instance.FpsController.MainCamera.fieldOfView / 2f;
				float num = (num2 < num3)
					? (_r1 * MathHelper.ProportionalValue(num2, 0f, num3, 0f, 1f))
					: ((!(num2 >= num3) || !(num2 < 90f))
						? (_r2 + (_r3 - _r2) * MathHelper.ProportionalValue(num2, 90f, 180f, 0f, 1f))
						: (_r1 + (_r2 - _r1) * MathHelper.ProportionalValue(num2, num3, 90f, 0f, 1f)));
				float y = Mathf.Sin(f3) * num;
				float x = (0f - Mathf.Cos(f3)) * num;
				transform.localPosition = new Vector2(x, y);
			}
		}
	}
}
