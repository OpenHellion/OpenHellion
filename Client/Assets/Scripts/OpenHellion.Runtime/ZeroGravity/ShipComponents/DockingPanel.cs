using System;
using System.Collections.Generic;
using System.Linq;
using OpenHellion;
using OpenHellion.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using ZeroGravity.Data;
using ZeroGravity.LevelDesign;
using ZeroGravity.Math;
using ZeroGravity.Objects;
using ZeroGravity.UI;
using Object = UnityEngine.Object;

namespace ZeroGravity.ShipComponents
{
	public class DockingPanel : MonoBehaviour
	{
		private class DockingPortUI
		{
			public SceneDockingPort Port;

			public DockingPanelUIItem UI;
		}

		private readonly UpdateTimer _timer = new UpdateTimer(0.35f);

		public float ThrustModifier = 0.2f;

		private Ship _parentShip;

		public List<Ship> AvailableTargetShips = new List<Ship>();

		public RectTransform AvailableDockingPortsTransform;

		public RectTransform TargetedModulePortsTranform;

		public DockingPanelUIItem ListItemPrefab;

		private readonly List<DockingPortUI> _myDockingPortsUI = new List<DockingPortUI>();

		private readonly List<DockingPortUI> _myVisibleDockingPortsUI = new List<DockingPortUI>();

		private List<SceneDockingPort> _dockingPorts = new List<SceneDockingPort>();

		private int _myCurrentPort;

		public SceneDockingPort TargetDockingPort;

		private readonly List<DockingPortUI> _targetedModulePorts = new List<DockingPortUI>();

		private int _currentTargetedModulePortIndex;

		private bool _changeTargetedModulePort;

		public GameObject HasTargetPanel;

		public Text DistanceValText;

		public GameObject DistanceGraph;

		public GameObject DirectionalGraph;

		public GameObject RCSPanel;

		public Image RcsFuelSlider;

		public Image FillerRcs;

		public Text RcsFuelText;

		public Text RCSFuelStatus;

		private bool _testStabilization;

		private Transform _mainCameraParent;

		private Vector3 _mainCameraPosition;

		private Quaternion _mainCameraRotation;

		private Vector3 _mainCameraScale;

		private const float RadarScanningRange = 3000f;

		public DockingPanelRadarTarget RadarTarget;

		public float timer = 2f;

		public float timerCounter;

		public bool timerPositive = true;

		private GameObject _currentRootBlinker;

		[SerializeField] private Text OnSpeedValText;

		[SerializeField] private Text OffSpeedValText;

		[SerializeField] private Image RollAngleIndicator;

		public Image RotationIndicator;

		[SerializeField] private Image lateralXE;

		[SerializeField] private Image lateralXW;

		[SerializeField] private Image lateralYN;

		[SerializeField] private Image lateralYS;

		[SerializeField] private Text ValForX;

		[SerializeField] private Text ValForY;

		public Text ControlsForChangingPort;

		public GameObject DockingTips;

		[Title("Target module")] public GameObject NoTargetModule;

		public GameObject AvailablePortsHolder;

		public Image TargetModuleIcon;

		public Text TargetModuleName;

		public SceneDockingPort DockingPort =>
			(_dockingPorts.Count - 1 >= _myCurrentPort) ? _dockingPorts[_myCurrentPort] : null;

		public bool IsDockingEnabled =>
			_parentShip != null && !_parentShip.DockingControlsDisabled && _myDockingPortsUI.Count > 0;

		private void Start()
		{
			RCSPanel.SetActive(_parentShip.RCS != null);
			ControlsForChangingPort.text = string.Format(Localization.ControlChangeDockingPort,
				GetControlName(ControlsSubsystem.ConfigAction.Equip)).ToUpper();
		}

