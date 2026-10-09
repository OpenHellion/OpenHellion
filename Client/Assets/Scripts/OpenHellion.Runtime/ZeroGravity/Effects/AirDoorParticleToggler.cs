using UnityEngine;

namespace ZeroGravity.Effects
{
	public class AirDoorParticleToggler : MonoBehaviour
	{
		public void Play()
		{
			GetComponent<ParticleSystem>().Play();
		}
	}
}
