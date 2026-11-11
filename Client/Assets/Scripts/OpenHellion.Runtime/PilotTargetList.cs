using System.Collections.Generic;
using OpenHellion;
using UnityEngine;
using UnityEngine.UI;
using ZeroGravity;
using ZeroGravity.ShipComponents;
using ZeroGravity.UI;

public class PilotTargetList : MonoBehaviour
{
	public Transform TargetListHolder;

	public TargetInListUI TargetListUI;

	public Text RadarRangeCurrent;

	public Text SelectionLabel;

	public GameObject NotActive;


	private void Start()
	{
		NotActive.SetActive(true);
		SelectionLabel.text = Localization.Selection.ToUpper();
	}

	public void CreateTargetInList(TargetObject target)
	{
		TargetInListUI targetInListUI = Instantiate(TargetListUI, TargetListHolder);
		targetInListUI.gameObject.transform.localScale = Vector3.one;
		targetInListUI.gameObject.SetActive(true);
		targetInListUI.Target = target;
		targetInListUI.Name.text = target.Name;
		targetInListUI.Icon.sprite = target.Icon;
	}

	public void UpdateTargetList(World world)
	{
		List<TargetInListUI> list = new List<TargetInListUI>();
		TargetInListUI[] componentsInChildren = TargetListHolder.GetComponentsInChildren<TargetInListUI>(true);
		foreach (TargetInListUI item in componentsInChildren)
		{
			list.Add(item);
		}

		foreach (TargetInListUI item2 in list)
		{
			if (world.InWorldPanels.Pilot.SelectedTarget != null)
			{
				item2.Selected.SetActive(world.InWorldPanels.Pilot.SelectedTarget == item2.Target);
			}

			item2.Distance.text = FormatHelper.DistanceFormat(item2.Target.Distance);
		}

		GoToCurrentElement(world);
	}

	private void GoToCurrentElement(World world)
	{
		int num = world.InWorldPanels.Pilot.AllTargets.IndexOf(world.InWorldPanels.Pilot.SelectedTarget);
		if (world.InWorldPanels.Pilot.AllTargets.Count > 6)
		{
			float y = 70f * num;
			TargetListHolder.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, y);
		}
		else if (world.InWorldPanels.Pilot.AllTargets.Count == 0)
		{
			TargetListHolder.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 0f);
		}
	}

	public void ToggleTargetList(bool toggle)
	{
		NotActive.SetActive(toggle);
	}
}