		private void Update()
		{
			if ((MyPlayer.Instance.ShipControlMode != ShipControlMode.Docking &&
			     !(MyPlayer.Instance.LockedToTrigger is SceneTriggerDockingPanel)))
			{
				return;
			}

			if (_timer.Update())
			{
				ReloadRadarElements();
			}

			if (ControlsSubsystem.GetButtonDown(ControlsSubsystem.ConfigAction.Equip))
			{
				TargetDockingPort = null;
				_changeTargetedModulePort = true;
				SwitchPort();
				ReloadRadarElements();
				UpdateTargetedModule();
			}

			if (Mouse.current.scroll.y.ReadValue().IsNotEpsilonZero() && _targetedModulePorts.Count > 0)
			{
				_targetedModulePorts[_currentTargetedModulePortIndex].UI.IsSelected = false;
				_targetedModulePorts[_currentTargetedModulePortIndex].UI.Distance = string.Empty;
				float axis = Mouse.current.scroll.y.ReadValue();
				if (axis > 0f)
				{
					if (_currentTargetedModulePortIndex - 1 >= 0)
					{
						_currentTargetedModulePortIndex--;
					}
					else
					{
						_currentTargetedModulePortIndex = _targetedModulePorts.Count - 1;
					}
				}
				else if (axis < 0f)
				{
					if (_targetedModulePorts.Count - 1 >= _currentTargetedModulePortIndex + 1)
					{
						_currentTargetedModulePortIndex++;
					}
					else
					{
						_currentTargetedModulePortIndex = 0;
					}
				}

				ChangeTargetedPort();
			}

			bool buttonDown = ControlsSubsystem.GetButtonDown(ControlsSubsystem.ConfigAction.TargetUp);
			bool buttonDown2 = ControlsSubsystem.GetButtonDown(ControlsSubsystem.ConfigAction.TargetDown);
			if (_targetedModulePorts.Count > 0 && (buttonDown || buttonDown2))
			{
				_targetedModulePorts[_currentTargetedModulePortIndex].UI.IsSelected = false;
				_targetedModulePorts[_currentTargetedModulePortIndex].UI.Distance = string.Empty;
				if (buttonDown2)
				{
					if (_targetedModulePorts.Count - 1 >= _currentTargetedModulePortIndex + 1)
					{
						_currentTargetedModulePortIndex++;
					}
					else
					{
						_currentTargetedModulePortIndex = 0;
					}
				}
				else if (buttonDown)
				{
					if (_currentTargetedModulePortIndex - 1 >= 0)
					{
						_currentTargetedModulePortIndex--;
					}
					else
					{
						_currentTargetedModulePortIndex = _targetedModulePorts.Count - 1;
					}
				}

				ChangeTargetedPort();
			}

			SceneDockingPort dockingPort = DockingPort;
			if (TargetDockingPort != null && dockingPort != null)
			{
				Vector3 vector = TargetDockingPort.ParentShip.transform.position - dockingPort.ParentShip.transform.position;
				Vector3 vector2 =
					dockingPort.ParentShip.transform.rotation.Inverse() *
					dockingPort.ParentShip.Velocity - TargetDockingPort.ParentShip.Velocity;
				Vector3 vector3 =
					Vector3.Project(
						dockingPort.ParentShip.Velocity - TargetDockingPort.ParentShip.Velocity,
						dockingPort.CameraPosition.forward);
				Vector3 vector4 = dockingPort.CameraPosition.rotation.Inverse() *
				                  Vector3.ProjectOnPlane(vector2, dockingPort.CameraPosition.forward);
				ValForX.text = FormatHelper.DistanceFormat(vector4.x) + "/s";
				ValForY.text = FormatHelper.DistanceFormat(vector4.y) + "/s";
				Vector3 vector5 = vector4;
				if (vector5.x > 0f)
				{
					lateralXW.fillAmount = 0f;
					lateralXE.fillAmount = Mathf.Clamp(Mathf.Abs(vector5.x) / 3f, 0f, 1f);
				}
				else
				{
					lateralXE.fillAmount = 0f;
					lateralXW.fillAmount = Mathf.Clamp(Mathf.Abs(vector5.x) / 3f, 0f, 1f);
				}

				if (vector5.y < 0f)
				{
					lateralYN.fillAmount = 0f;
					lateralYS.fillAmount = Mathf.Clamp(Mathf.Abs(vector5.y) / 3f, 0f, 1f);
				}
				else
				{
					lateralYS.fillAmount = 0f;
					lateralYN.fillAmount = Mathf.Clamp(Mathf.Abs(vector5.y) / 3f, 0f, 1f);
				}

				Vector3 relativePosition = GetRelativePosition(TargetDockingPort.CameraPosition.position);
				float f = vector3.magnitude *
				          MathHelper.Sign(Vector3.Dot(vector.normalized, vector3.normalized));
				OnSpeedValText.text = f.ToString("f1");
				OffSpeedValText.text = vector4.magnitude.ToString("f1");
				DirectionalGraph.transform.localScale = new Vector3(Mathf.Clamp(Mathf.Abs(f) / 5f, 0.01f, 1f), 1f, 1f);
				if (Mathf.Abs(f) <= 1f)
				{
					DirectionalGraph.GetComponent<Image>().color = Colors.Green;
				}
				else if (Mathf.Abs(f) > 1f && Mathf.Abs(f) <= 5f)
				{
					DirectionalGraph.GetComponent<Image>().color = Colors.Orange;
				}
				else
				{
					DirectionalGraph.GetComponent<Image>().color = Colors.Red;
				}

				Quaternion quaternion = dockingPort.SceneDockingTriggerTrans.rotation * Quaternion
					.LookRotation(-TargetDockingPort.SceneDockingTriggerTrans.forward,
						TargetDockingPort.SceneDockingTriggerTrans.up).Inverse();
				float num = quaternion.eulerAngles.x;
				float num2 = quaternion.eulerAngles.y;
				float num3 = quaternion.eulerAngles.z;
				if (num2 > 180f)
				{
					num2 -= 360f;
				}

				if (num > 180f)
				{
					num -= 360f;
				}

				if (num3 > 180f)
				{
					num3 -= 360f;
				}

				float num4 = vector3.magnitude *
				             MathHelper.Sign(Vector3.Dot(vector.normalized, vector3.normalized));
				Vector3 vector6 = TargetDockingPort.SceneDockingTriggerTrans.position -
				                  dockingPort.SceneDockingTriggerTrans.position;
				float num5 = Vector3.SignedAngle(TargetDockingPort.SceneDockingTriggerTrans.up,
					Vector3.ProjectOnPlane(dockingPort.SceneDockingTriggerTrans.up,
						TargetDockingPort.SceneDockingTriggerTrans.forward),
					TargetDockingPort.SceneDockingTriggerTrans.forward);
				float num6 = MathHelper.AngleSigned(dockingPort.SceneDockingTriggerTrans.forward,
					-TargetDockingPort.SceneDockingTriggerTrans.forward, dockingPort.SceneDockingTriggerTrans.up);
				float num7 = Vector3.Distance(dockingPort.SceneDockingTriggerTrans.position,
					TargetDockingPort.SceneDockingTriggerTrans.position);
				if (!RotationIndicator.gameObject.activeInHierarchy)
				{
					RotationIndicator.gameObject.SetActive(value: true);
				}

				RollAngleIndicator.fillClockwise = num5 < 0f;
				RollAngleIndicator.fillAmount = Mathf.Abs(num5) / 360f;
				if (Mathf.Abs(num5) >= 5f)
				{
					RotationIndicator.color = Colors.Red;
					RollAngleIndicator.color = Colors.Red;
				}
				else
				{
					RotationIndicator.color = Colors.Green;
					RollAngleIndicator.color = Colors.Green;
				}

				DistanceValText.text = FormatHelper.DistanceFormat(num7);
				DistanceGraph.transform.localScale = new Vector3(Mathf.Clamp(Mathf.Abs(num7) / 50f, 0.01f, 1f), 1f, 1f);
				if (Mathf.Abs(num7) <= 5f)
				{
					DistanceGraph.GetComponent<Image>().color = Colors.Green;
				}
				else if (Mathf.Abs(num7) > 5f && Mathf.Abs(num7) <= 50f)
				{
					DistanceGraph.GetComponent<Image>().color = Colors.Orange;
				}
				else
				{
					DistanceGraph.GetComponent<Image>().color = Colors.Red;
				}

				if (Mathf.Abs(num) < 10f && Mathf.Abs(num2) < 10f && Mathf.Abs(num3) < 10f && num7 < 2f && num4 < 3f)
				{
					if (dockingPort.ParentShip.Engine != null && TargetDockingPort.ParentShip.Engine == null &&
					    !TargetDockingPort.ParentShip.IsDocked &&
					    TargetDockingPort.ParentShip.AllDockedVessels.Count == 0 && !dockingPort.ParentShip.IsDocked &&
					    dockingPort.ParentShip.AllDockedVessels.Count == 0)
					{
						TargetDockingPort.ToggleDockTo(dockingPort, toggle: true);
					}
					else
					{
						dockingPort.ToggleDockTo(TargetDockingPort, toggle: true);
					}

					TargetDockingPort = null;
				}

				if (MyPlayer.Instance.IsAdmin &&
				    (Keyboard.current.numpad9Key.isPressed || Keyboard.current.numpad8Key.isPressed))
				{
					if (dockingPort.ParentShip.Engine != null && TargetDockingPort.ParentShip.Engine == null &&
					    !TargetDockingPort.ParentShip.IsDocked &&
					    TargetDockingPort.ParentShip.AllDockedVessels.Count == 0 && !dockingPort.ParentShip.IsDocked &&
					    dockingPort.ParentShip.AllDockedVessels.Count == 0)
					{
						TargetDockingPort.ToggleDockTo(dockingPort, toggle: true);
					}
					else if (Keyboard.current.numpad9Key.isPressed)
					{
						dockingPort.ToggleDockTo(TargetDockingPort, toggle: true);
					}
					else
					{
						TargetDockingPort.ToggleDockTo(dockingPort, toggle: true);
					}

					TargetDockingPort = null;
				}

				RadarTarget.gameObject.Activate(value: true);
				RadarTarget.ParentTrans = null;
				RadarTarget.ParentTrans = dockingPort.CameraPosition;
				if (TargetDockingPort != null)
				{
					RadarTarget.TargetTrans = TargetDockingPort.CameraPosition;
				}
			}
			else
			{
				RadarTarget.gameObject.Activate(value: false);
				RotationIndicator.gameObject.Activate(value: false);
				RollAngleIndicator.fillAmount = 0f;
				lateralXE.fillAmount = 0f;
				lateralXW.fillAmount = 0f;
				lateralYN.fillAmount = 0f;
				lateralYS.fillAmount = 0f;
			}

			if (timerCounter > timer || timerCounter < 0f)
			{
				timerPositive = !timerPositive;
			}

			if (timerPositive)
			{
				timerCounter += Time.deltaTime;
			}
			else
			{
				timerCounter -= Time.deltaTime;
			}
		}

