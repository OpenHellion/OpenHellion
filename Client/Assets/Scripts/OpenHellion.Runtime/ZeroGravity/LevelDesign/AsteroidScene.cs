using System;
using System.Collections.Generic;
using UnityEngine;
using ZeroGravity.Data;

namespace ZeroGravity.LevelDesign
{
	public class AsteroidScene : MonoBehaviour
	{
		[Tooltip("Unique ID for asteroid. ID will be populated automatially on save")]
		public long GUID;

		[Tooltip("Scene name used in game")] public string GameName;

		[Tooltip("Scene mass in tons")] public float Mass;

		private float _radius;

		public GameObject ExteriorObject;

		public float RelativeDensity = 2.5f;

		public bool SaveSceneData(out string error, List<CelestialSceneData> asteroids = null)
		{
			error = string.Empty;
			return true;
		}

		private void Awake()
		{
			gameObject.SetActive(false);
		}

		public float GetMass()
		{
			float triggerVolume = 0f;
			SceneTriggerRoom[] componentsInChildren = gameObject.GetComponentsInChildren<SceneTriggerRoom>();
			foreach (SceneTriggerRoom sceneTriggerRoom in componentsInChildren)
			{
				triggerVolume += sceneTriggerRoom.Volume;
			}

			float objectVolume = 0f;
			if (ExteriorObject != null)
			{
				objectVolume = SceneHelper.VolumeOfGameObject(ExteriorObject);
				if (objectVolume <= 0f)
				{
					Debug.LogWarning("Unable to calculate volume of '" + ExteriorObject.name + "'");
					ExteriorObject = null;
				}
			}

			if (objectVolume <= float.Epsilon || objectVolume < triggerVolume)
			{
				SceneColliderShipCollision[] componentsInChildren2 =
					gameObject.GetComponentsInChildren<SceneColliderShipCollision>();
				foreach (SceneColliderShipCollision sceneColliderShipCollision in componentsInChildren2)
				{
					objectVolume += SceneHelper.VolumeOfGameObject(sceneColliderShipCollision.gameObject);
				}
			}

			float num3 = (objectVolume - triggerVolume) * RelativeDensity;
			if (num3 <= 0f)
			{
				throw new Exception(
					"Error calculating structure mass (ExteriorObject field invalid OR room triggers summary volume too big OR ship colliders invalid)");
			}

			return num3;
		}
	}
}
