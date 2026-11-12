using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZeroGravity.Data;
using ZeroGravity.Objects;

namespace ZeroGravity.LevelDesign
{
	public class StructureScene : MonoBehaviour
	{
		public class SceneStatisticData
		{
			public int NumberOfColliders;

			public int NumberOfDontOptimizeColliders;

			public int NumberOfMeshColliders;

			public int NumberOfMeshCollidersConcave;

			public int NumberOfMeshCollidersConvex;

			public int NumberOfPrimitiveColliders;

			public int NumberOfTriggers;

			public int NumberOfDontOptimizeTriggers;

			public int NumberOfMeshTriggers;

			public int NumberOfPrimitiveTriggers;

			public int NumberOfRigidbodies;

			public int NumberOfGameObjects;

			public void Reset()
			{
				NumberOfColliders = 0;
				NumberOfDontOptimizeColliders = 0;
				NumberOfMeshColliders = 0;
				NumberOfMeshCollidersConcave = 0;
				NumberOfMeshCollidersConvex = 0;
				NumberOfPrimitiveColliders = 0;
				NumberOfTriggers = 0;
				NumberOfDontOptimizeTriggers = 0;
				NumberOfMeshTriggers = 0;
				NumberOfPrimitiveTriggers = 0;
				NumberOfRigidbodies = 0;
				NumberOfGameObjects = 0;
			}

			public void IncrementCollider(bool isTrigger, bool isDontOptimize, bool isMesh, bool isConvex)
			{
				if (isTrigger)
				{
					NumberOfTriggers++;
					NumberOfDontOptimizeTriggers += (isDontOptimize ? 1 : 0);
					NumberOfMeshTriggers += (isMesh ? 1 : 0);
					NumberOfPrimitiveTriggers += ((!isMesh) ? 1 : 0);
				}
				else
				{
					NumberOfColliders++;
					NumberOfDontOptimizeColliders += (isDontOptimize ? 1 : 0);
					NumberOfMeshColliders += (isMesh ? 1 : 0);
					NumberOfPrimitiveColliders += ((!isMesh) ? 1 : 0);
					NumberOfMeshCollidersConvex += ((isMesh && isConvex) ? 1 : 0);
					NumberOfMeshCollidersConcave += ((isMesh && !isConvex) ? 1 : 0);
				}
			}

			public void IncrementRigidbody()
			{
				NumberOfRigidbodies++;
			}

			public void IncrementGameObjectsCount()
			{
				NumberOfGameObjects++;
			}
		}

		[Tooltip("Unique ID for structure. ID will be populated automatially on save")]
		public long GUID;

		[Tooltip("Scene name used in game")] public string GameName;

		[Tooltip("Scene mass in tons")] public float Mass;

		public float HeatCollectionFactor = 10000f;

		public float HeatDissipationFactor = 20f;

		[Space(10f)] [SerializeField] private float _MaxHealth = 10000f;

		[SerializeField] private float _Health = 10000f;

		public float BaseArmor;

		public bool InvulnerableWhenDocked;

		public AnimationCurve DamageEffectsFrequency =
			new AnimationCurve(new Keyframe(0f, 5f), new Keyframe(0.3f, 0f), new Keyframe(1f, 0f));

		[Space(10f)] public SceneSpawnSettings[] SpawnSettings;

		[Space(10f)] public GameObject HullExterior;

		public float RelativeDensity = 0.5f;

		[Space(10f)] public TagChance[] AdditionalTags;

		public SceneStatisticData Statistic;

		public float MaxHealth
		{
			get => _MaxHealth;
			set => _MaxHealth = Mathf.Clamp(value, 0f, float.MaxValue);
		}

		public float Health
		{
			get => _Health;
			set => _Health = Mathf.Clamp(value, 0f, MaxHealth);
		}

		public bool SaveSceneData(out string error, List<StructureSceneData> structures = null,
			bool isAutoSaveAll = false)
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
			float num = 0f;
			SceneTriggerRoom[] componentsInChildren = gameObject.GetComponentsInChildren<SceneTriggerRoom>();
			foreach (SceneTriggerRoom sceneTriggerRoom in componentsInChildren)
			{
				num += sceneTriggerRoom.Volume;
			}

			float num2 = 0f;
			if (HullExterior != null)
			{
				num2 = SceneHelper.VolumeOfGameObject(HullExterior);
				if (num2 <= 0f)
				{
					Debug.LogWarning("Unable to calculate volume of '" + HullExterior.name + "'");
					HullExterior = null;
				}
			}

			if (num2 <= float.Epsilon || num2 < num)
			{
				SceneColliderShipCollision[] componentsInChildren2 =
					gameObject.GetComponentsInChildren<SceneColliderShipCollision>();
				foreach (SceneColliderShipCollision sceneColliderShipCollision in componentsInChildren2)
				{
					num2 += SceneHelper.VolumeOfGameObject(sceneColliderShipCollision.gameObject);
				}
			}

			float num3 = (num2 - num) * RelativeDensity;
			if (num3 <= 0f)
			{
				throw new Exception(
					"Error calculating structure mass (HullExterior field invalid OR room triggers summary volume too big OR ship colliders invalid)");
			}

			return num3;
		}

		private void ParseTransformStatistic(ref SceneStatisticData stats, Transform trans)
		{
			if (trans.GetComponent<DynamicSceneObject>() != null || trans.GetComponent<DynamicObject>() != null ||
			    trans.gameObject.layer == LayerMask.NameToLayer("ShipCollision"))
			{
				return;
			}

			stats.IncrementGameObjectsCount();
			Collider[] components = trans.GetComponents<Collider>();
			foreach (Collider collider in components)
			{
				stats.IncrementCollider(collider.isTrigger, collider.tag == "DontOptimize", collider is MeshCollider,
					collider is MeshCollider && (collider as MeshCollider).convex);
			}

			Rigidbody[] components2 = trans.GetComponents<Rigidbody>();
			foreach (Rigidbody rigidbody in components2)
			{
				stats.IncrementRigidbody();
			}

			IEnumerator enumerator = trans.transform.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					Transform trans2 = (Transform)enumerator.Current;
					ParseTransformStatistic(ref stats, trans2);
				}
			}
			finally
			{
				IDisposable disposable;
				if ((disposable = enumerator as IDisposable) != null)
				{
					disposable.Dispose();
				}
			}
		}

		public void ReadSceneStatistic()
		{
			if (Statistic == null)
			{
				Statistic = new SceneStatisticData();
			}
			else
			{
				Statistic.Reset();
			}

			IEnumerator enumerator = transform.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					Transform trans = (Transform)enumerator.Current;
					ParseTransformStatistic(ref Statistic, trans);
				}
			}
			finally
			{
				IDisposable disposable;
				if ((disposable = enumerator as IDisposable) != null)
				{
					disposable.Dispose();
				}
			}
		}
	}
}