		private void ChangeTargetedPort()
		{
			_targetedModulePorts[_currentTargetedModulePortIndex].UI.IsSelected = true;
			_targetedModulePorts[_currentTargetedModulePortIndex].UI.Distance = Localization.Selected.ToUpper();
			if (_currentRootBlinker != null)
			{
				_currentRootBlinker.SetActive(value: false);
			}

			_currentRootBlinker = _targetedModulePorts[_currentTargetedModulePortIndex].Port.DockingVisualROOT;
			if (_currentRootBlinker != null)
			{
				_currentRootBlinker.SetActive(value: true);
			}

			TargetDockingPort = _targetedModulePorts[_currentTargetedModulePortIndex].Port;
			if (_targetedModulePorts.Count > 8)
			{
				float y = 34.5f * _currentTargetedModulePortIndex - 34.5f;
				TargetedModulePortsTranform.anchoredPosition = new Vector2(0f, y);
			}
			else if (_targetedModulePorts.Count == 0)
			{
				TargetedModulePortsTranform.anchoredPosition = new Vector2(0f, 0f);
			}

			UpdateTargetedModule();
		}

		private void UpdateTargetedModule()
		{
			if (TargetDockingPort != null)
			{
				NoTargetModule.Activate(value: false);
				TargetModuleIcon.sprite = SpriteManager.Instance.GetSprite(TargetDockingPort.ParentShip);
				TargetModuleName.text = TargetDockingPort.ParentShip.MainVessel.CustomName;
				AvailablePortsHolder.Activate(value: true);
			}
			else
			{
				NoTargetModule.Activate(value: true);
				AvailablePortsHolder.Activate(value: false);
			}
		}

