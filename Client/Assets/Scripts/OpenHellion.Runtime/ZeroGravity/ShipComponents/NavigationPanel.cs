using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using OpenHellion;
using OpenHellion.Map;
using UnityEngine;
using UnityEngine.UI;
using ZeroGravity.Data;
using ZeroGravity.LevelDesign;
using ZeroGravity.Network;
using ZeroGravity.Objects;
using ZeroGravity.UI;
using OpenHellion.Net;
using OpenHellion.Net.Message;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace ZeroGravity.ShipComponents
{
	public class NavigationPanel : MonoBehaviour
	{
		public struct POINT
		{
			public int x;

			public int y;

			public static implicit operator Point(POINT point)
			{
				return new Point(point.x, point.y);
			}
		}

		[Title("Navigation")] public Map Map;

		public Ship ParentVessel;

		public GameObject RightHolder;

		public Text NameOfSelectedObjectText;

		public UnityEngine.UI.Image SelectedMapObjectIcon;

		public Text SelectedMapObjectDescription;

		public GameObject HoverObjectUi;

		public Animator HoverObjectUiAnimator;

		public Text HoverObjectName;

		public Text GroupObject;

		public GameObject RadiationInfo;

		public Text DegradationRateValue;

		public Text SignatureValue;

		[Title("Orbit parameters")] public GameObject OrbitParametersHolder;

		public InputField Inclination;

		public InputField ArgumentOfPeriapsis;

		public InputField LongitudeOfAscendingNode;

		public InputField PeriapsisHeight;

		public InputField ApoapsisHeight;

		public InputField PositionOnOrbit;

		public Text OrbitalPeriod;

		[Title("Buttons")] public Button HomeStationButton;

		public Button WarpToButton;

		public Button AddCustomOrbitButton;

		public Button RemoveCustomOrbitButton;

		public Button WarpButton;

		[Title("Actions")] public GameObject CurrentItemScreen;

		public GameObject CurrentItemActive;

		public GameObject AuthorizedVesselsScreen;

		public GameObject AuthorizedVesselsActive;

		public Transform AuthorizedItemsHolder;

		public GameObject DistressSignalsScreen;

		public GameObject DistressSignalsActive;

		public Transform DistressItemsHolder;

		public GameObject GroupItemsScreen;

		public GameObject GroupItemsActive;

		public Transform GroupItemsHolder;

		public MapObjectUI MapObjectPrefab;

		public GameObject WarpScreen;

		public GameObject WarpScreenActive;

		public GameObject ScanScreen;

		public GameObject ScanScreenActive;

		public GameObject Warnings;

		public Text WarningText;

		[Title("Lists values")] public Text GroupObjectsValue;

		public Text DistressSignalsValue;

		public Text AuthorizedVesselsValue;

		[Title("FTL")] public GameObject ManeuverDetails;

		public GameObject ManeuverInProgress;

		public GameObject AlignShipInfo;

		public GameObject NoManeuverSelected;

		public UnityEngine.UI.Image PowerRequired;

		public Text PowerRequiredValue;

		public Text DistanceValue;

		public Text DockedVessels;

		public int SelectedWarpStage;

		public Transform WarpStageHolder;

		public WarpStageUI WarpStage;

		public Transform WarpCellsHolder;

		public List<WarpCellUI> AllWarpCells = new List<WarpCellUI>();

		public WarpCellUI WarpCell;

		public bool StartTimeEdit;

		public bool EndTimeEdit;

		public Text WarpStartTime;

		public Text WarpEndTime;

		public Text WarpStatus;

		public Text ManeuverTimeLeft;

		public GameObject TimerStatus;

		public GameObject DockedStructureStatus;

		public GameObject WarpCellStatus;

		public GameObject PowerStatus;

		public GameObject VesselBaseSystemsStatus;

		public UnityEngine.UI.Image MassFiller;

		public Text MassValue;

		public Text DockedVesselsMass;

		[Title("Warp Summary")] public Text WarpConsumptionSummary;

		public Text PowerConsumptionSummary;

		[Title("Scan")] public GameObject SelectedDetails;

		public Text ActiveSens;

		public Text PassiveSens;

		public Text WarpSens;

		public Slider ScaningSlider;

		public Text ScaningAngle;

		public Text ScanPowerConsumption;

		private float _scanAngle = 30f;

		public float PitchAngle;

		public float YawAngle;

		public UnityEngine.UI.Image ScanPowerFiller;

		public Text ScanPowerValue;

		public Text ScanStatus;

		public Button ScanButton;

		public Text SignalAmplificationValue;

		[Title("Pitch & Yaw")] public Text PitchValue;

		public Slider PitchSlider;

		public Text YawValue;

		public Slider YawSlider;

		public UnityEngine.UI.Image CapacitorFiller;

		[FormerlySerializedAs("_worldState")] [SerializeField] private World _world;

		public bool InputFocused
		{
			get
			{
				if (Inclination.isFocused || ArgumentOfPeriapsis.isFocused || LongitudeOfAscendingNode.isFocused ||
				    PeriapsisHeight.isFocused || ApoapsisHeight.isFocused || PositionOnOrbit.isFocused)
				{
					return true;
				}

				return false;
			}
		}

		private float CurrentCapacity
		{
			get
			{
				if (ParentVessel != null && ParentVessel.Capacitor != null)
				{
					return ParentVessel.Capacitor.Capacity;
				}

				return 0f;
			}
		}

		private float MaximumCapacity
		{
			get
			{
				if (ParentVessel != null && ParentVessel.Capacitor != null)
				{
					return ParentVessel.Capacitor.MaxCapacity;
				}

				return 0f;
			}
		}

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
		[DllImport("user32.dll")]
		private static extern bool SetCursorPos(int x, int y);

		[DllImport("user32.dll")]
		private static extern bool GetCursorPos(out POINT lpPoint);
#endif

		private void Start()
		{
			if (HoverObjectUiAnimator == null)
			{
				HoverObjectUiAnimator = HoverObjectUi.GetComponent<Animator>();
			}

			ScaningSlider.onValueChanged.AddListener(ScaningAngleSlider);
			PitchSlider.onValueChanged.AddListener(ScaningConePitch);
			YawSlider.onValueChanged.AddListener(ScanningConeYaw);
		}

		public void ShowWarning(string warningText)
		{
			WarningText.text = warningText;
			Warnings.SetActive(value: true);
		}

		public void HideWarning()
		{
			Warnings.GetComponent<Animator>().SetTrigger("Stop");
		}

		public void OnInteract(Ship parent)
		{
			gameObject.Activate(value: true);
			if (parent != ParentVessel)
			{
				Map.RemoveManeuverCourse();
			}

			ParentVessel = parent;
			ParentVessel.NavPanel = this;
			Inclination.onEndEdit.AddListener(SetInclination);
			ArgumentOfPeriapsis.onEndEdit.AddListener(SetAop);
			PeriapsisHeight.onEndEdit.AddListener(SetPeriapsis);
			ApoapsisHeight.onEndEdit.AddListener(SetApoapsis);
			LongitudeOfAscendingNode.onEndEdit.AddListener(SetLoan);
			PositionOnOrbit.onEndEdit.AddListener(SetOrbitPosition);
			EventSystem.AddListener(typeof(AuthorizedVesselsResponse), AuthorizedVesselsResponseListener);
			NetworkController.SendAndForget(new AuthorizedVesselsRequest());
			WarpButton.gameObject.Activate(
				ParentVessel.SubSystems.Values.FirstOrDefault((SubSystem m) => m.Type == SubSystemType.FTL) != null);
		}

		public void OnDetach()
		{
			gameObject.SetActive(value: false);
			Inclination.onEndEdit.RemoveAllListeners();
			ArgumentOfPeriapsis.onEndEdit.RemoveAllListeners();
			PeriapsisHeight.onEndEdit.RemoveAllListeners();
			ApoapsisHeight.onEndEdit.RemoveAllListeners();
			LongitudeOfAscendingNode.onEndEdit.RemoveAllListeners();
			PositionOnOrbit.onEndEdit.RemoveAllListeners();
			EventSystem.RemoveListener(typeof(AuthorizedVesselsResponse), AuthorizedVesselsResponseListener);
		}

		private void Update()
		{
			if (Map == null)
			{
				return;
			}

			UpdateUIElements();
			if (Map.SelectedObject != null && (!(Map.SelectedObject is MapObjectCustomOrbit) || Map.Dragging))
			{
				UpdateParameters();
			}

			if (ManeuverDetails.activeInHierarchy)
			{
				UpdateManeuver();
			}
			else if (ParentVessel.EndWarpTime > _world.SolarSystem.CurrentTime && ParentVessel.IsWarpOnline &&
			         ParentVessel.FTLEngine.IsSwitchedOn())
			{
				ManeuverInProgress.SetActive(value: true);
				ManeuverTimeLeft.text = Localization.ETA + " " +
				                        FormatHelper.PeriodFormat(ParentVessel.EndWarpTime -
				                                                  _world.SolarSystem.CurrentTime);
				AlignShipInfo.SetActive(value: false);
				NoManeuverSelected.SetActive(value: false);
			}
			else if (ManeuverInProgress.activeInHierarchy && Map.WarpManeuver == null)
			{
				ManeuverInProgress.SetActive(value: false);
				ManeuverTimeLeft.text = string.Empty;
				NoManeuverSelected.SetActive(value: true);
			}

			if (Mouse.current.scroll.y.ReadValue().IsNotEpsilonZero() && (StartTimeEdit || EndTimeEdit) &&
			    Map.WarpManeuver != null)
			{
				float axis = Mouse.current.scroll.y.ReadValue();
				if (axis > 0f)
				{
					if (EndTimeEdit)
					{
						UpdateEndTime(10f);
					}
					else if (StartTimeEdit)
					{
						UpdateStartTime(10f);
					}
				}
				else if (axis < 0f)
				{
					if (EndTimeEdit)
					{
						UpdateEndTime(-10f);
					}
					else if (StartTimeEdit)
					{
						UpdateStartTime(-10f);
					}
				}
			}

			if (HoverObjectUi.activeInHierarchy)
			{
				HoverObjectUi.transform.position = Mouse.current.position.ReadValue();
			}
		}

		private void UpdateUIElements()
		{
			if (!HomeStationButton.interactable && Map.Home != null)
			{
				HomeStationButton.interactable = true;
			}
			else if (Map.Home == null)
			{
				HomeStationButton.interactable = false;
			}

			OrbitParametersHolder.Activate(Map.SelectedObject != null);
			RemoveCustomOrbitButton.gameObject.Activate(Map.SelectedObject is MapObjectCustomOrbit);
			if (ManeuverDetails.activeInHierarchy)
			{
				CapacitorFiller.fillAmount = CurrentCapacity / MaximumCapacity;
			}

			if (ScanScreenActive.activeInHierarchy)
			{
				UpdateScanDetails();
			}
		}

		public void ShowObjectInfo(MapObject obj)
		{
			HoverObjectName.text = obj.Name;
		}

		public void SetInclination(string value)
		{
			double.TryParse(value, out var result);
			result = CheckValue(0.0, 360.0, result);
			UpdateOrbit(1, result);
		}

		public void SetAop(string value)
		{
			double.TryParse(value, out var result);
			result = CheckValue(0.0, 360.0, result);
			UpdateOrbit(2, result);
		}

		public void SetPeriapsis(string value)
		{
			double.TryParse(value, out var result);
			result = CheckValue(0.0, Map.SelectedObject.Orbit.ApoapsisDistance / 1000.0, result);
			UpdateOrbit(3, result * 1000.0);
		}

		public void SetApoapsis(string value)
		{
			double.TryParse(value, out var result);
			result = CheckValue(Map.SelectedObject.Orbit.PeriapsisDistance / 1000.0,
				Map.SelectedObject.Orbit.Parent.GravityInfluenceRadius / 1000.0, result);
			UpdateOrbit(4, result * 1000.0);
		}

		public void SetLoan(string value)
		{
			double.TryParse(value, out var result);
			result = CheckValue(0.0, 360.0, result);
			UpdateOrbit(5, result);
		}

		public void SetOrbitPosition(string value)
		{
			double.TryParse(value, out var result);
			result = CheckValue(0.0, 360.0, result);
			UpdateOrbit(6, result);
		}

		private double CheckValue(double min, double max, double value)
		{
			if (value > max)
			{
				return max;
			}

			if (value < min)
			{
				return min;
			}

			return value;
		}

		private void UpdateOrbit(int arg, double value)
		{
			OrbitParameters orbit = Map.SelectedObject.Orbit;
			double inclination = ((arg != 1) ? orbit.Inclination : value);
			double argumentOfPeriapsis = ((arg != 2) ? orbit.ArgumentOfPeriapsis : value);
			double periapsisDistance = ((arg != 3) ? orbit.PeriapsisDistance : value);
			double apoapsisDistance = ((arg != 4) ? orbit.ApoapsisDistance : value);
			double longitudeOfAscendingNode = ((arg != 5) ? orbit.LongitudeOfAscendingNode : value);
			double trueAnomalyAngleDeg =
				((arg != 6) ? (orbit.TrueAnomalyAtZeroTime() * (180.0 / System.Math.PI)) : value);
			Map.SelectedObject.Orbit.InitFromPeriapisAndApoapsis(orbit.Parent, periapsisDistance, apoapsisDistance,
				inclination, argumentOfPeriapsis, longitudeOfAscendingNode, trueAnomalyAngleDeg, 0.0);
			Map.SelectedObject.Orbit.ResetOrbit(_world.SolarSystem.CurrentTime);
			Map.SelectedObject.SetOrbit();
			Map.SelectedObject.UpdateOrbitPlane();
			UpdateParameters();
		}

		public void UpdateParameters()
		{
			if (Map.SelectedObject != null)
			{
				OrbitParametersHolder.Activate(Map.SelectedObject.Orbit != null);
				if (Map.SelectedObject.Orbit != null)
				{
					Inclination.text =
						Map.SelectedObject.Orbit.Inclination.ToString("0.00", NumberFormatInfo.InvariantInfo);
					ArgumentOfPeriapsis.text =
						Map.SelectedObject.Orbit.ArgumentOfPeriapsis.ToString("0.00", NumberFormatInfo.InvariantInfo);
					LongitudeOfAscendingNode.text =
						Map.SelectedObject.Orbit.LongitudeOfAscendingNode.ToString("0.00",
							NumberFormatInfo.InvariantInfo);
					PeriapsisHeight.text =
						(Map.SelectedObject.Orbit.PeriapsisDistance / 1000.0).ToString("0.00",
							NumberFormatInfo.InvariantInfo);
					ApoapsisHeight.text =
						(Map.SelectedObject.Orbit.ApoapsisDistance / 1000.0).ToString("0.00",
							NumberFormatInfo.InvariantInfo);
					PositionOnOrbit.text =
						(Map.SelectedObject.Orbit.TrueAnomalyAtZeroTime() * (180.0 / System.Math.PI)).ToString("0.00",
							NumberFormatInfo.InvariantInfo);
					OrbitalPeriod.text = FormatHelper.PeriodFormat(Map.SelectedObject.Orbit.OrbitalPeriod);
					Inclination.interactable = Map.SelectedObject is MapObjectCustomOrbit;
					ArgumentOfPeriapsis.interactable = Map.SelectedObject is MapObjectCustomOrbit;
					PeriapsisHeight.interactable = Map.SelectedObject is MapObjectCustomOrbit;
					ApoapsisHeight.interactable = Map.SelectedObject is MapObjectCustomOrbit;
					LongitudeOfAscendingNode.interactable = Map.SelectedObject is MapObjectCustomOrbit;
					PositionOnOrbit.interactable = Map.SelectedObject is MapObjectCustomOrbit;
				}
			}
		}

		public void UpdateObjectData()
		{
			if (Map.SelectedObject != null)
			{
				UpdateSelectedObjectInfo();
				UpdateParameters();
				AddCustomOrbitButton.gameObject.SetActive(Map.SelectedObject is MapObjectCelestial);
				if (Map.SelectedObject == Map.MyShip)
				{
					WarpToButton.gameObject.Activate(value: false);
				}
				else if (!(Map.SelectedObject is MapObjectCelestial) && Map.SelectedObject.Orbit != null)
				{
					WarpToButton.gameObject.Activate(!ParentVessel.IsWarpOnline &&
					                                 ParentVessel.SubSystems.Values.FirstOrDefault((SubSystem m) =>
						                                 m.Type == SubSystemType.FTL) != null);
				}
				else
				{
					WarpToButton.gameObject.Activate(value: false);
				}

				RightHolder.Activate(value: true);
			}
			else
			{
				RightHolder.Activate(value: false);
				ActivateOther(-1);
			}
		}

		private void UpdateSelectedObjectInfo(bool ToScan = false)
		{
			string text = Map.SelectedObject.Name.ToUpper();
			Sprite icon = Map.SelectedObject.Icon;
			string text2 = Map.SelectedObject.Description;
			string empty = string.Empty;
			string text3 = ((!(Map.SelectedObject is MapObjectVessel))
				? "-"
				: (Map.SelectedObject as MapObjectVessel).CrossSection.ToString("0"));
			RadiationInfo.Activate(value: true);
			if (Map.SelectedObject is MapObjectCustomOrbit)
			{
				float vesselExposureDamage =
					_world.GetVesselExposureDamage((Map.SelectedObject as MapObjectCustomOrbit).Orbit.Position
						.Magnitude);
				empty = (vesselExposureDamage * SpaceObjectVessel.VesselDecayRateMultiplier).ToString("0.0");
			}
			else if (Map.SelectedObject is MapObjectFuzzyScan)
			{
				MapObjectFuzzyScan mapObjectFuzzyScan = Map.SelectedObject as MapObjectFuzzyScan;
				text3 = "-";
				text2 = Localization.PossibleContacts + ": " + mapObjectFuzzyScan.Vessels.Count;
				empty = "-";
				RadiationInfo.Activate(value: false);
			}
			else if (Map.SelectedObject is MapObjectFixedPosition)
			{
				empty = "-";
				RadiationInfo.Activate(value: false);
			}
			else
			{
				empty =
					(Map.SelectedObject.VesselExposureDamage * SpaceObjectVessel.VesselDecayRateMultiplier)
					.ToString("0.0");
			}

			if (ToScan)
			{
				SelectedDetails.Activate(value: true);
				SubSystemRadar radar = ParentVessel.RadarSystem;
				double noise = radar.GetNoise();
				ActiveSens.text = (radar.GetActiveRange(SubSystemRadar.ReferenceCrossSection, _scanAngle,
					SubSystemRadar.ActiveThreshold, noise) / 1000.0).ToString("0");
				PassiveSens.text = (radar.GetPassiveRange(SubSystemRadar.ReferenceEmission, noise) / 1000.0).ToString("0");
				WarpSens.text = (radar.GetPassiveRange(SubSystemRadar.WarpingReferenceEmission, noise) / 1000.0).ToString("0");
				ScanPowerConsumption.text = FormatHelper.FormatValue(
					ParentVessel.RadarSystem.GetPowerConsumption(true) * ParentVessel.RadarSystem.ActiveScanDuration);
				SignalAmplificationValue.text = ((!(ParentVessel.RadarSystem.SignalAmplification > 1f))
					? Localization.NoPart.ToUpper()
					: (FormatHelper.FormatValue(ParentVessel.RadarSystem.SignalAmplification) + "x"));
			}
			else
			{
				NameOfSelectedObjectText.GetComponentInChildren<Text>().text = text;
				SelectedMapObjectIcon.sprite = icon;
				SelectedMapObjectDescription.text = text2;
				DegradationRateValue.text = empty;
				SignatureValue.text = text3;
			}
		}

		public void ShowMeHome()
		{
			Map.FocusToHome();
			ActivateOther(0);
			UpdateObjectData();
		}

		public void ShowMeMyShip()
		{
			Map.FocusToParentVessel();
			ActivateOther(0);
			UpdateObjectData();
		}

		public void GroupItemsList()
		{
			if (Map.SelectedObject == null)
			{
				RightHolder.SetActive(value: false);
				ActivateOther(-1);
				return;
			}

			RightHolder.SetActive(value: true);
			int num = 0;
			GroupItemsHolder.DestroyAll<MapObjectUI>();
			foreach (MapObject item in Map.SelectedVesselsGroup)
			{
				MapObjectUI mapObjectUI = Instantiate(MapObjectPrefab, GroupItemsHolder);
				mapObjectUI.gameObject.SetActive(value: true);
				mapObjectUI.gameObject.transform.Reset();
				mapObjectUI.gameObject.transform.localScale = Vector3.one;
				mapObjectUI.MapObj = item;
				mapObjectUI.UpdateUI();
				num++;
			}

			GroupObjectsValue.text = num.ToString("0");
		}

		public void DistressSignals()
		{
			RightHolder.SetActive(value: true);
			int num = 0;
			DistressItemsHolder.DestroyAll<MapObjectUI>();
			foreach (MapObject item in Map.AllMapObjects.Values.Where((MapObject m) =>
				         m.RadarVisibilityType == RadarVisibilityType.Distress))
			{
				MapObjectUI mapObjectUI = Instantiate(MapObjectPrefab, DistressItemsHolder);
				mapObjectUI.gameObject.SetActive(value: true);
				mapObjectUI.gameObject.transform.Reset();
				mapObjectUI.gameObject.transform.localScale = Vector3.one;
				mapObjectUI.MapObj = item;
				mapObjectUI.UpdateUI();
				num++;
			}

			DistressSignalsValue.text = num.ToString("0");
		}

		private void AuthorizedVesselsResponseListener(NetworkData data)
		{
			AuthorizedVesselsResponse avr = data as AuthorizedVesselsResponse;
			AuthorizedItemsHolder.DestroyAll<MapObjectUI>();
			int num = 0;
			if (avr.GUIDs == null)
			{
				return;
			}

			foreach (MapObject value in Map.AllMapObjects.Values)
			{
				if (value.MainObject is not MapItemData item || !avr.GUIDs.Contains(item.Guid))
				{
					continue;
				}

				MapObjectUI mapObjectUI = Instantiate(MapObjectPrefab, AuthorizedItemsHolder);
				mapObjectUI.gameObject.SetActive(value: true);
				mapObjectUI.gameObject.transform.Reset();
				mapObjectUI.gameObject.transform.localScale = Vector3.one;
				mapObjectUI.MapObj = value;
				mapObjectUI.UpdateUI();
				num++;
			}

			AuthorizedVesselsValue.text = num.ToString("0");
			_world.InGameGUI.QuestIndicators.AddMarkersOnMap();
		}

		public void Scan()
		{
			if (Map.SelectedObject == null)
			{
				Map.SelectMapObject(Map.MyShip);
			}

			UpdateSelectedObjectInfo(ToScan: true);
			MapObjectShip mapObjectShip = Map.MyShip as MapObjectShip;
			if (Map.SelectedObject != null && Map.SelectedObject != mapObjectShip)
			{
				Quaternion quaternion = Quaternion.FromToRotation(Vector3.forward,
					(Map.SelectedObject.TruePosition - mapObjectShip.MainObject.Position).ToVector3());
				PitchAngle = quaternion.eulerAngles.x;
				YawAngle = quaternion.eulerAngles.y;
			}

			Map.FocusToParentVessel();
			mapObjectShip.ToggleCone(val: true);
			RightHolder.Activate(value: true);
			ScaningAngleSlider(_scanAngle);
			SetScanningConePitch(PitchAngle);
			ScanningConeYaw(YawAngle);
		}

		public void ScaningAngleSlider(float val)
		{
			ScaningAngle.text = val.ToString("0.00");
			ScaningSlider.value = val;
			_scanAngle = val;
			(Map.MyShip as MapObjectShip).ScanningConeAngle = _scanAngle;
			(Map.MyShip as MapObjectShip).UpdateObject();
		}

		private void ScaningConePitch(float val)
		{
			if (val == -1f)
			{
				int num = (int)PitchSlider.handleRect.position.x;
				SetScanningConePitch(359f);
				int num2 = (int)PitchSlider.handleRect.position.x;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
				GetCursorPos(out var lpPoint);
				SetCursorPos(lpPoint.x + (num2 - num), lpPoint.y);
#endif
			}
			else if (val == 360f)
			{
				int num3 = (int)PitchSlider.handleRect.position.x;
				SetScanningConePitch(0f);
				int num4 = (int)PitchSlider.handleRect.position.x;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
				GetCursorPos(out var lpPoint2);
				SetCursorPos(lpPoint2.x + (num4 - num3), lpPoint2.y);
#endif
			}
			else
			{
				SetScanningConePitch(val);
			}
		}

		public void SetScanningConePitch(float val)
		{
			PitchAngle = val;
			PitchSlider.value = val;
			MapObjectShip mapObjectShip = Map.SelectedObject as MapObjectShip;
			if (mapObjectShip?.ScanningCone?.activeInHierarchy == true)
			{
				mapObjectShip.ScanningCone.transform.localRotation = Quaternion.Euler(val, YawAngle, 0f);
				mapObjectShip.Pitch.transform.localRotation = Quaternion.Euler(val, 0f, 0f);
			}

			PitchValue.text = val.ToString("0.00");
		}

		private void ScanningConeYaw(float val)
		{
			if (val == -1f)
			{
				int num = (int)YawSlider.handleRect.position.x;
				SetScanningConeYaw(359f);
				int num2 = (int)YawSlider.handleRect.position.x;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
				GetCursorPos(out var lpPoint);
				SetCursorPos(lpPoint.x + (num2 - num), lpPoint.y);
#endif
			}
			else if (val == 360f)
			{
				int num3 = (int)YawSlider.handleRect.position.x;
				SetScanningConeYaw(0f);
				int num4 = (int)YawSlider.handleRect.position.x;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
				GetCursorPos(out var lpPoint2);
				SetCursorPos(lpPoint2.x + (num4 - num3), lpPoint2.y);
#endif
			}
			else
			{
				SetScanningConeYaw(val);
			}
		}

		public void SetScanningConeYaw(float val)
		{
			YawAngle = val;
			YawSlider.value = val;
			MapObjectShip mapObjectShip = Map.SelectedObject as MapObjectShip;
			if (mapObjectShip?.ScanningCone?.activeInHierarchy == true)
			{
				mapObjectShip.ScanningCone.transform.localRotation = Quaternion.Euler(PitchAngle, val, 0f);
				mapObjectShip.Yaw.transform.localRotation = Quaternion.Euler(0f, val, 0f);
			}

			YawValue.text = val.ToString("0.00");
		}

		public void DoScan()
		{
			MapObjectShip mos = Map.MyShip as MapObjectShip;
			if (ParentVessel.RadarSystem != null && (ParentVessel.RadarSystem.Status == SystemStatus.Offline ||
			                                         ParentVessel.RadarSystem.Status == SystemStatus.Cooldown))
			{
				ParentVessel.RadarSystem.ActiveScanPending = true;
				NetworkController.SendAndForget(new ScanForObjectsRequest
				{
					ScanDirection = mos.ScanningCone.transform.forward.ToArray(),
					ScanAngle = mos.ScanningConeAngle
				});
			}
		}

		private void UpdateScanDetails()
		{
			if (ParentVessel.RadarSystem.Status == SystemStatus.Offline &&
			    ParentVessel.VesselBaseSystem.Status == SystemStatus.Online)
			{
				ScanButton.interactable = true;
			}
			else
			{
				ScanButton.interactable = false;
			}

			ScanStatus.text = ParentVessel.RadarSystem.GetStatus(out var color);
			ScanStatus.color = color;
			ScanPowerFiller.fillAmount = CurrentCapacity / MaximumCapacity;
			ScanPowerValue.text = FormatHelper.CurrentMax(CurrentCapacity, MaximumCapacity);
		}

		public void ActivateOther(int oth)
		{
			CurrentItemScreen.SetActive(oth == 0);
			CurrentItemActive.SetActive(oth == 0);
			AuthorizedVesselsScreen.SetActive(oth == 1);
			AuthorizedVesselsActive.SetActive(oth == 1);
			DistressSignalsScreen.SetActive(oth == 2);
			DistressSignalsActive.SetActive(oth == 2);
			GroupItemsScreen.SetActive(oth == 3);
			GroupItemsActive.SetActive(oth == 3);
			WarpScreen.SetActive(oth == 4);
			WarpScreenActive.SetActive(oth == 4);
			ScanScreen.Activate(oth == 5);
			ScanScreenActive.Activate(oth == 5);
			switch (oth)
			{
				case 0:
					UpdateObjectData();
					break;
				case 1:
					RightHolder.Activate(value: true);
					break;
				case 2:
					DistressSignals();
					break;
				case 3:
					GroupItemsList();
					break;
				case 4:
					Warp();
					break;
				case 5:
					Scan();
					break;
			}
		}

		public void AddCustomOrbit()
		{
			Map.CreateCustomOrbit();
		}

		public void RemoveCustomOrbit()
		{
			Map.RemoveCustomOrbit();
			UpdateObjectData();
		}

		public void WarpTo()
		{
			Map.CreateManeuverCourse();
			ActivateOther(4);
		}

		public void Warp()
		{
			RightHolder.SetActive(value: true);
			if (Map.WarpManeuver != null)
			{
				NoManeuverSelected.SetActive(value: false);
				if (Map.WarpManeuver.Initialized && ParentVessel.FTLEngine.IsSwitchedOn())
				{
					ManeuverDetails.SetActive(value: false);
					ManeuverInProgress.SetActive(value: true);
					AlignShipInfo.SetActive(value: true);
					ManeuverTimeLeft.text = string.Empty;
				}
				else
				{
					AlignShipInfo.SetActive(value: false);
					ManeuverInProgress.SetActive(value: false);
					ManeuverDetails.SetActive(value: true);
					DockedVessels.text = ParentVessel.MainVessel.AllDockedVessels.Count.ToString();
					SetWarpStages();
					SetWarpCells();
					UpdateManeuver();
				}

				CalculateMass();
			}
			else
			{
				ManeuverDetails.SetActive(value: false);
				ManeuverInProgress.SetActive(value: false);
				NoManeuverSelected.SetActive(value: true);
				ManeuverTimeLeft.text = string.Empty;
			}
		}

		public void SetWarpStages()
		{
			WarpStageHolder.DestroyAll<WarpStageUI>();
			for (int i = 0; i <= ParentVessel.FTLEngine.MaxWarp; i++)
			{
				WarpStageUI warpStageUI = Instantiate(WarpStage, WarpStageHolder);
				warpStageUI.gameObject.SetActive(value: true);
				warpStageUI.gameObject.transform.Reset();
				warpStageUI.gameObject.transform.localScale = Vector3.one;
				warpStageUI.Value = i;
				warpStageUI.Panel = this;
			}

			if (SelectedWarpStage >= 0 && SelectedWarpStage <= ParentVessel.FTLEngine.MaxWarp)
			{
				SelectWarpStage(SelectedWarpStage);
			}
			else
			{
				SelectWarpStage(ParentVessel.FTLEngine.MaxWarp);
			}
		}

		public void SelectWarpStage(int i)
		{
			SelectedWarpStage = i;
			WarpStageUI[] componentsInChildren = WarpStageHolder.GetComponentsInChildren<WarpStageUI>();
			foreach (WarpStageUI warpStageUI in componentsInChildren)
			{
				warpStageUI.Selected.gameObject.SetActive(SelectedWarpStage == warpStageUI.Value);
			}

			PowerRequired.fillAmount =
				ParentVessel.FTLEngine.WarpsData[SelectedWarpStage].PowerConsumption / MaximumCapacity;
			PowerRequiredValue.text =
				FormatHelper.CurrentMax(ParentVessel.FTLEngine.WarpsData[SelectedWarpStage].PowerConsumption,
					MaximumCapacity);
			Map.WarpManeuver.Transfer.WarpIndex = SelectedWarpStage;
			PowerConsumptionSummary.text =
				FormatHelper.FormatValue(ParentVessel.FTLEngine.WarpsData[SelectedWarpStage].PowerConsumption);
			CalculateMass();
		}

		public void SetWarpCells()
		{
			float num = 0f;
			WarpCellsHolder.DestroyAll<WarpCellUI>();
			AllWarpCells.Clear();
			foreach (SceneMachineryPartSlot item in from m in ParentVessel.FTLEngine.MachineryPartSlots
			         where m.CanAttachItemType(ItemType.MachineryPart, null, MachineryPartType.WarpCell)
			         orderby m.SlotIndex
			         select m)
			{
				WarpCellUI warpCellUI = Instantiate(WarpCell, WarpCellsHolder);
				warpCellUI.gameObject.SetActive(value: true);
				warpCellUI.gameObject.transform.Reset();
				warpCellUI.gameObject.transform.localScale = Vector3.one;
				warpCellUI.Panel = this;
				warpCellUI.Slot = item;
				if (item.Item != null)
				{
					num += item.Item.Health;
					warpCellUI.IsSelected = true;
				}

				AllWarpCells.Add(warpCellUI);
				warpCellUI.UpdateUI();
			}
		}

		public void UpdateManeuver()
		{
			if (!(Map.WarpManeuver != null))
			{
				return;
			}

			float num = 0f;
			float num2 = Map.WarpManeuver.FuelAmountRequired;
			WarpConsumptionSummary.text = FormatHelper.FormatValue(num2);
			DistanceValue.text = (Map.WarpManeuver.ManeuverDistance / 1000.0).ToString("0.0") + " km";
			WarpStartTime.text = FormatHelper.PeriodFormat(Map.WarpManeuver.Transfer.StartEta);
			WarpEndTime.text = FormatHelper.PeriodFormat(Map.WarpManeuver.Transfer.EndEta);
			foreach (WarpCellUI item in AllWarpCells.Where((WarpCellUI m) => m.IsSelected && m.Slot.Item != null))
			{
				num += item.Slot.Item.Health;
				if (item.Slot.Item.Health > num2)
				{
					item.FillerRequired.fillAmount = num2 / item.Slot.Item.MaxHealth;
					num2 = 0f;
				}
				else
				{
					item.FillerRequired.fillAmount = item.Slot.Item.Health / item.Slot.Item.MaxHealth;
					num2 -= item.Slot.Item.Health;
				}
			}

			Map.WarpManeuver.Transfer.WarpCells = (from m in AllWarpCells
				where m.IsSelected
				select m.Slot.SlotIndex).ToList();
			Map.WarpManeuver.Transfer.WarpIndex = SelectedWarpStage;
			if (ParentVessel.VesselBaseSystem.IsSwitchedOn())
			{
				if (Map.WarpManeuver.FeasibilityError != 0)
				{
					WarpStatus.color = Colors.Red;
					WarpStatus.text = Map.WarpManeuver.FeasibilityError.ToLocalizedString().ToUpper();
				}
				else
				{
					WarpStatus.color = Colors.Green;
					WarpStatus.text = Localization.Ready.ToUpper();
				}
			}
			else
			{
				WarpStatus.color = Colors.Red;
				WarpStatus.text = Localization.VesselPowerOffline.ToUpper();
			}

			ManeuverChecklist();
		}

		public void ManeuverChecklist()
		{
			VesselBaseSystemsStatus.SetActive(!ParentVessel.VesselBaseSystem.IsSwitchedOn());
			if (Map.WarpManeuver.FeasibilityError == ManeuverCourse.FeasibilityErrorType.CourseImpossible)
			{
				TimerStatus.SetActive(value: true);
				PowerStatus.SetActive(value: true);
				WarpCellStatus.SetActive(value: true);
				DockedStructureStatus.SetActive(value: true);
			}
			else
			{
				TimerStatus.SetActive(Map.WarpManeuver.ManeuverError);
				PowerStatus.SetActive(Map.WarpManeuver.PowerError);
				WarpCellStatus.SetActive(Map.WarpManeuver.WarpCellError);
				DockedStructureStatus.SetActive(Map.WarpManeuver.DockingStructureError);
			}
		}

		public void InitializeManeuver()
		{
			if (Map.WarpManeuver.FeasibilityError == ManeuverCourse.FeasibilityErrorType.None &&
			    ParentVessel.VesselBaseSystem.IsSwitchedOn())
			{
				ParentVessel.FTLEngine.SwitchOn();
				Map.InitializeManeuverCourse();
				NoManeuverSelected.SetActive(value: false);
				ManeuverDetails.SetActive(value: false);
				ManeuverInProgress.SetActive(value: true);
				AlignShipInfo.SetActive(value: true);
				ManeuverTimeLeft.text = string.Empty;
			}
		}

		public void CancelManeuver()
		{
			Map.RemoveManeuverCourse();
			ManeuverInProgress.SetActive(value: false);
			ActivateOther(0);
			ParentVessel.CancelManeuver();
		}

		public void CancelWarp()
		{
			if (ManeuverInProgress.activeInHierarchy)
			{
				ParentVessel.FTLEngine.SwitchOff();
				ManeuverDetails.SetActive(value: true);
			}

			CancelManeuver();
		}

		public void UpdateStartTime(float time)
		{
			Map.WarpManeuver.AddStartTime(time);
		}

		public void UpdateEndTime(float time)
		{
			Map.WarpManeuver.AddEndTime(time);
		}

		public void ToggleFTL()
		{
			if (ParentVessel.FTLEngine.IsSwitchedOn())
			{
				ParentVessel.FTLEngine.SwitchOff();
			}
			else
			{
				ParentVessel.FTLEngine.SwitchOn();
			}
		}

		public void ShowHoverInfo(List<MapObject> mapObjects)
		{
			HoverObjectUi.SetActive(value: true);
			if (Keyboard.current.leftCtrlKey.isPressed && Map.SelectedObject != null)
			{
				HoverObjectName.text = Map.SelectedObject.Name.ToUpper() + " - " + mapObjects[0].Name.ToUpper() + "\n" +
				                       Localization.Distance + ": " +
				                       FormatHelper.DistanceFormat(
					                       (Map.SelectedObject.TruePosition - mapObjects[0].TruePosition).Magnitude);
				HoverObjectUiAnimator.SetBool("cluster", value: false);
				HoverObjectUiAnimator.SetBool("object", value: true);
				return;
			}

			if (mapObjects.Count > 1)
			{
				GroupObject.text = string.Empty;
				foreach (MapObject mapObject in mapObjects)
				{
					Text groupObject = GroupObject;
					groupObject.text = groupObject.text + mapObject.Name.ToUpper() + "\n";
				}

				HoverObjectUiAnimator.SetBool("object", value: false);
				HoverObjectUiAnimator.SetBool("cluster", value: true);
				return;
			}

			if (mapObjects[0] is MapObjectFuzzyScan)
			{
				MapObjectFuzzyScan mapObjectFuzzyScan = mapObjects[0] as MapObjectFuzzyScan;
				HoverObjectName.text = Localization.PossibleContacts + ": " + mapObjectFuzzyScan.Vessels.Count;
			}
			else
			{
				HoverObjectName.text = mapObjects[0].Name.ToUpper();
			}

			HoverObjectUiAnimator.SetBool("cluster", value: false);
			HoverObjectUiAnimator.SetBool("object", value: true);
		}

		public void HideHoverInfo()
		{
			HoverObjectUiAnimator.SetBool("object", value: false);
			HoverObjectUiAnimator.SetBool("cluster", value: false);
		}

		public void CalculateMass()
		{
			float num = ((SelectedWarpStage != 0) ? ParentVessel.FTLEngine.TowingCapacity : 0f);
			float num2 = (float)(ParentVessel.GetCompoundMass() - ParentVessel.Mass);
			if (num2 / num > 0.85f)
			{
				MassFiller.color = Colors.Red;
			}
			else
			{
				MassFiller.color = Colors.White;
			}

			if (num > 0f)
			{
				MassFiller.fillAmount = num2 / num;
				MassValue.text = FormatHelper.CurrentMax(num2, num);
			}
			else
			{
				MassFiller.fillAmount = 0f;
				MassValue.text = Localization.TowingDisabled.ToUpper();
				MassFiller.color = Colors.Red;
			}

			string text = string.Empty;
			foreach (SpaceObjectVessel allVessel in ParentVessel.AllVessels)
			{
				if (!(allVessel == ParentVessel))
				{
					string text2 = text;
					text = text2 + allVessel.Name + " - " + allVessel.Mass.ToString("0.0") + "\n";
				}
			}

			if (text == string.Empty)
			{
				DockedVesselsMass.text = Localization.NoDockedVessels.ToUpper();
			}
			else
			{
				DockedVesselsMass.text = text;
			}
		}
	}
}
