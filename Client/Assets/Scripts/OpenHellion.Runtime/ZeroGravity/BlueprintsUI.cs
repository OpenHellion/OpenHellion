using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ZeroGravity.Data;
using ZeroGravity.Objects;
using ZeroGravity.UI;

namespace ZeroGravity
{
	public class BlueprintsUI : MonoBehaviour
	{
		public List<GameObject> Options;

		public Transform BlueprintsHolder;

		public GameObject BlueprintUI;

		[NonSerialized] public BlueprintItemUI Current;

		public Transform TiersHolder;

		public GameObject TierPrefab;

		public GameObject CurrentHolder;

		public Image Icon;

		public Text Name;

		public Text Description;

		public Text TiersDescription;

		public Text Researched;

		public Image ResFiller;

		public void Toggle(bool val)
		{
			gameObject.SetActive(val);
			if (val)
			{
				SetScreen(0);
				Current = null;
				SetCurrent();
			}
		}

		public void SetScreen(int scr)
		{
			foreach (GameObject option in Options)
			{
				option.SetActive(value: false);
			}

			Options[scr].SetActive(value: true);
			switch (scr)
			{
				case 0:
					InstantiateAll(ItemCategory.Weapons);
					break;
				case 1:
					InstantiateAll(ItemCategory.Tools);
					break;
				case 2:
					InstantiateAll(ItemCategory.Utility);
					break;
				case 3:
					InstantiateAll(ItemCategory.Suits);
					break;
				case 4:
					InstantiateAll(ItemCategory.Medical);
					break;
				case 5:
					InstantiateAll(ItemCategory.Parts);
					break;
				case 6:
					InstantiateAll(ItemCategory.Containers);
					break;
				case 7:
					InstantiateAll(ItemCategory.Magazines);
					break;
				case 8:
					InstantiateAll(ItemCategory.General);
					break;
				default:
					InstantiateAll(ItemCategory.General);
					break;
			}

			CountResearched();
		}

		public void InstantiateAll(ItemCategory cat)
		{
			BlueprintsHolder.DestroyAll<BlueprintItemUI>();
			foreach (DynamicObjectData item in StaticData.DynamicObjectsDataList.Values.Where((DynamicObjectData m) =>
				         Item.GetCraftingResources(MyPlayer.Instance.World, m.CompoundType) != null && m.DefaultAuxData.Category == cat))
			{
				ItemCompoundType compoundType = item.CompoundType;
				GameObject gameObject = Instantiate(BlueprintUI, BlueprintsHolder);
				gameObject.transform.localScale = Vector3.one;
				BlueprintItemUI component = gameObject.GetComponent<BlueprintItemUI>();
				component.Screen = this;
				component.gameObject.transform.localScale = Vector3.one;
				component.CompoundType = compoundType;
				component.InstantiateTiers(inst: false);
			}
		}

		public void SetCurrent()
		{
			TiersHolder.DestroyAll(childrenOnly: true);
			if (Current != null)
			{
				CurrentHolder.Activate(value: true);
				Name.text = Item.GetName(Current.CompoundType);
				Icon.sprite = SpriteManager.Instance.GetSprite(Current.CompoundType);
				Description.text = Item.GetDescription(Current.CompoundType.Type, Current.CompoundType.SubType,
					Current.CompoundType.PartType);
				Current.InstantiateTiers(inst: true);
			}
			else
			{
				CurrentHolder.Activate(value: false);
			}
		}

		public void CountResearched()
		{
			float num = StaticData.DynamicObjectsDataList.Values
				.Where((DynamicObjectData m) => Item.GetCraftingResources(MyPlayer.Instance.World, m.CompoundType) != null).Sum(
					(DynamicObjectData m) => (m.DefaultAuxData.TierMultipliers.Length < 2)
						? 1
						: m.DefaultAuxData.TierMultipliers.Length);
			float num2 = MyPlayer.Instance.Blueprints.Count();
			Researched.text = num2 + " / " + num;
			ResFiller.fillAmount = num2 / num;
		}
	}
}
