using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity.LevelDesign
{
	public class SceneDockingPort : MonoBehaviour, ISceneObject
	{
		public Transform CameraPosition;

		public string Name;

		public string DockType = string.Empty;

		[Tooltip("Whether this port control from the docking panel is disabled or not.")]
		public bool LocalyDisabled;

		[FormerlySerializedAs("Executer")] public SceneTriggerExecutor Executor;

		public string DockState;

		public string UndockState;

		public Transform SceneDockingTriggerTrans;

		public int DockingPortOrder;

		private bool _isDocked;

		[SerializeField] private int _inSceneID;

		public SceneDockingPort DockedToPort;

		[HideInInspector] public Ship ParentShip;

		public bool IsSlave;

		public bool Locked;

		[FormerlySerializedAs("mergeExecuters")] public List<SceneTriggerExecutor> MergeExecutors;

		[FormerlySerializedAs("mergeExecutersDistance")] public float MergeExecutorDistance = 1f;

		[Space(5f)] public List<SceneDoor> Doors;

		public float DoorPairingDistance = 0.5f;

		public List<SceneDockingPortController> portControllers = new List<SceneDockingPortController>();

		public GameObject DockingVisualROOT;

		public bool IgnoreThisDockingPort;

		public bool LeverPulse;

		public int InSceneID
		{
			get => _inSceneID;
			set => _inSceneID = value;
		}

		private void Start()
		{
			if (ParentShip == null)
			{
				ParentShip = GetComponentInParent<GeometryRoot>().MainObject as Ship;
			}
		}

		public void ToggleDock(bool? isDocked = null, bool isInstant = false)
		{
			_isDocked = !isDocked.HasValue ? (!_isDocked) : isDocked.Value;
			if (Executor != null)
			{
				if (isInstant)
				{
					Executor.ChangeStateImmediate((!_isDocked) ? UndockState : DockState);
				}
				else
				{
					Executor.ChangeState((!_isDocked) ? UndockState : DockState);
				}
			}

			if (!_isDocked || portControllers.Count <= 0)
			{
				return;
			}

			foreach (SceneDockingPortController portController in portControllers)
			{
				portController.ToggleLock(isLocked: false, isInstant);
			}
		}

		public void SetDetails(SceneDockingPortDetails details, bool isInitialize)
		{
			isInitialize |= details.IsImmediate is true;

			Locked = details.Locked;
			if (details.DockedToID == null)
			{
				return;
			}

			if (details.DockingStatus)
			{
				IsSlave = true;
				Ship ship = ParentShip.World.GetVessel(details.DockedToID.VesselGUID) as Ship;
				if (ship == null)
				{
					if (!isInitialize)
					{
						Debug.LogErrorFormat("Docking Port: There is no parent ship. Child ship: {0}, Parent ship: {1}", ParentShip.Guid,
							details.DockedToID.VesselGUID);
					}

					return;
				}

				SceneDockingPort dockedToPort = ship.GetStructureObject<SceneDockingPort>(details.DockedToID.InSceneID);
				if (dockedToPort == null)
				{
					if (!isInitialize)
					{
						Debug.LogErrorFormat("Docking Port: There is no docking port on ship. Child ship: {0}, Parent ship: {1}, Parent loaded: {2}, Docked to port id: {3}", ParentShip.Guid, details.DockedToID.VesselGUID, ship.SceneObjectsLoaded, details.DockedToID.InSceneID);
					}

					return;
				}

				if (DockedToPort == dockedToPort && dockedToPort.DockedToPort == this &&
				    ParentShip.MainVessel == ship.MainVessel)
				{
					Debug.LogWarningFormat("Docking Port: ignored a re-dock of an already docked pair. Child ship: {0}, port: {1}, Parent ship: {2}, port: {3}, Immediate: {4}",
						ParentShip.Guid, InSceneID, ship.Guid, dockedToPort.InSceneID, details.IsImmediate is true);

					dockedToPort.Locked = Locked;
					if (details.RelativePosition != null && details.RelativeRotation != null)
					{
						ParentShip.RelativePosition = details.RelativePosition.ToVector3();
						ParentShip.RelativeRotation = details.RelativeRotation.ToQuaternion();
					}

					return;
				}

				DockedToPort = dockedToPort;
				DockedToPort.Locked = Locked;
				DockedToPort.DockedToPort = this;
				ParentShip.OnDockCompleted = OnDockCompleted;
				ParentShip.DockToShip(this, ship, DockedToPort, details, isInitialize);
				if (!isInitialize && MyPlayer.Instance.Parent == ParentShip &&
				    MyPlayer.Instance.LockedToTrigger != null && MyPlayer.Instance.LockedToTrigger.TriggerType ==
				    SceneTriggerType.DockingPanel)
				{
					ParentShip.World.InWorldPanels.Docking.UpdateDockingPorts();
				}
			}
			else
			{
				IsSlave = false;
				if (DockedToPort != null)
				{
					ParentShip.OnUndockStarted = OnUndockStarted;
					ParentShip.UndockFromShip(this, DockedToPort.ParentShip, DockedToPort, details);
				}
			}
		}

		public void OnDockCompleted(bool isInitialize)
		{
			ToggleDock(true, isInitialize);
			DockedToPort.ToggleDock(true, isInitialize);
			foreach (SceneDockingPortController portController in portControllers)
			{
				portController.OnLeverStateChange();
			}
		}

		public void OnUndockStarted(bool isInitialize)
		{
			ToggleDock(false, isInitialize);
			DockedToPort.ToggleDock(false, isInitialize);
			DockedToPort.DockedToPort = null;
			DockedToPort = null;
		}

		public void UndockPort()
		{
			if (DockedToPort != null)
			{
				if (IsSlave)
				{
					ToggleDockTo(DockedToPort, toggle: false);
				}
				else
				{
					DockedToPort.ToggleDockTo(this, toggle: false);
				}
			}
		}

		public IEnumerator PulseColourOnMaterial()
		{
			float time = 0f;
			while (LeverPulse)
			{
				yield return new WaitForEndOfFrame();
				time += Time.deltaTime * 5f;
				foreach (SceneDockingPortController portController in portControllers)
				{
					portController.LeverLight.material.SetFloat("_EmissionAmount", 30f + Mathf.Sin(time) * 20f);
				}
			}
		}

		private IEnumerator ReturnLever(float time, List<SceneDockingPortController> controllers)
		{
			yield return new WaitForSeconds(time);
			foreach (SceneDockingPortController controller in controllers)
			{
				SceneTriggerExecutor getExecutor = controller.GetExecutor;
				getExecutor.ChangeState("up");
				controller.IsLocked = false;
				controller.OnLeverStateChange();
			}
		}

		public bool ToggleDockTo(SceneDockingPort sdc, bool toggle, bool checkTogglePossibility = true)
		{
			if (DockType.TagCount() >= 4 && portControllers.Count > 0)
			{
				StartCoroutine(ReturnLever(2f, portControllers));
			}

			if (!IsDockTogglePossibleWith(sdc, toggle) && checkTogglePossibility)
			{
				return false;
			}

			Ship parentShip = sdc.ParentShip;
			SceneDockingPortDetails sceneDockingPortDetails = new SceneDockingPortDetails
			{
				ID = new VesselObjectID(ParentShip.Guid, InSceneID),
				DockedToID = new VesselObjectID(parentShip.Guid, sdc.InSceneID),
				DockingStatus = toggle
			};
			Ship parentShip2 = ParentShip;
			SceneDockingPortDetails dockingPort = sceneDockingPortDetails;
			parentShip2.ChangeStats(null, null, null, null, null, null, null, null, null, dockingPort);
			return true;
		}

		public void AddPortController(SceneDockingPortController controller)
		{
			portControllers.Add(controller);
		}

		public void TryToUndock()
		{
			if (DockedToPort == null)
			{
				return;
			}

			if (DockType.TagCount() >= 4)
			{
				if (IsSlave)
				{
					ToggleDockTo(DockedToPort, toggle: false, checkTogglePossibility: false);
				}
				else
				{
					DockedToPort.ToggleDockTo(this, toggle: false, checkTogglePossibility: false);
				}
			}
			else if (DockType.ExtractTags().Contains("airlock") && DockedToPort.DockType.TagCount() < 4)
			{
				if (IsSlave)
				{
					ToggleDockTo(DockedToPort, toggle: false, checkTogglePossibility: false);
				}
				else
				{
					DockedToPort.ToggleDockTo(this, toggle: false, checkTogglePossibility: false);
				}
			}
			else if (DockedToPort == null ||
			         (portControllers.Count > 0 &&
			          portControllers.Find((SceneDockingPortController m) => !m.IsLocked) != null) ||
			         (DockedToPort.portControllers.Count > 0 &&
			          DockedToPort.portControllers.Find((SceneDockingPortController m) => !m.IsLocked) != null))
			{
				return;
			}

			if (IsSlave)
			{
				ToggleDockTo(DockedToPort, toggle: false);
			}
			else
			{
				DockedToPort.ToggleDockTo(this, toggle: false);
			}
		}

		public bool IsDockTogglePossibleWith(SceneDockingPort sdp, bool isDocking)
		{
			if (!isDocking)
			{
				if (portControllers.Count > 0 &&
				    portControllers.Find((SceneDockingPortController m) => !m.IsLocked) != null)
				{
					return false;
				}

				if (sdp.portControllers.Count > 0 &&
				    sdp.portControllers.Find((SceneDockingPortController m) => !m.IsLocked) != null)
				{
					return false;
				}
			}

			return true;
		}

		private void OnDrawGizmos()
		{
			Gizmos.color = new Color(1f, 0.2f, 0.3f, 0.2f);
			if (SceneDockingTriggerTrans != null)
			{
				Gizmos.matrix = SceneDockingTriggerTrans.localToWorldMatrix;
				Gizmos.DrawCube(SceneDockingTriggerTrans.GetComponent<BoxCollider>().center,
					SceneDockingTriggerTrans.GetComponent<BoxCollider>().size);
			}

			Gizmos.matrix = transform.localToWorldMatrix;
			Gizmos.DrawLine(Vector3.zero, Vector3.forward);
			Gizmos.DrawLine(Vector3.forward, new Vector3(-0.2f, 0f, 0.8f));
			Gizmos.DrawLine(new Vector3(-0.2f, 0f, 0.8f), new Vector3(0.2f, 0f, 0.8f));
			Gizmos.DrawLine(new Vector3(0.2f, 0f, 0.8f), Vector3.forward);
			Gizmos.DrawLine(new Vector3(0.3f, 0f, 0f), Vector3.up * 0.5f);
			Gizmos.DrawLine(new Vector3(-0.3f, 0f, 0f), Vector3.up * 0.5f);
			Gizmos.color = new Color(1f, 0.2f, 0.3f, 0.1f);
			if (CameraPosition != null)
			{
				Gizmos.matrix = CameraPosition.localToWorldMatrix;
				Gizmos.DrawFrustum(CameraPosition.TransformPoint(Vector3.zero), 60f, 1f, 0.2f, 1.7f);
				Gizmos.DrawLine(new Vector3(0.3f, 0f, 0f) * 0.5f, Vector3.up * 0.25f);
				Gizmos.DrawLine(new Vector3(-0.3f, 0f, 0f) * 0.5f, Vector3.up * 0.25f);
			}
		}

		private void OnDestroy()
		{
			if ((bool)GetComponentInChildren<Camera>() &&
			    MyPlayer.Instance != null &&
			    MyPlayer.Instance.LockedToTrigger is SceneTriggerShipControl &&
			    MyPlayer.Instance.ShipControlMode == ShipControlMode.Docking)
			{
				(MyPlayer.Instance.LockedToTrigger as SceneTriggerShipControl).CancelInteract(MyPlayer.Instance);
			}
		}
	}
}
