using System.Collections.Generic;
using System.Linq;
using OpenHellion;
using OpenHellion.IO;
using UnityEngine;
using UnityEngine.UI;
using ZeroGravity.Data;
using ZeroGravity.LevelDesign;
using ZeroGravity.Objects;

namespace ZeroGravity.UI
{
	public class HelmetHudUI : MonoBehaviour
	{
		[Title("Main")] public GameObject HelmetRoot;

		public GameObject Active;

		public GameObject GeneralInfo;

		public GameObject JetpackInfo;

		public HelmetOverlayModel HelmetOverlayModel;

		[Title("Quick switch")] public GameObject QuickSlotsHolder;

		public GameObject PrimarySlot;

		public GameObject SecondarySlot;

		public GameObject GranadeSlot;

		public GameObject ConsumableSlot;

		[Title("General info")] public Text PressureValue;

		public Text HealthValue;

		public Image HeartBeat;

		public Animator Hb;

		public Image Armor;

		public Image HandsSlotIcon;

		public Sprite HandsSlotSprite;

		public GameObject ItemQuantity;

		public Image QuantityFiller;

		public Text ItemInfo;

		public GameObject WeaponInfo;

		public Image FireMod;

		[Title("Battery")] public GameObject BatteryMissing;

		public GameObject BatteryDetails;

		public Image PowerFiller;

		public GameObject LightsOn;

		[Title("Jetpack")] public GameObject JetpackHud;

		public GameObject NoJetpack;

		public GameObject JetpackDetails;

		public Text OxygenValue;

		public Image OxygenFiller;

		public Text RcsValue;

		public Image RCSFiller;

		public GameObject StabilityOn;

		public GameObject RCSOn;

		public GameObject RcsActive;

		public Text RCSDisabled;

		public Text ToggleTargetingInfo;

		public GameObject ZeroGravityTips;

		[Title("Warnings")] public GameObject WarningsHolder;

		public GameObject DebrisWarning;

		public GameObject OxygenWarning;

		public GameObject RcsWarning;

		public GameObject BatteryWarning;

		public GameObject RadiationWarning;

		public GameObject Breach;

		public GameObject Fire;

		public GameObject Gravity;

		[Title("Radar")] public RadarUI Radar;

		[Title("Targeting")] public GameObject TargetInfo;

		public Text TargetInfoName;

		private Camera _uiCamera;

		public Helmet CurrentHelmet => MyPlayer.Instance != null ? MyPlayer.Instance.CurrentHelmet : null;

		private float CurrentPressure
		{
			get
			{
				if (CurrentHelmet == null)
				{
					if (MyPlayer.Instance.CurrentRoomTrigger == null)
					{
						return 0f;
					}

					return MyPlayer.Instance.CurrentRoomTrigger.AirPressure;
				}

				return CurrentHelmet.Pressure;
			}
		}

		public bool ArmorOk
		{
			get
			{
				if (MyPlayer.Instance.CurrentOutfit != null)
				{
					if (MyPlayer.Instance.CurrentOutfit.Armor >
						MyPlayer.Instance.World.GetPlayerExposureDamage(
							MyPlayer.Instance.World.LocalToWorldPosition(MyPlayer.Instance.transform.position).Magnitude))
					{
						return true;
					}

					return false;
				}

				return false;
			}
		}

		public Item CurrentItem => MyPlayer.Instance.Inventory.ItemInHands;

		public bool UiActive => !MyPlayer.Instance.IsLockedToTrigger;

		private void Awake()
		{
			Canvas canvas = TargetInfo.GetComponentInParent<Canvas>(includeInactive: true).rootCanvas;
			_uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
		}

		private void Start()
		{
			ToggleTargetingInfo.text = string.Format(Localization.PressToToggleTargeting.ToUpper(),
				ControlsSubsystem.GetAxisKeyName(ControlsSubsystem.ConfigAction.HelmetRadar));
			UpdateQuickSlots();
		}

