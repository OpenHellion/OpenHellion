using System;
using System.Collections.Generic;
using System.Linq;
using OpenHellion;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;
using ZeroGravity.Math;
using ZeroGravity.Objects;

namespace ZeroGravity.UI
{
	public class RadarUI : MonoBehaviour
	{
		[HideInInspector] public bool CanRadarWork;

		[HideInInspector] public bool IsActive = true;

		public GameObject Root;

		public List<TargetObject> AllTargets = new List<TargetObject>();

		public TargetObject SelectedTarget;

		public Transform OverlayTargets;

		public TargetOverlayUI TargetOnScreen;

		public TargetObject HoveredTarget;

		public GameObject ParametersHolder;

		public Text TargetName;

		public Text TargetDistance;

		public Text TargetLateral;

		public Text TargetDirectional;

		public GameObject RadarCroshair;

		[FormerlySerializedAs("_worldState")] [SerializeField] private World _world;
		private Camera _uiCamera;

		public int CurrentRadarRange => (int)(10000f * (MyPlayer.Instance.CurrentHelmet == null
			? 1f
			: MyPlayer.Instance.CurrentHelmet.TierMultiplier));

		private void Awake()
		{
			Canvas canvas = OverlayTargets.GetComponentInParent<Canvas>(includeInactive: true).rootCanvas;
			_uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
		}

		private void Start()
		{
			IsActive = true;
		}

		private void Update()
		{
			if (!CanRadarWork && gameObject.activeInHierarchy)
			{
				SetTarget();
				gameObject.SetActive(value: false);
				ToggleTargeting(val: false);
				return;
			}

			if (CanRadarWork && !gameObject.activeInHierarchy)
			{
				ToggleTargeting(val: true);
				gameObject.SetActive(value: true);
			}

			UpdateRadarList();
			if (AllTargets.Count <= 0)
			{
				SetTarget();
			}
			else if (AllTargets.Count > 0 && SelectedTarget == null)
			{
				SetTarget(AllTargets[0]);
			}

			if (AllTargets.Count > 0)
			{
				UpdateOverlayTargets();
				CalculateScaleAndRotation();
			}

			if (HoveredTarget != null && Mouse.current.leftButton.wasPressedThisFrame)
			{
				int index = AllTargets.IndexOf(HoveredTarget);
				SetTarget(AllTargets[index]);
			}

			if (Mouse.current.scroll.y.ReadValue().IsNotEpsilonZero() && AllTargets.Count > 0)
			{
				float axis = Mouse.current.scroll.y.ReadValue();
				int num = AllTargets.IndexOf(SelectedTarget);
				if (axis > 0f)
				{
					num = ((num - 1 < 0) ? (AllTargets.Count - 1) : (num - 1));
				}
				else if (axis < 0f)
				{
					num = ((AllTargets.Count - 1 > num) ? (num + 1) : 0);
				}

				SetTarget(AllTargets[num]);
			}

			RadarCroshair.Activate(!_world.InGameGUI.IsCroshairActive);
		}

		public void UpdateRadarList()
		{
			Vector3 myPos = MyPlayer.Instance.Parent.transform.position + MyPlayer.Instance.transform.position;
			List<TargetObject> list2 = AllTargets.Where((TargetObject m) =>
				m.ArtificialBody == null || m.ArtificialBody is not SpaceObjectVessel ||
				!(m.ArtificialBody as SpaceObjectVessel).IsMainVessel || m.Distance > CurrentRadarRange).ToList();
			foreach (TargetObject item in list2)
			{
				AllTargets.Remove(item);
			}

			TargetOverlayUI[] componentsInChildren =
				OverlayTargets.GetComponentsInChildren<TargetOverlayUI>(includeInactive: true);
			foreach (TargetOverlayUI targetOverlayUI in componentsInChildren)
			{
				if (list2.Contains(targetOverlayUI.Target))
				{
					Destroy(targetOverlayUI.gameObject);
				}
			}

			List<ArtificialBody> list = AllTargets.Select((TargetObject m) => m.ArtificialBody).ToList();
			foreach (ArtificialBody item2 in _world.AllArtificialBodies.Where((ArtificialBody m) =>
				         m is SpaceObjectVessel &&
				         (m as SpaceObjectVessel).IsMainVessel && !(m as SpaceObjectVessel).IsWarpOnline &&
				         (myPos - m.transform.position).magnitude <= CurrentRadarRange && !list.Contains(m)))
			{
				TargetObject targetObject = new TargetObject
				{
					Guid = item2.Guid,
					ArtificialBody = item2
				};
				AllTargets.Add(targetObject);
				CreateOverlayTargets(targetObject);
			}

			if (!AllTargets.Contains(SelectedTarget))
			{
				SetTarget(AllTargets.FirstOrDefault());
			}
		}

