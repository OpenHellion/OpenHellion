using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using ZeroGravity.LevelDesign;
using ZeroGravity.Objects;

namespace ZeroGravity.CharacterMovement
{
	public class TransitionTriggerHelper : MonoBehaviour
	{
		[FormerlySerializedAs("transferableObj")] [SerializeField]
		private SpaceObjectTransferable _transferableObj;

		[HideInInspector]
		public bool Enabled = true;

		private SphereCollider _sphereCollider;

		private Dictionary<SceneTriggerRoom, HashSet<Collider>> _roomTriggersDict = new Dictionary<SceneTriggerRoom, HashSet<Collider>>();

		public SpaceObjectTransferable TransferableObject => _transferableObj;

		private SceneTriggerRoom GetRoomTrigger(Collider triggerCollider)
		{
			SceneTriggerRoom sceneTriggerRoom = triggerCollider.GetComponent<SceneTriggerRoom>();
			if (sceneTriggerRoom != null) return sceneTriggerRoom;
			SceneTriggerRoomSegment component = triggerCollider.GetComponent<SceneTriggerRoomSegment>();
			if (component != null)
			{
				sceneTriggerRoom = component.BaseRoom;
			}
			return sceneTriggerRoom;
		}

		private void Awake()
		{
			if (_transferableObj == null)
			{
				Debug.LogErrorFormat("Transition trigger transferable object not set {0}.", name);
			}
		}

		private void Start()
		{
			if (_transferableObj is not MyPlayer) return;
			_sphereCollider = GetComponentInParent<SphereCollider>();
			this.InvokeRepeating(AuxCheckRoomTriggers, 0f, 5f);
		}

		private void OnTriggerEnter(Collider coli)
		{
			if (!Enabled)
			{
				return;
			}
			SceneTriggerRoom roomTrigger = GetRoomTrigger(coli);
			if (TransferableObject is DynamicObject && roomTrigger == MyPlayer.Instance.MyRoomTrigger)
			{
				return;
			}
			if (roomTrigger != null)
			{
				bool isFirstRoom = _roomTriggersDict.Count == 0;
				if (!_roomTriggersDict.ContainsKey(roomTrigger))
				{
					_roomTriggersDict.Add(roomTrigger, new HashSet<Collider>());
				}
				_roomTriggersDict[roomTrigger].Add(coli);
				if (isFirstRoom)
				{
					_transferableObj.EnterVesselRoomTrigger = roomTrigger;
					_transferableObj.EnterVessel(roomTrigger.ParentVessel);
					SceneTriggerRoom currentRoomTrigger = _transferableObj.CurrentRoomTrigger;
					_transferableObj.CurrentRoomTrigger = roomTrigger;
					_transferableObj.RoomChanged(currentRoomTrigger);
				}
				else if (_transferableObj.CurrentRoomTrigger != null && _transferableObj.CurrentRoomTrigger.ParentVessel is Asteroid && roomTrigger.ParentVessel is Ship)
				{
					_transferableObj.EnterVesselRoomTrigger = roomTrigger;
					_transferableObj.EnterVessel(roomTrigger.ParentVessel);
					SceneTriggerRoom currentRoomTrigger2 = _transferableObj.CurrentRoomTrigger;
					_transferableObj.CurrentRoomTrigger = roomTrigger;
					_transferableObj.RoomChanged(currentRoomTrigger2);
				}
				else
				{
					_transferableObj.EnterVesselRoomTrigger = null;
				}
			}
			else if (_transferableObj is MyPlayer myPlayer)
			{
				SceneTriggerLadder componentInParent = coli.GetComponentInParent<SceneTriggerLadder>();
				if (componentInParent == null) return;
				myPlayer.InLadderTrigger = true;
				myPlayer.LadderTrigger = componentInParent;
			}
		}

