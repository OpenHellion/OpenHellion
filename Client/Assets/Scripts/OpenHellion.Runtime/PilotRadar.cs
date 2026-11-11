using UnityEngine;
using UnityEngine.UI;
using ZeroGravity;
using ZeroGravity.ShipComponents;
using ZeroGravity.UI;

public class PilotRadar : MonoBehaviour
{
	public Transform Root;

	public RadarShipElement RadarElement;

	public GameObject NotActive;

	public Text PilotingNotActiveLabel;

	private void Start()
	{
		PilotingNotActiveLabel.text = Localization.PilotingNotActive.ToUpper();
		NotActive.SetActive(true);
	}

	public void CreateRadarTarget(PilotOverlayUI pilot, TargetObject target)
	{
		if (!(target.Distance > 10000f))
		{
			RadarShipElement radarShipElement = Instantiate(RadarElement, Root);
			radarShipElement.PilotP = pilot;
			radarShipElement.gameObject.transform.localScale = Vector3.one;
			radarShipElement.gameObject.SetActive(true);
			radarShipElement.Target = target;
			radarShipElement.Icon.sprite = target.Icon;
		}
	}

	public void UpdateRadar(TargetObject selectedTarget)
	{
		RadarShipElement[] radarElementsInChildren = Root.GetComponentsInChildren<RadarShipElement>(true);
		foreach (RadarShipElement radarElement in radarElementsInChildren)
		{
			radarElement.Selected.SetActive(selectedTarget != null && selectedTarget == radarElement.Target);
		}
	}

	public void ToggleRadarScreen(bool toggle)
	{
		NotActive.SetActive(toggle);
	}
}
