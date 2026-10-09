using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ZeroGravity.Data;
using ZeroGravity.LevelDesign;
using ZeroGravity.Network;
using ZeroGravity.Objects;
using ZeroGravity.ShipComponents;

namespace ZeroGravity.UI
{
	public class DoorEnviormentPanel : MonoBehaviour
	{
		public SceneTriggerRoom Room;

		public Text EnvMonitorHeading;

		public Text PressureValue;

		public Text AirQualityValue;

		public Text Gravity;

		public GameObject Breach;

		public GameObject Fire;

		public GameObject GravityHazard;

		public Image VesselHealth;

		public Text VesselHealthText;

		public GameObject HPDecay;

		private List<SceneMachineryPartSlot> ArmorSlots = new List<SceneMachineryPartSlot>();

		public Image NaniteHealth;

		public Text NaniteHealthValue;

		public GameObject Armor;

		public Text ArmorValue;

		public GameObject SelfDestructActive;

		public Text SelfDestructTime;

		public GameObject NoPower;

		public GameObject ManeuverActive;

		public Text ManeuverEta;

		public List<GameObject> BaseConsumers;

		public Text DegradationRate;

		public Text SafeTime;

		public Text SystemFailLog;

		public GameObject WarningHolder;

		public GameObject RadiationWarning;

		public GameObject SystemFail;

		public GameObject DebrisField;

		public GameObject DistressCallActive;

		public Ship ParentShip
		{
			get
			{
				if (Room != null)
				{
					return Room.ParentVessel.MainVessel as Ship;
				}

				return null;
			}
		}

		public float Degradation =>
			(float)(ParentShip.ExposureDamage * SpaceObjectVessel.VesselDecayRateMultiplier);

		private void Start()
		{
			if (Room != null)
			{
				Room.AddBehaviourScript(this);
				DoorEnviormentUpdateUI();
				foreach (SceneMachineryPartSlot item in Room.ParentVessel.VesselBaseSystem.MachineryPartSlots.Where(
					         (SceneMachineryPartSlot m) => m.Scope == MachineryPartSlotScope.Armor))
				{
					ArmorSlots.Add(item);
				}
			}

			Text[] componentsInChildren = GetComponentsInChildren<Text>(includeInactive: true);
			foreach (Text text in componentsInChildren)
			{
				string value = null;
				if (Localization.EnvironmentPanelLocalization.TryGetValue(text.name, out value))
				{
					text.text = value;
				}
			}

			if (MyPlayer.Instance.IsAlive && GetComponentInChildren<Canvas>() != null)
			{
				GetComponentInChildren<Canvas>().worldCamera = MyPlayer.Instance.FpsController.MainCamera;
			}
		}

		public void DoorEnviormentUpdateUI()
		{
			if (Room == null || NoPower == null || Room.ParentVessel == null)
			{
				return;
			}

			if (this.IsInvoking(DoorEnviormentUpdateUI))
			{
				this.CancelInvoke(DoorEnviormentUpdateUI);
			}

			this.InvokeRepeating(DoorEnviormentUpdateUI, 3f, 3f);
			if (!MyPlayer.Instance.IsInVesselHierarchy(ParentShip))
			{
				return;
			}

			NoPower.SetActive(!Room.ParentVessel.HasPower);
			foreach (GameObject baseConsumer in BaseConsumers)
			{
				baseConsumer.SetActive(Room.ParentVessel.HasPower);
			}

			if (!(Room != null))
			{
				return;
			}

			CheckSystems();
			DebrisField.SetActive(MyPlayer.Instance.InDebrisField != null);
			if (Room.ParentVessel.MainVessel.IsDistressSignalActive && !DistressCallActive.activeInHierarchy)
			{
				DistressCallActive.SetActive(value: true);
			}
			else if (!Room.ParentVessel.MainVessel.IsDistressSignalActive && DistressCallActive.activeInHierarchy)
			{
				DistressCallActive.SetActive(value: false);
			}

			VesselHealth.fillAmount = Room.ParentVessel.Health / Room.ParentVessel.MaxHealth;
			VesselHealthText.text = FormatHelper.CurrentMax(Room.ParentVessel.Health, Room.ParentVessel.MaxHealth);
			if (Room.ParentVessel.Armor > 0f)
			{
				Armor.GetComponent<Image>().color = Colors.ArmorActive;
			}
			else
			{
				Armor.GetComponent<Image>().color = Colors.FormatedRed;
			}

			UpdateArmor();
			float? selfDestructTimer = Room.ParentVessel.SelfDestructTimer;
			if (selfDestructTimer.HasValue)
			{
				TimeSpan timeSpan = TimeSpan.FromSeconds(Room.ParentVessel.SelfDestructTimer.Value);
				string text = $"{timeSpan.TotalHours:n0} : {timeSpan.Minutes:n0} : {timeSpan.Seconds:n0}";
				SelfDestructActive.SetActive(value: true);
				SelfDestructTime.text = text;
			}
			else
			{
				SelfDestructActive.SetActive(value: false);
				SelfDestructTime.text = string.Empty;
			}

			AirQualityValue.text = (Room.AirQuality * 100f).ToString("f0");
			PressureValue.text = Room.AirPressure.ToString("0.0");
			if (!Room.IsAirOk)
			{
				AirQualityValue.color = Colors.Red;
				PressureValue.color = Colors.Red;
			}
			else
			{
				AirQualityValue.color = Colors.White;
				PressureValue.color = Colors.White;
			}

			if (Room.UseGravity && Room.GravityForce.sqrMagnitude > float.Epsilon)
			{
				Gravity.color = Colors.White;
				Gravity.text = "ON";
			}
			else
			{
				Gravity.text = "OFF";
				Gravity.color = Colors.Red;
			}

			Breach.SetActive(Room.Breach);
			Fire.SetActive(Room.Fire);
			GravityHazard.SetActive(Room.GravityMalfunction);
			if (ManeuverActive != null)
			{
				if (ParentShip != null && ParentShip.EndWarpTime > 0.0 && ParentShip.IsWarpOnline)
				{
					ManeuverActive.SetActive(value: true);
					ManeuverEta.text = Localization.ETA + " " +
					                   FormatHelper.PeriodFormat(ParentShip.EndWarpTime -
					                                             Room.ParentVessel.World.SolarSystem.CurrentTime);
				}
				else
				{
					ManeuverActive.SetActive(value: false);
				}
			}

			DegradationRate.text = Degradation.ToString("0.0") + " HP/s";
			ArmorValue.text = Room.ParentVessel.Armor.ToString("0.0") + " HP/s";
			if (Degradation > Room.ParentVessel.Armor)
			{
				ArmorValue.color = Colors.FormatedRed;
				RadiationWarning.Activate(value: true);
				HPDecay.Activate(value: true);
			}
			else
			{
				ArmorValue.color = Colors.White;
				RadiationWarning.Activate(value: false);
				HPDecay.Activate(value: false);
			}
		}