		private Vector3 GetRelativePosition(Vector3 pos)
		{
			return (pos - MyPlayer.Instance.transform.position).normalized *
			       (transform.position - MyPlayer.Instance.transform.position).magnitude +
			       MyPlayer.Instance.transform.position;
		}

		public void OnInteract(Ship ship)
		{
			_parentShip = ship;
			_mainCameraPosition = MyPlayer.Instance.FpsController.MainCamera.transform.localPosition;
			_mainCameraRotation = MyPlayer.Instance.FpsController.MainCamera.transform.localRotation;
			_mainCameraScale = MyPlayer.Instance.FpsController.MainCamera.transform.localScale;
			_mainCameraParent = MyPlayer.Instance.FpsController.MainCamera.transform.parent;
			TargetDockingPort = null;
			AvailableTargetShips.Clear();
			foreach (DockingPortUI targetedModulePort in _targetedModulePorts)
			{
				Destroy(targetedModulePort.UI.gameObject);
			}

			_targetedModulePorts.Clear();
			RefreshDockingPorts();
			if (_myCurrentPort > _dockingPorts.Count - 1)
			{
				_myCurrentPort = 0;
			}

			SceneDockingPort dockingPort = DockingPort;
			if (dockingPort != null)
			{
				MyPlayer.Instance.FpsController.MainCamera.transform.parent = dockingPort.CameraPosition.parent;
				MyPlayer.Instance.FpsController.MainCamera.transform.position = dockingPort.CameraPosition.position;
				MyPlayer.Instance.FpsController.MainCamera.transform.rotation = dockingPort.CameraPosition.rotation;
				MyPlayer.Instance.FpsController.MainCamera.transform.localScale = dockingPort.CameraPosition.localScale;
			}

			_changeTargetedModulePort = true;
			MakeMyDockingPorts();
			ReloadRadarElements();
			DockingTips.Activate(Settings.SettingsData.GameSettings.ShowTips);
			MakeTargetModulePorts();
			gameObject.SetActive(value: true);
		}