		private void Update()
		{
			if (Radar.CanRadarWork && Radar.AllTargets.Count > 0 &&
				ControlsSubsystem.GetButtonDown(ControlsSubsystem.ConfigAction.HelmetRadar))
			{
				Radar.ToggleTargeting(!Radar.IsActive);
			}

			if (MyPlayer.Instance.CurrentRoomTrigger == null &&
				(Breach.activeInHierarchy || Fire.activeInHierarchy || Gravity.activeInHierarchy))
			{
				WarningsUpdate();
			}

			UpdateUI();
		}

		public void UpdateUI()
		{
			if (CurrentHelmet == null)
			{
				Active.Activate(value: false);
				JetpackInfo.Activate(value: false);
				HelmetRoot.Activate(value: false);
				if (MyPlayer.Instance.FpsController.StarDustParticle != null && MyPlayer.Instance.FpsController
						.StarDustParticle.gameObject.activeInHierarchy)
				{
					TurnOffStardust();
				}

				return;
			}

			HelmetRoot.Activate(value: true);
			Active.Activate(CurrentHelmet.IsVisorActive && UiActive);
			if (HelmetOverlayModel.gameObject.activeSelf)
			{
				HelmetOverlayModel.Animator.SetBool("VisorRaise", !CurrentHelmet.IsVisorActive);
				HelmetOverlayModel.UpdateHelmetOverlay(CurrentHelmet.HelmetOverlay);
			}

			if (CurrentHelmet.Battery != null)
			{
				BatteryMissing.Activate(value: false);
				BatteryDetails.Activate(value: true);
				PowerFiller.fillAmount = CurrentHelmet.BatteryPower;
				if (CurrentHelmet.BatteryPower < 0.7f && CurrentHelmet.BatteryPower > 0.3f)
				{
					PowerFiller.color = Colors.YellowDark;
				}
				else if (CurrentHelmet.BatteryPower <= 0.3f)
				{
					PowerFiller.color = Colors.Red50;
				}
				else
				{
					PowerFiller.color = Colors.GreenNavColor;
				}

				BatteryWarning.Activate(CurrentHelmet.BatteryPower < 0.21f);
				if (CurrentHelmet.BatteryPower > 0f)
				{
					GeneralInfo.Activate(value: true);
					HandsUI();
					JetpackDetailsUI();
					WarningsHolder.Activate(BatteryWarning.activeSelf || Breach.activeSelf || Fire.activeSelf ||
											Gravity.activeSelf || DebrisWarning.activeSelf ||
											OxygenWarning.activeSelf || RcsWarning.activeSelf ||
											RadiationWarning.activeSelf);
				}
				else
				{
					GeneralInfo.Activate(value: false);
					JetpackInfo.Activate(value: false);
					WarningsHolder.Activate(value: false);
				}
			}
			else
			{
				GeneralInfo.Activate(value: false);
				JetpackInfo.Activate(value: false);
				BatteryDetails.Activate(value: false);
				WarningsHolder.Activate(value: false);
				BatteryMissing.Activate(value: true);
			}
		}

		public void HandsUI()
		{
			PressureValue.text = CurrentPressure.ToString("0.0") + " BAR";
			if (CurrentPressure < 0.4f)
			{
				PressureValue.color = Colors.FormatedRed;
			}
			else
			{
				PressureValue.color = Colors.White;
			}

			Hb.speed = 3f - MyPlayer.Instance.Health / 100f * 2f;
			if (MyPlayer.Instance.Health < 70 && MyPlayer.Instance.Health > 30)
			{
				HeartBeat.color = Colors.Yellow;
			}
			else if (MyPlayer.Instance.Health <= 30)
			{
				HeartBeat.color = Colors.FormatedRed;
			}
			else
			{
				HeartBeat.color = Colors.White;
			}

			if (MyPlayer.Instance.CurrentOutfit != null)
			{
				if (ArmorOk)
				{
					Armor.color = Colors.ArmorActive;
				}
				else
				{
					Armor.color = Colors.FormatedRed;
				}
			}
			else
			{
				Armor.color = Colors.FormatedRed;
			}
		}

