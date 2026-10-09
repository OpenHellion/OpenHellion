using System;
using System.Linq;
using OpenHellion;
using UnityEngine;
using ZeroGravity.Data;
using ZeroGravity.LevelDesign;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity.ShipComponents
{
	public abstract class VesselComponent : MonoBehaviour, ISceneObject
	{
		[SerializeField] private int _inSceneID;

		protected World World => _ParentVessel.World;

		[Space(5f)] public SceneMachineryPartSlot[] MachineryPartSlots = new SceneMachineryPartSlot[0];

		protected SpaceObjectVessel _ParentVessel;

		public int InSceneID
		{
			get { return _inSceneID; }
			set { _inSceneID = value; }
		}

		public abstract VesselComponentType ComponentType { get; }

		public SpaceObjectVessel ParentVessel => _ParentVessel;

		public virtual SystemAuxData GetAuxData()
		{
			return null;
		}

		public virtual IAuxDetails GetAuxDetails()
		{
			return null;
		}

		public float GetScopeMultiplier(MachineryPartSlotScope scope, out int slots, out int parts,
			out int workingParts)
		{
			float num = 1f;
			parts = 0;
			workingParts = 0;
			slots = 0;
			foreach (SceneMachineryPartSlot item in MachineryPartSlots.Where((SceneMachineryPartSlot m) =>
				         m.Scope == scope))
			{
				slots++;
				MachineryPart componentInChildren = item.GetComponentInChildren<MachineryPart>();
				if (componentInChildren != null)
				{
					num *= componentInChildren.TierMultiplier;
					parts++;
					if (componentInChildren.Health > float.Epsilon)
					{
						workingParts++;
					}
				}
			}

			return num;
		}

		protected virtual void Start()
		{
			SceneMachineryPartSlot[] machineryPartSlots = MachineryPartSlots;
			foreach (SceneMachineryPartSlot sceneMachineryPartSlot in machineryPartSlots)
			{
				sceneMachineryPartSlot.ParentVesselComponent = this;
			}
		}

		public virtual void MachineryPartAttached(SceneMachineryPartSlot slot)
		{
		}

		public virtual void MachineryPartDetached(SceneMachineryPartSlot slot)
		{
		}
	}
}