		private void RefreshDockingPorts()
		{
			_dockingPorts.Clear();
			if (MyPlayer.Instance.Parent is SpaceObjectVessel &&
			    (MyPlayer.Instance.Parent as SpaceObjectVessel).SceneId == GameScenes.SceneId.AltCorp_Shuttle_CECA &&
			    MyPlayer.Instance.ShipControlMode == ShipControlMode.Docking)
			{
				foreach (SceneDockingPort value in _parentShip.MainVessel.DockingPorts.Values)
				{
					if (!value.LocalyDisabled && value.DockedToPort == null)
					{
						_dockingPorts.Add(value);
					}
				}

				foreach (SpaceObjectVessel allDockedVessel in _parentShip.MainVessel.AllDockedVessels)
				{
					foreach (SceneDockingPort value2 in allDockedVessel.DockingPorts.Values)
					{
						if (!value2.LocalyDisabled && value2.DockedToPort == null)
						{
							_dockingPorts.Add(value2);
						}
					}
				}
			}
			else
			{
				SceneDockingPort[] componentsInChildren =
					_parentShip.GeometryRoot.GetComponentsInChildren<SceneDockingPort>(includeInactive: true);
				foreach (SceneDockingPort sceneDockingPort in componentsInChildren)
				{
					if (!sceneDockingPort.LocalyDisabled)
					{
						_dockingPorts.Add(sceneDockingPort);
					}
				}
			}

			_dockingPorts = (from m in _dockingPorts
				where !m.Locked
				orderby m.ParentShip != _parentShip, m.ParentShip.Guid, m.Name
				select m).ToList();
		}