		private void JetpackDetailsUI()
		{
			JetpackInfo.Activate(value: true);
			if (MyPlayer.Instance.FpsController.CurrentJetpack == null)
			{
				NoJetpack.Activate(value: true);
				JetpackDetails.Activate(value: false);
				return;
			}

			NoJetpack.Activate(value: false);
			JetpackDetails.Activate(value: true);
			JetpackHud.Activate(Radar.CanRadarWork && CurrentHelmet.IsVisorActive && UiActive);
			Radar.gameObject.Activate(Radar.CanRadarWork && CurrentHelmet.IsVisorActive);
			ToggleTargetingInfo.gameObject.Activate(Radar.CanRadarWork && CurrentHelmet.IsVisorActive);
			if ((bool)MyPlayer.Instance.LookingAtPlayer)
			{
				ShowTargetPlayer(MyPlayer.Instance.LookingAtPlayer);
			}
			else if (TargetInfo.activeInHierarchy)
			{
				TargetInfo.SetActive(value: false);
			}

			if (MyPlayer.Instance.FpsController.IsJetpackOn)
			{
				StabilityOn.Activate(ControlsSubsystem.GetButton(ControlsSubsystem.ConfigAction.Sprint));
			}
			else if (StabilityOn.activeInHierarchy)
			{
				StabilityOn.Activate(value: false);
			}

			if (MyPlayer.Instance.CurrentRoomTrigger != null && MyPlayer.Instance.CurrentRoomTrigger.UseGravity &&
				MyPlayer.Instance.CurrentRoomTrigger.GravityForce != Vector3.zero)
			{
				RCSDisabled.text = Localization.Disabled.ToUpper();
			}
			else
			{
				RCSDisabled.text = string.Empty;
			}

			RCSOn.Activate(MyPlayer.Instance.FpsController.CurrentJetpack.IsActive);
			RcsValue.text = FormatHelper.Percentage(CurrentHelmet.Fuel);
			RCSFiller.fillAmount = CurrentHelmet.Fuel;
			RcsWarning.Activate(CurrentHelmet.Fuel < 0.21f);
			OxygenValue.text = FormatHelper.Percentage(CurrentHelmet.Oxygen);
			OxygenFiller.fillAmount = CurrentHelmet.Oxygen;
			OxygenWarning.Activate(CurrentHelmet.Oxygen < 0.21f);
			LightsOn.Activate(CurrentHelmet.LightOn);
			if (Settings.SettingsData.GameSettings.ShowTips)
			{
				ZeroGravityTips.Activate(MyPlayer.Instance.FpsController.IsZeroG);
			}
			else
			{
				ZeroGravityTips.Activate(value: false);
			}
		}

		public void ShowTargetPlayer(Player target)
		{
			if (target != null && target != MyPlayer.Instance)
			{
				Transform bone =
					(target as OtherPlayer).tpsController.animHelper.GetBone(AnimatorHelper.HumanBones.Head_END);
				Vector3 vector =
					MyPlayer.Instance.FpsController.MainCamera.WorldToScreenPoint(bone.position +
						target.transform.up * 0.14f);
				Vector2 localPoint = Vector2.zero;
				RectTransformUtility.ScreenPointToLocalPointInRectangle(
					TargetInfo.transform.parent.GetComponent<RectTransform>(), vector, _uiCamera, out localPoint);
				TargetInfo.transform.localPosition = localPoint;
				TargetInfo.transform.rotation = Quaternion.identity;
				TargetInfo.SetActive(value: true);
				TargetInfoName.text = target.PlayerName;
			}
		}

		private void TurnOffStardust()
		{
			MyPlayer.Instance.FpsController.StarDustParticle.gameObject.SetActive(value: false);
			MyPlayer.Instance.FpsController.StarDustParticle.Stop();
		}

		public void CheckFireMod()
		{
			if (CurrentItem != null && CurrentItem is Weapon && (CurrentItem as Weapon).Mods.Count > 0)
			{
				FireMod.sprite =
					SpriteManager.Instance.GetSprite((CurrentItem as Weapon).CurrentWeaponMod.ModsFireMode);
				WeaponInfo.SetActive(value: true);
			}
			else
			{
				WeaponInfo.SetActive(value: false);
			}
		}