		private void OnTriggerExit(Collider coli)
		{
			if (!Enabled)
			{
				return;
			}
			SceneTriggerRoom roomTrigger = GetRoomTrigger(coli);
			if (roomTrigger != null && _roomTriggersDict.ContainsKey(roomTrigger))
			{
				_roomTriggersDict[roomTrigger].Remove(coli);
				if (_roomTriggersDict[roomTrigger].Count != 0)
				{
					return;
				}
				_roomTriggersDict.Remove(roomTrigger);
				SceneTriggerRoom sceneTriggerRoom = null;
				foreach (SceneTriggerRoom key in _roomTriggersDict.Keys)
				{
					if (sceneTriggerRoom == null)
					{
						sceneTriggerRoom = key;
					}
					if (sceneTriggerRoom.ParentVessel is Ship)
					{
						break;
					}
					if (sceneTriggerRoom != key && key.ParentVessel is not Asteroid)
					{
						sceneTriggerRoom = key;
					}
				}
				if (sceneTriggerRoom != null)
				{
					SceneTriggerRoom currentRoomTrigger = _transferableObj.CurrentRoomTrigger;
					if (_transferableObj.Parent != sceneTriggerRoom.ParentVessel)
					{
						if (_transferableObj.Parent is SpaceObjectVessel vessel && vessel != sceneTriggerRoom.ParentVessel)
						{
							if (vessel.IsDocked)
							{
								vessel = vessel.DockedToMainVessel;
							}
							if (vessel == sceneTriggerRoom.ParentVessel || vessel.AllDockedVessels.Contains(sceneTriggerRoom.ParentVessel))
							{
								_transferableObj.DockedVesselParentChanged(sceneTriggerRoom.ParentVessel);
							}
							else
							{
								_transferableObj.EnterVessel(sceneTriggerRoom.ParentVessel);
							}
						}
						else
						{
							Debug.LogError("How did this happen, player exited room trigger and its parent is not vessel.");
						}
					}
					_transferableObj.CurrentRoomTrigger = sceneTriggerRoom;
					_transferableObj.RoomChanged(currentRoomTrigger);
				}
				else
				{
					// Unity raises a trigger exit whenever a collider's physics representation changes - a
					// re-parent, a collider toggle, an isKinematic flip - none of which mean the object moved.
					// Measure the volumes directly and rebuild the bookkeeping the spurious exit emptied.
					foreach (Collider candidate in Physics.OverlapSphere(_transferableObj.transform.position,
						         0.1f, ~0, QueryTriggerInteraction.Collide))
					{
						SceneTriggerRoom room = GetRoomTrigger(candidate);
						if (room == null)
						{
							continue;
						}

						if (!_roomTriggersDict.TryGetValue(room, out HashSet<Collider> roomColliders))
						{
							_roomTriggersDict[room] = roomColliders = new HashSet<Collider>();
						}

						roomColliders.Add(candidate);
					}

					sceneTriggerRoom = _roomTriggersDict.Keys.FirstOrDefault(room => room.ParentVessel is Ship)
					                   ?? _roomTriggersDict.Keys.FirstOrDefault();
					if (sceneTriggerRoom != null)
					{
						if (_transferableObj.CurrentRoomTrigger != sceneTriggerRoom)
						{
							SceneTriggerRoom previousRoomTrigger = _transferableObj.CurrentRoomTrigger;
							_transferableObj.CurrentRoomTrigger = sceneTriggerRoom;
							_transferableObj.RoomChanged(previousRoomTrigger);
						}

						return;
					}

					if (_transferableObj.CurrentRoomTrigger != null)
					{
						SceneTriggerRoom currentRoomTrigger2 = _transferableObj.CurrentRoomTrigger;
						_transferableObj.CurrentRoomTrigger = null;
						_transferableObj.RoomChanged(currentRoomTrigger2);
						_transferableObj.ExitVessel(forceExit: false);
					}
					CheckMyPlayerCanGrabWall();
				}
			}
			else
			{
				SceneTriggerLadder ladderTrigger = coli.GetComponentInParent<SceneTriggerLadder>();
				if (_transferableObj is not MyPlayer myPlayer || ladderTrigger == null)
				{
					return;
				}
				if (myPlayer.FpsController.IsOnLadder)
				{
					ladderTrigger.LadderDetach(MyPlayer.Instance);
				}
				myPlayer.InLadderTrigger = false;
				myPlayer.LadderTrigger = null;
			}
		}

		public void ResetTriggers()
		{
			_roomTriggersDict = new Dictionary<SceneTriggerRoom, HashSet<Collider>>();
		}

		public void ExitTriggers(Collider[] colliders)
		{
			foreach (Collider coli in colliders)
			{
				SceneTriggerRoom roomTrigger = GetRoomTrigger(coli);
				if (roomTrigger != null && _roomTriggersDict.ContainsKey(roomTrigger))
				{
					OnTriggerExit(coli);
				}
			}
		}

		public void SetTransferableObject(SpaceObjectTransferable transObj)
		{
			_transferableObj = transObj;
		}

		public void CheckMyPlayerCanGrabWall()
		{
			Collider[] hitColliders = new Collider[2];
			var size = Physics.OverlapSphereNonAlloc(transform.position, 0.5f, hitColliders, LayerMask.NameToLayer("Default") | LayerMask.NameToLayer("Player"));
		}

		private void AuxCheckRoomTriggers()
		{
			if (_sphereCollider == null || !Enabled)
			{
				return;
			}

			Collider firstCollider = Physics.OverlapSphere(transform.position, _sphereCollider.radius)
				.FirstOrDefault((Collider m) => GetRoomTrigger(m) != null);
			if (firstCollider == null)
			{
				if (TransferableObject.CurrentRoomTrigger != null || _transferableObj.Parent is not Pivot)
				{
					if (TransferableObject.CurrentRoomTrigger != null)
					{
						SceneTriggerRoom previousRoomTrigger = TransferableObject.CurrentRoomTrigger;
						TransferableObject.CurrentRoomTrigger = null;
						TransferableObject.RoomChanged(previousRoomTrigger);
					}
					TransferableObject.ExitVessel(forceExit: true);
					ResetTriggers();
				}
				else if (_roomTriggersDict.Count > 0)
				{
					ResetTriggers();
				}
			}
			else if (TransferableObject.CurrentRoomTrigger == null)
			{
				OnTriggerEnter(firstCollider);
			}
		}
	}
}