		public void DistressSignalsRefresh(bool isDistress)
		{
			if (DistressCallActive != null)
			{
				DistressCallActive.SetActive(isDistress);
			}
		}

		private void OnDrawGizmos()
		{
			if (Room == null)
			{
				Gizmos.DrawIcon(transform.position, "RoomNotAssigned");
			}
		}

		private void OnDrawGizmosSelected()
		{
			if (Room != null)
			{
				Gizmos.color = Color.green;
				Gizmos.DrawLine(Room.transform.position, transform.position);
			}
		}

		public void CheckSystems()
		{
			WarningHolder.Activate(DebrisField.activeSelf || SystemFail.activeSelf || DistressCallActive.activeSelf ||
			                       RadiationWarning.activeSelf);
			SystemFailLog.text = string.Empty;
			bool flag = false;
			foreach (SubSystem value in ParentShip.SubSystems.Values)
			{
				if (value.SecondaryStatus == SystemSecondaryStatus.Defective)
				{
					flag = true;
					Text systemFailLog = SystemFailLog;
					string text = systemFailLog.text;
					systemFailLog.text = text + value.Type.ToLocalizedString().ToUpper() + " " +
					                     Localization.SystemFailiure.ToUpper() + "\n";
				}
			}

			foreach (Generator value2 in ParentShip.Generators.Values)
			{
				if (value2.SecondaryStatus == SystemSecondaryStatus.Defective)
				{
					flag = true;
					Text systemFailLog2 = SystemFailLog;
					string text = systemFailLog2.text;
					systemFailLog2.text = text + value2.Type.ToLocalizedString().ToUpper() + " " +
					                      Localization.SystemFailiure.ToUpper() + "\n";
				}
			}

			if (flag)
			{
				SystemFail.Activate(value: true);
			}
			else
			{
				SystemFailLog.text = string.Empty;
				SystemFail.Activate(value: false);
			}

			if (DebrisField.activeSelf)
			{
				Text systemFailLog3 = SystemFailLog;
				systemFailLog3.text = systemFailLog3.text + Localization.InDebrisField.ToUpper() + "\n";
			}

			if (DistressCallActive.activeSelf)
			{
				Text systemFailLog4 = SystemFailLog;
				systemFailLog4.text = systemFailLog4.text + Localization.DistressCallActive.ToUpper() + "\n";
			}

			if (RadiationWarning.activeSelf)
			{
				Text systemFailLog5 = SystemFailLog;
				string text = systemFailLog5.text;
				systemFailLog5.text =
					text + Localization.Radiation.ToUpper() + " " + Localization.High.ToUpper() + "\n";
			}
		}

		public void UpdateArmor()
		{
			float cumulativeHealth = 0f;
			float cumulativeMaxHealth = 0f;
			foreach (SceneMachineryPartSlot armorSlot in ArmorSlots)
			{
				if (armorSlot.Item != null)
				{
					cumulativeHealth += armorSlot.Item.Health;
					cumulativeMaxHealth += armorSlot.Item.MaxHealth;
				}
			}

			if (Room.ParentVessel.Armor > 0f)
			{
				NaniteHealth.fillAmount = cumulativeHealth / cumulativeMaxHealth;
				NaniteHealthValue.text = FormatHelper.CurrentMax(cumulativeHealth, cumulativeMaxHealth);
				float num3 = Degradation > Room.ParentVessel.Armor
					? (cumulativeHealth / Room.ParentVessel.Armor)
					: (cumulativeHealth / Degradation);
				SafeTime.text = FormatHelper.PeriodFormat(num3);
			}
			else
			{
				NaniteHealth.fillAmount = 0f;
				NaniteHealthValue.text = "0";
				SafeTime.text = Localization.Armor.ToUpper() + " " + Localization.Missing.ToUpper();
			}
		}
	}
}