		public void HandsSlotUpdate()
		{
			Item itemInHands = MyPlayer.Instance == null ? null : MyPlayer.Instance.Inventory?.ItemInHands;
			if (itemInHands == null)
			{
				HandsSlotIcon.color = Colors.WhiteHalfTransparent;
				HandsSlotIcon.sprite = HandsSlotSprite;
				ItemQuantity.Activate(value: false);
				WeaponInfo.Activate(value: false);
				return;
			}

			HandsSlotIcon.color = Colors.White;
			HandsSlotIcon.sprite = itemInHands.Icon;
			if (itemInHands.MaxQuantity > 0f)
			{
				float quantityFraction = itemInHands.Quantity / itemInHands.MaxQuantity;
				QuantityFiller.fillAmount = quantityFraction;
				ItemInfo.color = quantityFraction < 0.2f ? Colors.FormatedRed : Colors.White;
				ItemInfo.text = itemInHands.Quantity.ToString("0");
				ItemQuantity.Activate(value: true);
			}
			else if (itemInHands is DisposableHackingTool)
			{
				QuantityFiller.fillAmount = itemInHands.Health / itemInHands.MaxHealth;
				ItemInfo.color = Colors.White;
				ItemInfo.text = itemInHands.Health.ToString("0");
				ItemQuantity.Activate(value: true);
			}
			else
			{
				ItemQuantity.Activate(value: false);
			}

			CheckFireMod();
		}

		public void UpdateQuickSlots()
		{
			if (MyPlayer.Instance.CurrentOutfit != null)
			{
				QuickSlotsHolder.Activate(value: true);
				List<ItemType> gre = new List<ItemType>
				{
					ItemType.APGrenade,
					ItemType.EMPGrenade
				};
				List<ItemType> con = new List<ItemType>
				{
					ItemType.AltairMedpackBig,
					ItemType.AltairMedpackSmall,
					ItemType.AltairResourceContainer
				};
				PrimarySlot.SetActive(MyPlayer.Instance.CurrentOutfit.GetSlotsByGroup(InventorySlot.Group.Primary)
					.Values.FirstOrDefault()?.Item != null);
				SecondarySlot.SetActive(MyPlayer.Instance.CurrentOutfit.GetSlotsByGroup(InventorySlot.Group.Secondary)
					.Values.FirstOrDefault()?.Item != null);
				GranadeSlot.SetActive(MyPlayer.Instance.CurrentOutfit.GetAllSlots().Values
					.FirstOrDefault((InventorySlot m) => m.Item != null && gre.Contains(m.Item.Type)) != null);
				ConsumableSlot.SetActive(MyPlayer.Instance.CurrentOutfit.GetAllSlots().Values
					.FirstOrDefault((InventorySlot m) => m.Item != null && con.Contains(m.Item.Type)) != null);
			}
			else
			{
				QuickSlotsHolder.Activate(value: false);
			}
		}

		public void WarningsUpdate()
		{
			if (Breach.activeInHierarchy)
			{
				Breach.SetActive(value: false);
			}

			if (Fire.activeInHierarchy)
			{
				Fire.SetActive(value: false);
			}

			if (Gravity.activeInHierarchy)
			{
				Gravity.SetActive(value: false);
			}

			SceneTriggerRoom currentRoom = MyPlayer.Instance.CurrentRoomTrigger;
			if (currentRoom != null)
			{
				foreach (SceneTriggerRoom item in from m in currentRoom.ParentVessel.MainVessel
							 .GetComponentsInChildren<SceneTriggerRoom>()
						 where m.CompoundRoomID == currentRoom.CompoundRoomID
						 select m)
				{
					if (item.Breach)
					{
						Breach.SetActive(value: true);
					}

					if (item.Fire)
					{
						Fire.SetActive(value: true);
					}

					if (item.GravityMalfunction)
					{
						Gravity.SetActive(value: true);
					}
				}
			}

			DebrisWarning.SetActive(MyPlayer.Instance.InDebrisField != null);
			RadiationWarning.Activate(!ArmorOk);
		}
	}
}
