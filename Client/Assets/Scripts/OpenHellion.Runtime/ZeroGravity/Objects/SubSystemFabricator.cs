using System;
using System.Collections.Generic;
using System.Linq;
using OpenHellion.Net;
using UnityEngine;
using ZeroGravity.Data;
using ZeroGravity.LevelDesign;
using ZeroGravity.Network;
using ZeroGravity.ShipComponents;

namespace ZeroGravity.Objects
{
	public class SubSystemFabricator : SubSystem, IPowerConsumer, ICargo
	{
		public class CraftableItemData
		{
			public short ItemID;

			public ItemCompoundType CompoundType;

			public Dictionary<ResourceType, float> Resources;
		}

		[SerializeField] private string _Name = "Fabricator";

		[SerializeField] private ResourceRequirement[] _ResourceRequirements = new ResourceRequirement[1]
		{
			new ResourceRequirement
			{
				ResourceType = DistributionSystemType.Power
			}
		};

		public float TimePerResourceUnit = 1f;

		public List<ItemCompoundType> AllowedItemTypes;

		public List<SceneAttachPoint> AttachPoints;

		[NonSerialized] public List<ItemCompoundType> ItemsInQueue;

		public float CurrentTimeLeft;

		[NonSerialized] public float TotalTimeLeft;

		public CargoCompartment CargoResources;

		private List<ICargoCompartment> _Compartments;

		public List<CraftableItemData> CraftableItems;

		public Animator FabricatorAnimator;

		private bool initialized;

		public override SubSystemType Type => SubSystemType.Fabricator;

		public string Name => _Name;

		public override ResourceRequirement[] ResourceRequirements => _ResourceRequirements;

		public List<ICargoCompartment> Compartments => _Compartments;

		protected override void Start()
		{
			base.Start();
			_Compartments = new List<ICargoCompartment> { CargoResources };
			CraftableItems = GetCraftableItems();
		}

		public ICargoCompartment GetCompartment(short? id = null)
		{
			if (id.HasValue)
			{
				return _Compartments.Find((ICargoCompartment m) => m.ID == id.Value);
			}

			return _Compartments[0];
		}

		public override SystemAuxData GetAuxData()
		{
			SubSystemFabricatorAuxData subSystemFabricatorAuxData = new SubSystemFabricatorAuxData();
			subSystemFabricatorAuxData.TimePerResourceUnit = TimePerResourceUnit;
			subSystemFabricatorAuxData.AllowedItemTypes = AllowedItemTypes;
			subSystemFabricatorAuxData.AttachPoints = (from m in AttachPoints
				where m != null
				select m.InSceneID).ToList();
			subSystemFabricatorAuxData.CargoCompartments = new List<CargoCompartmentData>
				{ !(CargoResources == null) ? CargoResources.GetData() : null };
			return subSystemFabricatorAuxData;
		}

		public override void SetDetails(SubSystemDetails details, bool instant = false)
		{
			base.SetDetails(details, instant);
			if (Status == SystemStatus.Online)
			{
				FabricatorAnimator.SetBool("Work", value: true);
				FabricatorAnimator.SetBool("Initialized", initialized);
			}
			else
			{
				FabricatorAnimator.SetBool("Work", value: false);
				FabricatorAnimator.SetBool("Initialized", initialized);
			}

			initialized = true;
		}

		public override void SetAuxDetails(IAuxDetails auxDetails)
		{
			base.SetAuxDetails(auxDetails);
			FabricatorAuxDetails fabricatorAuxDetails = auxDetails as FabricatorAuxDetails;
			ItemsInQueue = fabricatorAuxDetails.ItemsInQueue;
			CurrentTimeLeft = fabricatorAuxDetails.CurrentTimeLeft;
			TotalTimeLeft = fabricatorAuxDetails.TotalTimeLeft;
			if (_Compartments == null)
			{
				_Compartments = new List<ICargoCompartment> { CargoResources };
			}

			foreach (CargoCompartmentDetails ccd in fabricatorAuxDetails.CargoCompartments)
			{
				ICargoCompartment cargoCompartment = _Compartments.Find((ICargoCompartment m) => m.ID == ccd.ID);
				if (cargoCompartment != null)
				{
					cargoCompartment.Resources = ccd.Resources;
				}
			}
		}

		public void FabricateItem(ItemType type, GenericItemSubType subType, MachineryPartType partType, int tier)
		{
			NetworkController.SendAndForget(new FabricateItemMessage
			{
				ID = new VesselObjectID(ParentVessel.Guid, InSceneID),
				ItemType = new ItemCompoundType
				{
					Type = type,
					SubType = subType,
					PartType = partType,
					Tier = tier
				}
			});
		}

		public void CancelFabrication(bool currentItemOnly = false)
		{
			NetworkController.SendAndForget(new CancelFabricationMessage
			{
				ID = new VesselObjectID(ParentVessel.Guid, InSceneID),
				CurrentItemOnly = currentItemOnly
			});
		}

		public bool HasEnoughResources(ItemType itemType, GenericItemSubType subType, MachineryPartType partType,
			int tier)
		{
			Dictionary<ResourceType, float> craftingResources =
				Item.GetCraftingResources(World, itemType, subType, partType, tier);
			foreach (KeyValuePair<ResourceType, float> kv in craftingResources)
			{
				CargoResourceData cargoResourceData = GetCompartment().Resources
					.FirstOrDefault((CargoResourceData m) => m.ResourceType == kv.Key);
				if (cargoResourceData == null || cargoResourceData.Quantity < kv.Value)
				{
					return false;
				}
			}

			return true;
		}

		private List<CraftableItemData> GetCraftableItems()
		{
			List<CraftableItemData> list = new List<CraftableItemData>();
			foreach (DynamicObjectData value in StaticData.DynamicObjectsDataList.Values)
			{
				ItemType type = value.ItemType;
				GenericItemSubType subType = GenericItemSubType.None;
				MachineryPartType partType = MachineryPartType.None;
				if (value.ItemType == ItemType.GenericItem)
				{
					subType = (value.DefaultAuxData as GenericItemData).SubType;
				}
				else if (value.ItemType == ItemType.MachineryPart)
				{
					partType = (value.DefaultAuxData as MachineryPartData).PartType;
				}

				if (AllowedItemTypes.Count > 0 && AllowedItemTypes.FirstOrDefault((ItemCompoundType m) =>
					    m.Type == type && m.SubType == subType && m.PartType == partType) == null)
				{
					continue;
				}

				int index = 1;
				List<int> list2 = value.DefaultAuxData.TierMultipliers.Select((float m) => index++).ToList();
				if (list2.Count == 0)
				{
					list2.Add(1);
				}

				foreach (int item in list2)
				{
					Dictionary<ResourceType, float> craftingResources =
						Item.GetCraftingResources(World, type, subType, partType, item);
					ItemCompoundType itemCompoundType = new ItemCompoundType();
					itemCompoundType.Type = type;
					itemCompoundType.SubType = subType;
					itemCompoundType.PartType = partType;
					itemCompoundType.Tier = item;
					ItemCompoundType compoundType = itemCompoundType;
					int? num = craftingResources?.Count;
					if (num.HasValue && num.GetValueOrDefault() > 0)
					{
						list.Add(new CraftableItemData
						{
							ItemID = value.ItemID,
							CompoundType = compoundType,
							Resources = craftingResources
						});
					}
				}
			}

			return (from m in list
				orderby m.CompoundType.Type != ItemType.MachineryPart, m.CompoundType.Type == ItemType.GenericItem
				select m).ToList();
		}
	}
}
