using OpenHellion.Net.Message;
using System;
using UnityEngine;
using ZeroGravity.Data;
using ZeroGravity.LevelDesign;
using ZeroGravity.Network;
using ZeroGravity.UI;

namespace ZeroGravity.Objects
{
	public class Helmet : Item, IBatteryConsumer
	{
		[Serializable]
		public class Stats
		{
			public float DmgReduction;

			public float DmgResistance = 1f;
		}

		public Stats HelmetStats;

		[SerializeField] private SceneTriggerAnimation triggerAnim;

		public bool IsVisorActive;

		private Jetpack jetpack;

		public GameObject Light;

		public bool LightOn;

		public Animator LightAnimator;

		public Renderer lightMat;

		public bool IsVisorToggable;

		public SoundEffect HelmetSounds;

		public SoundEffect LightSounds;

		public float LightPowerConsumption;

		public float HUDPowerConsumption;

		private bool isOxygenSupplyActivated;

		public HelmetOverlayObject HelmetOverlay;

		public Jetpack Jetpack
		{
			get => jetpack;
			set
			{
				jetpack = value;
				if (HudUI.CurrentHelmet == this)
				{
					HudUI.UpdateUI();
				}

				if (jetpack != null)
				{
					jetpack.UpdateUI();
				}
			}
		}

		public ItemSlot BatterySlot { get; set; }

		public Battery Battery => !(BatterySlot != null) ? null : BatterySlot.Item as Battery;

		public float BatteryPower => !(Battery != null) ? 0f : Mathf.Clamp01(Battery.CurrentPower / Battery.MaxPower);

		public float Fuel => !(Jetpack != null) ? 0f : Jetpack.PropFuel;

		public float Oxygen => !(Jetpack != null) ? 0f : Jetpack.PropOxygen;

		public float Pressure =>
			!(Jetpack != null) || !(MyPlayer.Instance.CurrentRoomTrigger != null)
				? 0f
				: MyPlayer.Instance.CurrentRoomTrigger.AirPressure;

		public HelmetHudUI HudUI => World.InGameGUI.HelmetHud;

		public override bool IsInvetoryEquipable => true;

		public override EquipType EquipTo => EquipType.EquipInventory;

		private new void Start()
		{
			base.Start();
			if (triggerAnim != null)
			{
				triggerAnim.ChangeState(IsVisorActive);
			}

			if (Light != null)
			{
				LightAnimator = Light.GetComponent<Animator>();
			}
		}

		private void OnEnable()
		{
		}

		private void Update()
		{
			if (!(MyPlayer.Instance.CurrentHelmet == this))
			{
				return;
			}

			bool inDockingMode = MyPlayer.Instance.ShipControlMode == ShipControlMode.Docking;
			if (Light.GetComponent<Light>() != null)
			{
				if (Light.GetComponent<Light>().enabled && inDockingMode)
				{
					Light.GetComponent<Light>().enabled = false;
				}
				else if (!Light.GetComponent<Light>().enabled && !inDockingMode)
				{
					Light.GetComponent<Light>().enabled = true;
				}
			}
		}

		public override void Special()
		{
			InventorySlot inventorySlot = MyPlayer.Instance.Inventory.FindEmptyOutfitSlot(this, true);
			if (inventorySlot != null)
			{
				RequestAttach(inventorySlot);
				return;
			}

			string empty = string.Empty;
			empty = !(MyPlayer.Instance.Inventory.Outfit == null)
				? Localization.AlreadyEquipped.ToUpper()
				: Localization.EquipSuitFirst.ToUpper();
			World.InGameGUI.ShowInteractionCanvasMessage(empty);
		}

		public override void ChangeEquip(EquipType type, Player pl)
		{
			if (pl == null)
			{
				return;
			}

			HelmetSounds.UseFPSSounds = false;
			LightSounds.UseFPSSounds = false;
			if (pl is MyPlayer)
			{
				HelmetSounds.UseFPSSounds = true;
				LightSounds.UseFPSSounds = true;
				MyPlayer myPlayer = pl as MyPlayer;
				if (type == EquipTo)
				{
					myPlayer.CurrentHelmet = this;
					if ((bool)myPlayer.FpsController.CurrentJetpack)
					{
						Jetpack = myPlayer.FpsController.CurrentJetpack;
						Jetpack.Helmet = this;
					}

					transform.parent = myPlayer.HelmetPlacementTransform;
					transform.localPosition = Vector3.zero;
					transform.localRotation = Quaternion.identity;
					ToggleVisor(IsVisorActive, false, true);

					if (HudUI.CurrentHelmet == this)
					{
						HudUI.gameObject.SetActive(true);
					}

					MyPlayer.Instance.CheckEquipmentAchievement();
				}
				else if (myPlayer.CurrentHelmet == this)
				{
					myPlayer.CurrentHelmet = null;
					if (Jetpack != null)
					{
						Jetpack.Helmet = null;
						Jetpack = null;
					}

					myPlayer.FpsController.HelmetGlassEffect.Raise();

					if (HudUI.CurrentHelmet == this)
					{
						HudUI.gameObject.SetActive(false);
					}
				}
			}
			else if (pl is OtherPlayer)
			{
				(pl as OtherPlayer).CurrentHelmet = type != EquipTo ? null : this;
				if ((pl as OtherPlayer).hairMesh != null)
				{
					(pl as OtherPlayer).hairMesh.SetBlendShapeWeight(0, type != EquipTo ? 100f : 0f);
				}
			}
		}

