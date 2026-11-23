using UnityEngine;
using UnityEngine.UI;
using ZeroGravity.Objects;

namespace ZeroGravity.UI
{
	public class TaskUI : MonoBehaviour
	{
		public Text Name;

		public Text Description;

		public Animator Animator;

		public GameObject CompletedObject;

		public QuestTrigger NextTrigger;

		public void OnCompletedTask()
		{
			gameObject.SetActive(false);
		}

		public void OnHideTask()
		{
			gameObject.SetActive(false);
		}
	}
}