		public void OnDetach()
		{
			MyPlayer.Instance.FpsController.MainCamera.transform.parent = _mainCameraParent;
			MyPlayer.Instance.FpsController.MainCamera.transform.localPosition = _mainCameraPosition;
			MyPlayer.Instance.FpsController.MainCamera.transform.localRotation = _mainCameraRotation;
			MyPlayer.Instance.FpsController.MainCamera.transform.localScale = _mainCameraScale;
			if (_currentRootBlinker != null)
			{
				_currentRootBlinker.SetActive(value: false);
			}

			RadarTarget.gameObject.SetActive(value: false);
			SceneDockingPort[] componentsInChildren =
				_parentShip.GeometryRoot.GetComponentsInChildren<SceneDockingPort>(includeInactive: true);
			foreach (SceneDockingPort dockingPort in componentsInChildren)
			{
				_parentShip.World.CubemapRenderer.RemoveDockingPort(dockingPort);
			}

			gameObject.SetActive(value: false);
		}

		private void SwitchPort()
		{
			if (_myDockingPortsUI.Count > 1)
			{
				_myDockingPortsUI[_myCurrentPort].UI.IsSelected = false;
				_myDockingPortsUI[_myCurrentPort].UI.DistanceText.text = string.Empty;
				do
				{
					if (_myDockingPortsUI.Count - 1 >= _myCurrentPort + 1)
					{
						_myCurrentPort++;
					}
					else
					{
						_myCurrentPort = 0;
					}
				} while (_dockingPorts[_myCurrentPort].IgnoreThisDockingPort);

				_myDockingPortsUI[_myCurrentPort].UI.IsSelected = true;
				_myDockingPortsUI[_myCurrentPort].UI.DistanceText.text = Localization.Selected.ToUpper();
				MyPlayer.Instance.FpsController.MainCamera.transform.parent =
					_myDockingPortsUI[_myCurrentPort].Port.CameraPosition.parent;
				MyPlayer.Instance.FpsController.MainCamera.transform.position =
					_myDockingPortsUI[_myCurrentPort].Port.CameraPosition.position;
				MyPlayer.Instance.FpsController.MainCamera.transform.rotation =
					_myDockingPortsUI[_myCurrentPort].Port.CameraPosition.rotation;
				MyPlayer.Instance.FpsController.MainCamera.transform.localScale =
					_myDockingPortsUI[_myCurrentPort].Port.CameraPosition.localScale;
			}

			if (_myVisibleDockingPortsUI.Count > 8)
			{
				float y = 34.5f * _myVisibleDockingPortsUI.IndexOf(_myDockingPortsUI[_myCurrentPort]) - 34.5f;
				AvailableDockingPortsTransform.anchoredPosition = new Vector2(0f, y);
			}
			else if (_myVisibleDockingPortsUI.Count == 0)
			{
				AvailableDockingPortsTransform.anchoredPosition = new Vector2(0f, 0f);
			}
		}

