using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using ZeroGravity.LevelDesign;
using ZeroGravity.Network;

namespace ZeroGravity.ShipComponents
{
	public abstract class VesselSystem : VesselComponent, ISoundOccludable
	{
		public SceneTriggerRoom Room;

		public float PowerUpTime;

		public float CoolDownTime;

		public bool AutoReactivate = true;

		public UnityEvent OnOnlineEvent;

		public UnityEvent OnOfflineEvent;

		public UnityEvent OnPowerUpEvent;

		public UnityEvent OnCooldownEvent;

		[SerializeField] private SystemStatus _Status = SystemStatus.Offline;

		public SystemSecondaryStatus SecondaryStatus;

		[Space(5f)] public ResourceContainer[] ResourceContainers = new ResourceContainer[0];

		public bool ExclusiveResourceContainers = true;

		[NonSerialized] public HashSet<IVesselSystemAccessory> Accessories = new HashSet<IVesselSystemAccessory>();

		[NonSerialized] public bool AutoRestart;

		public float InputFactor;

		public float PowerInputFactor;

		public virtual SystemStatus Status
		{
			get { return _Status; }
			set
			{
				if (_Status == value)
				{
					return;
				}

				switch (value)
				{
					case SystemStatus.Online:
						SceneQuestTrigger.OnTrigger(gameObject, SceneQuestTriggerEvent.SystemSwitchOn);
						if (OnOnlineEvent != null)
						{
							OnOnlineEvent.Invoke();
						}

						break;
					case SystemStatus.Offline:
						SceneQuestTrigger.OnTrigger(gameObject, SceneQuestTriggerEvent.SystemSwitchOff);
						if (OnOfflineEvent != null)
						{
							OnOfflineEvent.Invoke();
						}

						break;
					case SystemStatus.Powerup:
						if (OnPowerUpEvent != null)
						{
							OnPowerUpEvent.Invoke();
						}

						break;
					case SystemStatus.Cooldown:
						if (OnCooldownEvent != null)
						{
							OnCooldownEvent.Invoke();
						}

						break;
				}

				_Status = value;
				foreach (IVesselSystemAccessory accessory in Accessories)
				{
					accessory.BaseVesselSystemUpdated();
				}
			}
		}

		public abstract void SwitchOn();

		public abstract void SwitchOff();

		public abstract void Toggle();

		public bool IsSwitchedOn()
		{
			return Status == SystemStatus.Online || Status == SystemStatus.Powerup ||
			       (Status == SystemStatus.Offline && AutoRestart);
		}

		public string GetStatus(out Color color)
		{
			string empty = string.Empty;
			color = Colors.White;
			if (Status == SystemStatus.Offline && SecondaryStatus == SystemSecondaryStatus.Defective)
			{
				empty = Localization.Defective.ToUpper();
				color = Colors.Defective;
			}
			else
			{
				empty = Status.ToLocalizedString().ToUpper();
				color = Colors.Status[Status];
			}

			return empty;
		}

		private void OnDrawGizmos()
		{
			Gizmos.DrawIcon(transform.position, "SubSystem");
			if (Room == null)
			{
				Gizmos.DrawIcon(transform.position + transform.up * 0.2f, "RoomNotAssigned");
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
	}
}