		public void CreateOverlayTargets(TargetObject target)
		{
			TargetOverlayUI targetOverlayUI = Instantiate(TargetOnScreen, OverlayTargets);
			targetOverlayUI.Target = target;
			targetOverlayUI.Name.text = target.Name;
		}

		public void UpdateOverlayTargets()
		{
			TargetOverlayUI[] componentsInChildren =
				OverlayTargets.GetComponentsInChildren<TargetOverlayUI>(includeInactive: true);
			foreach (TargetOverlayUI targetOverlayUI in componentsInChildren)
			{
				if (targetOverlayUI.Name.text != targetOverlayUI.Target.Name)
				{
					targetOverlayUI.Name.text = targetOverlayUI.Target.Name;
				}

				Vector3 pos = targetOverlayUI.Target.ArtificialBody.transform.position;
				Vector2 localPoint = Vector2.zero;
				Vector3 arrowUp;
				Vector3 positionOnOverlay = GetPositionOnOverlay(pos, out arrowUp);
				RectTransformUtility.ScreenPointToLocalPointInRectangle(OverlayTargets.GetComponent<RectTransform>(),
					positionOnOverlay, _uiCamera, out localPoint);
				targetOverlayUI.transform.localPosition = localPoint;
				targetOverlayUI.transform.rotation = Quaternion.identity;
				if (!targetOverlayUI.gameObject.activeInHierarchy)
				{
					targetOverlayUI.gameObject.SetActive(value: true);
				}

				if (arrowUp != Vector3.zero)
				{
					targetOverlayUI.Distance.gameObject.Activate(value: false);
					targetOverlayUI.OffScreenTarget.gameObject.Activate(value: true);
					targetOverlayUI.OffScreenTarget.transform.up = arrowUp;
				}
				else
				{
					targetOverlayUI.Distance.gameObject.Activate(value: true);
					targetOverlayUI.OffScreenTarget.gameObject.Activate(value: false);
				}

				HoveredTarget = AllTargets.OrderBy((TargetObject m) => m.AngleFromCameraForward)
					.FirstOrDefault((TargetObject m) => m.AngleFromCameraForward < 3f);
				if (HoveredTarget != targetOverlayUI.Target && targetOverlayUI.Hovered.activeInHierarchy)
				{
					targetOverlayUI.Hovered.SetActive(value: false);
				}

				if (HoveredTarget != null && HoveredTarget == targetOverlayUI.Target &&
				    !targetOverlayUI.Hovered.activeInHierarchy)
				{
					targetOverlayUI.Hovered.SetActive(value: true);
				}

				if (SelectedTarget != targetOverlayUI.Target && targetOverlayUI.Selected.activeInHierarchy)
				{
					targetOverlayUI.Default.SetActive(value: true);
					targetOverlayUI.Selected.SetActive(value: false);
					targetOverlayUI.Distance.color = Colors.GrayDefault;
				}

				if (SelectedTarget != null && SelectedTarget == targetOverlayUI.Target &&
				    !targetOverlayUI.Selected.activeInHierarchy)
				{
					targetOverlayUI.Default.SetActive(value: false);
					targetOverlayUI.Selected.SetActive(value: true);
					targetOverlayUI.Distance.color = Colors.White;
				}

				if ((targetOverlayUI.Target == SelectedTarget || targetOverlayUI.Target == HoveredTarget) &&
				    !targetOverlayUI.NameHolder.activeInHierarchy)
				{
					targetOverlayUI.NameHolder.SetActive(value: true);
				}

				if (targetOverlayUI.Target != SelectedTarget && targetOverlayUI.Target != HoveredTarget &&
				    targetOverlayUI.NameHolder.activeInHierarchy)
				{
					targetOverlayUI.NameHolder.SetActive(value: false);
				}

				targetOverlayUI.Distance.text = FormatHelper.DistanceFormat(
					(targetOverlayUI.Target.ArtificialBody.transform.position - MyPlayer.Instance.transform.position).magnitude);
			}
		}