		private void ReloadRadarElements()
		{
			List<Ship> list = new List<Ship>(AvailableTargetShips);
			foreach (ArtificialBody ab in _parentShip.World.AllArtificialBodies.Where((ArtificialBody m) =>
				         m is SpaceObjectVessel && (m as SpaceObjectVessel).MainVessel != _parentShip.MainVessel))
			{
				float num = (_parentShip.transform.position - ab.transform.position).magnitude;
				Ship ship = AvailableTargetShips.Find((Ship m) => m == ab as Ship);
				if (ab is Ship && num < RadarScanningRange)
				{
					if (ship == null)
					{
						AvailableTargetShips.Add(ab as Ship);
					}
					else
					{
						list.Remove(ship);
					}
				}
			}

			if (list.Count > 0)
			{
				foreach (Ship item in list)
				{
					AvailableTargetShips.Remove(item);
				}

				_changeTargetedModulePort = true;
			}

			if (_changeTargetedModulePort)
			{
				TargetDockingPort = null;
				MakeTargetModulePorts();
				_changeTargetedModulePort = false;
			}

			if (_parentShip != null && _parentShip.RCS != null)
			{
				float num2 = _parentShip.RCS.ResourceContainers[0].Quantity /
				             _parentShip.RCS.ResourceContainers[0].Capacity;
				RcsFuelSlider.fillAmount = num2;
				RcsFuelText.text = _parentShip.RCS.ResourceContainers[0].Quantity.ToString("f0") + " / " +
				                   _parentShip.RCS.ResourceContainers[0].Capacity.ToString("f0");
				if (num2 <= 0.2f)
				{
					FillerRcs.color = Colors.Red;
					RCSFuelStatus.gameObject.SetActive(value: true);
					if (num2 > 0.01f)
					{
						RCSFuelStatus.text = Localization.LowFuel.ToUpper();
					}
					else if (num2 <= 0.01f)
					{
						RCSFuelStatus.text = Localization.NoFuel.ToUpper();
					}
				}
				else
				{
					FillerRcs.color = Colors.White;
					RCSFuelStatus.gameObject.SetActive(value: false);
				}
			}

			UpdateTargetPortsUI();
		}

		private void MakeMyDockingPorts()
		{
			foreach (DockingPortUI item in _myDockingPortsUI)
			{
				item.UI.gameObject.SetActive(value: false);
				Destroy(item.UI.gameObject);
			}

			_myDockingPortsUI.Clear();
			_myVisibleDockingPortsUI.Clear();
			foreach (SceneDockingPort dockingPort in _dockingPorts)
			{
				DockingPortUI dockingPortUI = new DockingPortUI();
				dockingPortUI.Port = dockingPort;
				dockingPortUI.UI = MakeDockingPortUI(dockingPort);
				_myDockingPortsUI.Add(dockingPortUI);
				if (!dockingPort.IgnoreThisDockingPort)
				{
					_myVisibleDockingPortsUI.Add(dockingPortUI);
				}
			}

			if (_myCurrentPort > _dockingPorts.Count - 1)
			{
				_myCurrentPort = 0;
			}

			if (_myDockingPortsUI.Count > 0)
			{
				_myDockingPortsUI[_myCurrentPort].UI.IsSelected = true;
				_myDockingPortsUI[_myCurrentPort].UI.Distance = Localization.Selected.ToUpper();
			}
		}

		private DockingPanelUIItem MakeDockingPortUI(SceneDockingPort port)
		{
			DockingPanelUIItem dockingPanelUIItem = Instantiate(ListItemPrefab, AvailableDockingPortsTransform);
			dockingPanelUIItem.gameObject.SetActive(!port.IgnoreThisDockingPort && !port.Locked);
			dockingPanelUIItem.transform.localScale = Vector3.one;
			dockingPanelUIItem.gameObject.SetActive(!port.IgnoreThisDockingPort);
			dockingPanelUIItem.IsSelected = false;
			dockingPanelUIItem.NameText.text = ((!(port.ParentShip == _parentShip))
				? (GameScenes.GetShortVesselClassName(port.ParentShip.SceneId) + "-")
				: string.Empty) + port.Name;
			dockingPanelUIItem.DistanceText.text = string.Empty;
			return dockingPanelUIItem;
		}