		public void ToggleVisor(bool? isActive = null, bool send = true, bool initialize = false)
		{
			if ((IsVisorToggable && (triggerAnim == null || !triggerAnim.IsEventFinished)) ||
			    (isActive.HasValue && isActive.Value == IsVisorActive && !initialize))
			{
				return;
			}

			if (send)
			{
				World.SolarSystem.SendCommand(StateUpdateRequest.CommandType.SetHelmetVisor, GUID, value: !isActive.HasValue ? !IsVisorActive : isActive.Value);
			}
			else
			{
				if (!isActive.HasValue)
				{
					return;
				}

				IsVisorActive = isActive.Value;

				if (DynamicObj.Parent is MyPlayer)
				{
					MyPlayer myPlayer = DynamicObj.Parent as MyPlayer;
					if (!IsVisorActive && !IsVisorToggable)
					{
					}
				}

				if (IsVisorToggable)
				{
					if (DynamicObj.Parent is MyPlayer)
					{
						MyPlayer myPlayer2 = DynamicObj.Parent as MyPlayer;
						if (myPlayer2.CurrentHelmet == this)
						{
							triggerAnim.ChangeState(IsVisorActive);
							if (IsVisorActive)
							{
								myPlayer2.FpsController.HelmetGlassEffect.Lower();
							}
							else
							{
								myPlayer2.FpsController.HelmetGlassEffect.Raise();
							}
						}
					}
					else if (DynamicObj.Parent is OtherPlayer)
					{
						OtherPlayer otherPlayer = DynamicObj.Parent as OtherPlayer;
						if (otherPlayer.CurrentHelmet == this)
						{
							triggerAnim.ChangeState(IsVisorActive);
						}
					}
				}

				if (HudUI.CurrentHelmet == this)
				{
					HudUI.UpdateUI();
				}
			}
		}

		public void ToggleLight(bool? isActive = null, bool send = true)
		{
			if (send)
			{
				World.SolarSystem.SendCommand(StateUpdateRequest.CommandType.SetHelmetLight, GUID, value: isActive.HasValue ? isActive.Value : Light != null && !LightOn);
				return;
			}

			if (Light != null)
			{
				if (LightSounds != null && LightOn != isActive.Value)
				{
					LightSounds.Play();
				}

				LightOn = isActive.Value;
				LightAnimator.SetBool("OnOff", LightOn);
			}

			if (lightMat != null)
			{
				lightMat.material.SetColor("_EmissionColor", !isActive.Value ? Color.black : Color.white * 8f);
			}
		}

		public override void ProcesStatsData(DynamicObjectStats dos)
		{
			base.ProcesStatsData(dos);
			HelmetStats helmetStats = dos as HelmetStats;
			if (helmetStats.isVisorActive.HasValue)
			{
				ToggleVisor(helmetStats.isVisorActive.Value, false);
			}

			if (helmetStats.isLightActive.HasValue)
			{
				ToggleLight(helmetStats.isLightActive.Value, false);
			}
		}

		public override bool ProcessSlotChange(Inventory inv, InventorySlot mySlot, InventorySlot nextSlot)
		{
			if (mySlot.SlotType == InventorySlot.Type.Equip && (nextSlot == null || nextSlot.CanFitItem(this)))
			{
				if (inv.ItemInHands != null && !inv.AddToInventoryOrDrop(inv.ItemInHands, inv.HandsSlot))
				{
					return true;
				}

				StartItemAnimation(ItemAnimationType.Unequip, nextSlot, nextSlot == null);
				World.InGameGUI.PlayerOverview.Toggle(false);
				return true;
			}

			if (nextSlot != null && nextSlot.SlotType == InventorySlot.Type.Equip && nextSlot.CanFitItem(this))
			{
				if (inv.ItemInHands != null && inv.ItemInHands != this &&
				    !inv.AddToInventoryOrDrop(inv.ItemInHands, inv.HandsSlot))
				{
					return true;
				}

				StartItemAnimation(ItemAnimationType.Equip, nextSlot, false);
				World.InGameGUI.PlayerOverview.Toggle(false);
				return true;
			}

			return false;
		}

		public override DynamicObjectAuxData GetAuxData()
		{
			HelmetData baseAuxData = GetBaseAuxData<HelmetData>();
			baseAuxData.IsLightActive = Light != null && Light.activeInHierarchy;
			baseAuxData.IsVisorActive = triggerAnim != null && triggerAnim.IsActive;
			baseAuxData.IsVisorToggleable = IsVisorToggable;
			baseAuxData.DamageReduction = HelmetStats.DmgReduction;
			baseAuxData.DamageResistance = HelmetStats.DmgResistance;
			baseAuxData.LightPowerConsumption = LightPowerConsumption;
			baseAuxData.HUDPowerConsumption = HUDPowerConsumption;
			return baseAuxData;
		}

		public void SwitchCanvas(bool isRegular)
		{
			HudUI.gameObject.SetActive(isRegular);
		}

		public override string QuantityCheck()
		{
			return !(Battery == null) ? FormatHelper.Percentage(BatteryPower) : "0";
		}
	}
}
