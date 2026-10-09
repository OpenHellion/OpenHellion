using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using ZeroGravity.Data;
using ZeroGravity.Objects;
using ZeroGravity.UI;

namespace ZeroGravity.LevelDesign
{
	public class SceneTriggerAirlockPanel : SceneTriggerPanels
	{
		[FormerlySerializedAs("_cancelExecuterAtSameTime")] [SerializeField] private bool _cancelExecutorAtSameTime;

		[SerializeField] private bool _isExteriorTrigger;

		private AirLockControls myAirLockControls;

		public override SceneTriggerType TriggerType => SceneTriggerType.AirlockPanel;

		public override PlayerHandsCheckType PlayerHandsCheck => PlayerHandsCheckType.StoreItemInHands;

		public override List<ItemType> PlayerHandsItemType => null;

		public override bool IsNearTrigger => true;

		public override bool IsInteractable => true;

		public bool CancelExecutorAtSameTime => _cancelExecutorAtSameTime;

		public bool IsExteriorTrigger => _isExteriorTrigger;

		public override bool CameraMovementAllowed => false;

		public AirLockControls MyAirLockControls
		{
			get
			{
				if (myAirLockControls == null)
				{
					myAirLockControls = GetComponent<AirLockControls>();
				}

				return myAirLockControls;
			}
		}

		public AirlockUI AirlockUI => World.InWorldPanels.Airlock;

		private void Awake()
		{
			if (CheckAuthorizationType == AuthorizationType.None)
			{
				CheckAuthorizationType = AuthorizationType.AuthorizedOrNoSecurity;
			}
		}

		public override bool Interact(MyPlayer player, bool interactWithOverlappingTriggers = true)
		{
			if (player.CurrentActiveItem != null && (player.Inventory == null || !player.Inventory.StoreItemInHands()))
			{
				return false;
			}

			player.AttachToPanel(this);
			AirlockUI.MyAirlock = MyAirLockControls;
			AirlockUI.OnInteract();
			return true;
		}

		public override void CancelInteract(MyPlayer player)
		{
			base.CancelInteract(player);
			player.DetachFromPanel();
			AirlockUI.OnDetach();
		}
	}
}