		private static Vector3 GetPositionOnOverlay(Vector3 pos, out Vector3 arrowUp)
		{
			arrowUp = Vector3.zero;
			Vector3 vector = MyPlayer.Instance.FpsController.MainCamera.WorldToScreenPoint(pos);
			if (vector.x < 0f || vector.x > Screen.width || vector.y < 0f || vector.y > Screen.height ||
			    vector.z < 0f)
			{
				Vector3 vector2 = new Vector3(Screen.width, Screen.height, 0f) / 2f;
				Vector3 vector3 = vector - vector2;
				if (vector3.z < 0f)
				{
					vector3 = Quaternion.Euler(0f, 0f, 180f) * vector3;
				}

				vector =
					(!(System.Math.Abs(vector3.x / Screen.width) >
					   System.Math.Abs(vector3.y / Screen.height)))
						? (vector3 / System.Math.Abs(vector3.y / (Screen.height / 2f)) + vector2)
						: (vector3 / System.Math.Abs(vector3.x / (Screen.width / 2f)) + vector2);
				arrowUp = (vector2 - vector).normalized;
				arrowUp.z = 0f;
			}

			vector.z = 0f;
			return vector;
		}

		private void CalculateScaleAndRotation()
		{
			if (SelectedTarget != null)
			{
				if (!ParametersHolder.activeInHierarchy)
				{
					ParametersHolder.SetActive(value: true);
				}

				Vector3 vector = SelectedTarget.ArtificialBody.transform.position - MyPlayer.Instance.transform.position;
				Vector3 vector2 =
					SelectedTarget.ArtificialBody.Velocity - MyPlayer.Instance.Parent.Velocity -
					MyPlayer.Instance.rigidBody.linearVelocity;
				Vector3 vector4 = Vector3.ProjectOnPlane(vector2, vector.normalized);
				float magnitude = vector.magnitude;
				float num = 0f - Vector3.Dot(vector2, vector.normalized);
				TargetDirectional.color = !(num >= 0f) ? Colors.FormatedRed : Colors.White;
				TargetDirectional.text = num.ToString("0.0") + " m/s";
				TargetLateral.text = vector4.magnitude.ToString("0.0") + " m/s";
				TargetDistance.color = !(magnitude <= 150f) ? Colors.White : Colors.FormatedRed;
				TargetDistance.text = FormatHelper.DistanceFormat(magnitude);
			}
		}

		private void SetTarget(TargetObject target = null)
		{
			if (target != null && target.ArtificialBody != null)
			{
				SelectedTarget = target;
				TargetName.text = SelectedTarget.Name;
				if (!ParametersHolder.activeInHierarchy)
				{
					ParametersHolder.SetActive(value: true);
				}

				if (IsActive)
				{
					ToggleStarDust(value: true);
				}
			}
			else
			{
				ParametersHolder.Activate(value: false);
				ToggleStarDust(value: false);
			}
		}

		private void ToggleStarDust(bool value)
		{
			if (value && SelectedTarget != null)
			{
				if (AllTargets.Count > 0)
				{
					ParticleSystem.MainModule main = MyPlayer.Instance.FpsController.StarDustParticle.main;
					main.customSimulationSpace = SelectedTarget.ArtificialBody.transform;
					MyPlayer.Instance.FpsController.StarDustParticle.gameObject.SetActive(value: true);
					MyPlayer.Instance.FpsController.StarDustParticle.Play();
				}
			}
			else
			{
				MyPlayer.Instance.FpsController.StarDustParticle.gameObject.SetActive(value: false);
				MyPlayer.Instance.FpsController.StarDustParticle.Stop();
			}
		}

		public void ToggleTargeting(bool val)
		{
			IsActive = val;
			Root.SetActive(IsActive);
			ToggleStarDust(IsActive);
		}
	}
}