		private void MakeTargetModulePorts()
		{
			if (_currentRootBlinker != null)
			{
				_currentRootBlinker.SetActive(value: false);
			}

			foreach (DockingPortUI targetedModulePort in _targetedModulePorts)
			{
				Destroy(targetedModulePort.UI.gameObject);
			}

			_targetedModulePorts.Clear();
			if (AvailableTargetShips.Count > 0)
			{
				foreach (Ship availableTargetShip in AvailableTargetShips)
				{
					foreach (SceneDockingPort item in availableTargetShip.DockingPorts.Values.Where(
						         (SceneDockingPort m) => !m.Locked))
					{
						if (item.DockedToPort == null && SceneHelper.CompareTags(item.DockType, DockingPort.DockType) &&
						    item.ParentShip.IsPlayerAuthorizedOrNoSecurity(MyPlayer.Instance))
						{
							DockingPortUI dockingPortUI = new DockingPortUI();
							dockingPortUI.Port = item;
							dockingPortUI.UI = MakeTargetModulePort(item);
							_targetedModulePorts.Add(dockingPortUI);
						}
					}
				}
			}

			_currentTargetedModulePortIndex = 0;
			UpdateTargetPortsUI();
		}

		private void UpdateTargetPortsUI()
		{
			if (_targetedModulePorts.Count > 0)
			{
				NoTargetModule.Activate(value: false);
				_targetedModulePorts[_currentTargetedModulePortIndex].UI.IsSelected = true;
				_targetedModulePorts[_currentTargetedModulePortIndex].UI.Distance = Localization.Selected.ToUpper();
				_currentRootBlinker = _targetedModulePorts[_currentTargetedModulePortIndex].Port.DockingVisualROOT;
				if (_currentRootBlinker != null)
				{
					_currentRootBlinker.SetActive(value: true);
				}

				TargetDockingPort = _targetedModulePorts[_currentTargetedModulePortIndex].Port;
				HasTargetPanel.Activate(value: true);
				UpdateTargetedModule();
			}
			else
			{
				HasTargetPanel.Activate(value: false);
				UpdateTargetedModule();
			}
		}

		private DockingPanelUIItem MakeTargetModulePort(SceneDockingPort port)
		{
			DockingPanelUIItem dockingPanelUIItem = Instantiate(ListItemPrefab, TargetedModulePortsTranform);
			dockingPanelUIItem.transform.localScale = Vector3.one;
			dockingPanelUIItem.gameObject.SetActive(value: true);
			dockingPanelUIItem.IsSelected = false;
			dockingPanelUIItem.NameText.text = ((!(port.ParentShip == _parentShip))
				? (GameScenes.GetShortVesselClassName(port.ParentShip.SceneId) + "-")
				: string.Empty) + port.Name;
			dockingPanelUIItem.DistanceText.text = string.Empty;
			return dockingPanelUIItem;
		}

		public string GetControlName(ControlsSubsystem.ConfigAction axName)
		{
			return ControlsSubsystem.GetAxisKeyName(axName);
		}

		public void UpdateDockingPorts()
		{
			RefreshDockingPorts();
			MakeMyDockingPorts();
			TargetDockingPort = null;
			_changeTargetedModulePort = true;
			SwitchPort();
			ReloadRadarElements();
			UpdateTargetedModule();
		}

		private void OnEnable()
		{
			_parentShip.World.InGameGUI.HelmetHud.gameObject.Activate(value: false);
		}

		private void OnDisable()
		{
			_parentShip.World.InGameGUI.HelmetHud.gameObject.Activate(value: true);
		}
	}
}
