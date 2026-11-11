using UnityEngine;
using UnityEngine.UI;
using ZeroGravity.Objects;

namespace ZeroGravity.UI
{
	public class TargetOverlayUI : MonoBehaviour
	{

		public TargetObject Target;

		public Text Name;

		public Text Distance;

		public GameObject NameHolder;

		public GameObject Default;

		public GameObject Hovered;

		public GameObject Selected;

		public OffScreenTargetArrow OffScreenTarget;

		public GameObject Direction;
	}
}
