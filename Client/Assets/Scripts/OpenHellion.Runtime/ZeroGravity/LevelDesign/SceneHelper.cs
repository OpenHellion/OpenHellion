using System;
using System.Collections.Generic;
using System.Linq;
using OpenHellion;
using UnityEngine;
using ZeroGravity.Data;
using ZeroGravity.Effects;
using ZeroGravity.Network;
using ZeroGravity.Objects;
using ZeroGravity.ShipComponents;

namespace ZeroGravity.LevelDesign
{
	public static class SceneHelper
	{
		public static List<StructureSceneConnectionData> GetStructureConnectionData(GameObject rootObject)
		{
			return new List<StructureSceneConnectionData>();
		}

		public static List<SpawnPointData> GetSpawnPointsData(GameObject rootObject)
		{
			return new List<SpawnPointData>();
		}

		public static List<SubSystemData> GetSubSystemsData(GameObject rootObject)
		{
			return new List<SubSystemData>();
		}

		public static List<GeneratorData> GetGeneratorsData(GameObject rootObject)
		{
			return new List<GeneratorData>();
		}

		public static List<NameTagData> GetNameTagsData(GameObject rootObject)
		{
			return new List<NameTagData>();
		}

		public static List<VesselRepairPointData> GetVesselRepaitPointsData(GameObject rootObject)
		{
			return new List<VesselRepairPointData>();
		}

		public static List<AsteroidMiningPointData> GetMiningPointsData(GameObject rootObject)
		{
			return new List<AsteroidMiningPointData>();
		}

		public static List<ResourceContainerData> GetResourceContainersData(GameObject rootObject)
		{
			return new List<ResourceContainerData>();
		}

		public static List<DynamicObjectSceneData> GetDynamicObjectsData(GameObject rootObject)
		{
			return new List<DynamicObjectSceneData>();
		}

		public static List<BaseAttachPointData> GetAttachPointData(GameObject rootObject)
		{
			return new List<BaseAttachPointData>();
		}

		public static List<SceneDockingPortData> GetSceneDockingPortData(GameObject rootObject)
		{
			return new List<SceneDockingPortData>();
		}

		public static List<SpawnObjectsWithChanceData> GetSpawnObjectsWithChanceData(GameObject rootObject)
		{
			return new List<SpawnObjectsWithChanceData>();
		}

		public static CargoBayData GetSceneCargoBayData(GameObject rootObject)
		{
			return null;
		}

		public static List<RoomData> GetRoomTriggersData(GameObject rootObject)
		{
			return new List<RoomData>();
		}

		public static float VolumeOfGameObject(GameObject gobj)
		{
			bool flag = gobj.GetComponent<SceneTriggerRoom>() != null;
			float num = 0f;
			Collider[] componentsInChildren = gobj.GetComponentsInChildren<Collider>();
			foreach (Collider collider in componentsInChildren)
			{
				if (collider != null && (!flag || (flag &&
				                                   (collider.gameObject.GetComponent<SceneTriggerRoomSegment>() !=
					                                   null || collider.gameObject == gobj))))
				{
					MeshFilter component = collider.GetComponent<MeshFilter>();
					if (component == null || collider is BoxCollider)
					{
						Vector3 size = collider.bounds.size;
						num += size.x * size.y * size.z * collider.transform.lossyScale.x *
						       collider.transform.lossyScale.y * collider.transform.lossyScale.z;
					}
					else
					{
						num += VolumeOfMesh(component.sharedMesh) * collider.transform.lossyScale.x *
						       collider.transform.lossyScale.y * collider.transform.lossyScale.z;
					}
				}
			}

			return num;
		}

		public static float SignedVolumeOfTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
		{
			float num = p3.x * p2.y * p1.z;
			float num2 = p2.x * p3.y * p1.z;
			float num3 = p3.x * p1.y * p2.z;
			float num4 = p1.x * p3.y * p2.z;
			float num5 = p2.x * p1.y * p3.z;
			float num6 = p1.x * p2.y * p3.z;
			return 1f / 6f * (0f - num + num2 + num3 - num4 - num5 + num6);
		}

		public static float VolumeOfMesh(Mesh mesh)
		{
			float num = 0f;
			Vector3[] vertices = mesh.vertices;
			int[] triangles = mesh.triangles;
			for (int i = 0; i < mesh.triangles.Length; i += 3)
			{
				Vector3 p = vertices[triangles[i]];
				Vector3 p2 = vertices[triangles[i + 1]];
				Vector3 p3 = vertices[triangles[i + 2]];
				num += SignedVolumeOfTriangle(p, p2, p3);
			}

			return Mathf.Abs(num);
		}

		public static List<DoorData> GetDoorsData(GameObject rootObject,
			string doorAirParticlesPrefabPath = "Assets/Prefabs/FX/Particles/AirDoorParticles.prefab",
			bool isAutoSaveAll = false)
		{
			return new List<DoorData>();
		}

		public static List<SceneTriggerExecutorData> GetSceneTriggerExecutorsData(GameObject rootObject)
		{
			return new List<SceneTriggerExecutorData>();
		}

		public static ServerCollisionData GetCollisionData(GameObject rootObject)
		{
			ServerCollisionData serverCollisionData = new ServerCollisionData();
			serverCollisionData.MeshCollidersData = new List<MeshData>();
			serverCollisionData.PrimitiveCollidersData = new List<PrimitiveColliderData>();
			return serverCollisionData;
		}

		public static void FillSubSystems(GameObject sceneRoot, Dictionary<int, SubSystem> subSystemsDict,
			List<SubSystemDetails> subSystemsDetails)
		{
			SubSystem[] componentsInChildren = sceneRoot.GetComponentsInChildren<SubSystem>();
			foreach (SubSystem subSys in componentsInChildren)
			{
				subSystemsDict[subSys.InSceneID] = subSys;
				subSys.SetParentVessel(sceneRoot.GetComponentInParent<GeometryRoot>().MainObject as SpaceObjectVessel);
				if (subSystemsDetails != null)
				{
					SubSystemDetails subSystemDetails =
						subSystemsDetails.Find((SubSystemDetails m) => m.InSceneID == subSys.InSceneID);
					if (subSystemDetails != null)
					{
						subSys.SetDetails(subSystemDetails, instant: true);
					}
				}
			}
		}

		public static void FillGenerators(GameObject sceneRoot, Dictionary<int, Generator> generatorsDict,
			List<GeneratorDetails> generatorsDetails)
		{
			Generator[] componentsInChildren = sceneRoot.GetComponentsInChildren<Generator>();
			foreach (Generator generator in componentsInChildren)
			{
				generatorsDict[generator.InSceneID] = generator;
				generator.SetParentVessel(
					sceneRoot.GetComponentInParent<GeometryRoot>().MainObject as SpaceObjectVessel);
				if (generatorsDetails != null)
				{
					GeneratorDetails generatorDetails =
						generatorsDetails.Find((GeneratorDetails m) => m.InSceneID == generator.InSceneID);
					if (generatorDetails != null)
					{
						generator.SetDetails(generatorDetails);
					}
				}
			}
		}

		public static void FillRoomTriggers(SpaceObjectVessel vessel, GameObject sceneRoot,
			Dictionary<int, SceneTriggerRoom> roomTriggersDict, List<RoomDetails> roomTriggersDetails)
		{
			SceneTriggerRoom[] componentsInChildren = sceneRoot.GetComponentsInChildren<SceneTriggerRoom>();
			foreach (SceneTriggerRoom str in componentsInChildren)
			{
				roomTriggersDict[str.InSceneID] = str;
				str.ParentVessel = vessel;
				if (roomTriggersDetails != null)
				{
					RoomDetails roomDetails = roomTriggersDetails.Find((RoomDetails m) => m.InSceneID == str.InSceneID);
					if (roomDetails != null)
					{
						str.UseGravity = roomDetails.UseGravity;
						str.AirPressure = roomDetails.AirPressure;
						str.AirQuality = roomDetails.AirQuality;
					}
				}
			}
		}

		public static void FillNameTags(Ship ship, GameObject sceneRoot, Dictionary<int, SceneNameTag> nameTags,
			List<NameTagData> nameTagDetails)
		{
			SceneNameTag[] componentsInChildren =
				sceneRoot.GetComponentsInChildren<SceneNameTag>(includeInactive: true);
			foreach (SceneNameTag obj in componentsInChildren)
			{
				if (obj.Local)
				{
					continue;
				}

				nameTags[obj.InSceneID] = obj;
				obj.ParentVessel = ship;
				if (nameTagDetails != null)
				{
					NameTagData nameTagData = nameTagDetails.Find((NameTagData m) => m.InSceneID == obj.InSceneID);
					if (nameTagData != null)
					{
						obj.NameTagText = nameTagData.NameTagText;
					}
				}
			}
		}

		public static void FillRepairPoints(Ship ship, GameObject sceneRoot,
			Dictionary<int, VesselRepairPoint> repairPoints, List<VesselRepairPointDetails> repairPointsDetails)
		{
			VesselRepairPoint[] componentsInChildren =
				sceneRoot.GetComponentsInChildren<VesselRepairPoint>(includeInactive: true);
			foreach (VesselRepairPoint obj in componentsInChildren)
			{
				repairPoints[obj.InSceneID] = obj;
				obj.ParentVessel = ship;
				if (repairPointsDetails != null)
				{
					VesselRepairPointDetails vesselRepairPointDetails =
						repairPointsDetails.Find((VesselRepairPointDetails m) => m.InSceneID == obj.InSceneID);
					if (vesselRepairPointDetails != null)
					{
						obj.MaxHealth = vesselRepairPointDetails.MaxHealth;
						obj.Health = vesselRepairPointDetails.Health;
						obj.SecondaryDamageActive = vesselRepairPointDetails.SecondaryDamageActive;
					}
				}
			}
		}

		public static void FillMiningPoints(Asteroid asteroid, GameObject sceneRoot,
			Dictionary<int, AsteroidMiningPoint> repairPoints, AsteroidMiningPointDetails[] miningPointsDetails)
		{
			AsteroidMiningPoint[] componentsInChildren =
				sceneRoot.GetComponentsInChildren<AsteroidMiningPoint>(includeInactive: true);
			foreach (AsteroidMiningPoint obj in componentsInChildren)
			{
				repairPoints[obj.InSceneID] = obj;
				obj.ParentVessel = asteroid;
				if (miningPointsDetails != null)
				{
					AsteroidMiningPointDetails asteroidMiningPointDetails =
						miningPointsDetails.FirstOrDefault((AsteroidMiningPointDetails m) => m.InSceneID == obj.InSceneID);
					if (asteroidMiningPointDetails != null)
					{
						obj.SetDetails(asteroidMiningPointDetails);
					}
				}
			}
		}

		public static void FillResourceContainers(GameObject sceneRoot,
			Dictionary<int, ResourceContainer> resourceContainersDict,
			List<ResourceContainerDetails> resourceContainersDetails)
		{
			ResourceContainer[] componentsInChildren = sceneRoot.GetComponentsInChildren<ResourceContainer>();
			foreach (ResourceContainer rc in componentsInChildren)
			{
				resourceContainersDict[rc.InSceneID] = rc;
				if (resourceContainersDetails != null)
				{
					ResourceContainerDetails resourceContainerDetails =
						resourceContainersDetails.Find((ResourceContainerDetails m) => m.InSceneID == rc.InSceneID);
					if (resourceContainerDetails != null)
					{
						rc.SetDetails(resourceContainerDetails);
					}
				}
			}
		}

		public static void FillDoors(SpaceObjectVessel vessel, GameObject sceneRoot,
			Dictionary<int, SceneDoor> doorsDict, List<DoorDetails> doorsDetails)
		{
			SceneDoor[] componentsInChildren = sceneRoot.GetComponentsInChildren<SceneDoor>();
			foreach (SceneDoor sd in componentsInChildren)
			{
				doorsDict[sd.InSceneID] = sd;
				sd.ParentVessel = vessel;
				if (doorsDetails != null)
				{
					DoorDetails doorDetails = doorsDetails.Find((DoorDetails m) => m.InSceneID == sd.InSceneID);
					if (doorDetails != null)
					{
						sd.SetDoorDetails(doorDetails, isInstant: true);
					}
				}
			}
		}

		public static void FillSceneTriggerExecutors(SpaceObjectVessel vessel, GameObject sceneRoot,
			Dictionary<int, SceneTriggerExecutor> SceneTriggerExecutors,
			List<SceneTriggerExecutorDetails> sceneTriggersExecutorDetails)
		{
			SceneTriggerExecutor[] componentsInChildren =
				sceneRoot.GetComponentsInChildren<SceneTriggerExecutor>(includeInactive: true);
			foreach (SceneTriggerExecutor ste in componentsInChildren)
			{
				SceneTriggerExecutors[ste.InSceneID] = ste;
				ste.ParentVessel = vessel;
				if (sceneTriggersExecutorDetails != null)
				{
					SceneTriggerExecutorDetails sceneTriggerExecutorDetails =
						sceneTriggersExecutorDetails.Find((SceneTriggerExecutorDetails m) =>
							m.InSceneID == ste.InSceneID);
					if (sceneTriggerExecutorDetails != null)
					{
						ste.SetExecutorDetails(sceneTriggerExecutorDetails, isInstant: true);
					}
				}
			}
		}

		public static void FillAttachPoints(SpaceObjectVessel vessel, GameObject sceneRoot,
			Dictionary<int, BaseSceneAttachPoint> attachPointDict, List<AttachPointDetails> AttachPointsDetails)
		{
			BaseSceneAttachPoint[] componentsInChildren = sceneRoot.GetComponentsInChildren<BaseSceneAttachPoint>();
			foreach (BaseSceneAttachPoint bsap in componentsInChildren)
			{
				attachPointDict[bsap.InSceneID] = bsap;
				bsap.ParentVessel = vessel;
				if (AttachPointsDetails != null && bsap is SceneMachineryPartSlot)
				{
					AttachPointDetails attachPointDetails =
						AttachPointsDetails.Find((AttachPointDetails m) => m.InSceneID == bsap.InSceneID);
					if (attachPointDetails != null && attachPointDetails.AuxDetails != null)
					{
						(bsap as SceneMachineryPartSlot).SetActive(
							(attachPointDetails.AuxDetails as MachineryPartSlotAuxDetails).IsActive,
							changeStats: false);
					}
				}
			}
		}

		public static void FillSceneDockingPorts(Ship ship, GameObject sceneRoot,
			Dictionary<int, SceneDockingPort> sceneDockingPorts, List<SceneDockingPortDetails> sceneDockingPortDetails)
		{
			SceneDockingPort[] componentsInChildren =
				sceneRoot.GetComponentsInChildren<SceneDockingPort>(includeInactive: true);
			foreach (SceneDockingPort obj in componentsInChildren)
			{
				sceneDockingPorts[obj.InSceneID] = obj;
				obj.ParentShip = ship;
				if (sceneDockingPortDetails != null)
				{
					SceneDockingPortDetails sceneDockingPortDetails2 =
						sceneDockingPortDetails.Find((SceneDockingPortDetails m) => m.ID.InSceneID == obj.InSceneID);
					if (sceneDockingPortDetails2 != null)
					{
						obj.SetDetails(sceneDockingPortDetails2, isInitialize: true);
					}
				}
			}
		}

		public static void FillSpawnWithChanceData(GameObject sceneRoot,
			List<SpawnObjectsWithChanceDetails> SpawnChanceDetails)
		{
			SpawnObjectsWithChanceScene[] componentsInChildren =
				sceneRoot.GetComponentsInChildren<SpawnObjectsWithChanceScene>();
			foreach (SpawnObjectsWithChanceScene sow in componentsInChildren)
			{
				SpawnObjectsWithChanceDetails spawnObjectsWithChanceDetails =
					SpawnChanceDetails.Find((SpawnObjectsWithChanceDetails m) => m.InSceneID == sow.InSceneID);
				if (spawnObjectsWithChanceDetails != null)
				{
					sow.SetDetails(spawnObjectsWithChanceDetails);
				}
			}
		}

		public static void FillCargoBays(GameObject sceneRoot, Dictionary<int, SceneCargoBay> cargoBaysDict,
			List<CargoBayDetails> cargoBaysDetails)
		{
			SceneCargoBay[] componentsInChildren = sceneRoot.GetComponentsInChildren<SceneCargoBay>();
			foreach (SceneCargoBay cargoBay in componentsInChildren)
			{
				cargoBaysDict[cargoBay.InSceneID] = cargoBay;
				if (cargoBaysDetails != null)
				{
					CargoBayDetails cargoBayDetails =
						cargoBaysDetails.Find((CargoBayDetails m) => m.InSceneID == cargoBay.InSceneID);
					if (cargoBayDetails != null)
					{
						cargoBay.SetDetails(cargoBayDetails);
					}
				}
			}
		}

		public static void FillSpawnPoints(Ship sh, GameObject sceneRoot,
			Dictionary<int, SceneSpawnPoint> sceneSpawnPoints, List<SpawnPointStats> sceneSpawnPointStats)
		{
			SceneSpawnPoint[] componentsInChildren =
				sceneRoot.GetComponentsInChildren<SceneSpawnPoint>(includeInactive: true);
			foreach (SceneSpawnPoint obj in componentsInChildren)
			{
				sceneSpawnPoints[obj.InSceneID] = obj;
				obj.ParentVessel = sh;
				if (sceneSpawnPointStats != null)
				{
					SpawnPointStats spawnPointStats =
						sceneSpawnPointStats.Find((SpawnPointStats m) => m.InSceneID == obj.InSceneID);
					if (spawnPointStats != null)
					{
						obj.SetStats(spawnPointStats);
					}
				}
			}
		}

		public static void FillCubemapProbes(GameObject sceneRoot, World state)
		{
			CubemapProbe[] componentsInChildren = sceneRoot.GetComponentsInChildren<CubemapProbe>();
			foreach (CubemapProbe cubemapProbe in componentsInChildren)
			{
				state.CubemapRenderer.AddReflectionProbe(cubemapProbe.GetComponent<ReflectionProbe>());
			}
		}

		public static void RemoveCubemapProbes(GameObject sceneRoot, World state)
		{
			CubemapProbe[] componentsInChildren = sceneRoot.GetComponentsInChildren<CubemapProbe>();
			foreach (CubemapProbe cubemapProbe in componentsInChildren)
			{
				state.CubemapRenderer.RemoveReflectionProbe(cubemapProbe.GetComponent<ReflectionProbe>());
			}
		}

		public static void SerializeOcclusionObjects(GameObject sceneRoot)
		{
			ZeroOccluder[] componentsInChildren =
				sceneRoot.GetComponentsInChildren<ZeroOccluder>(includeInactive: true);
			foreach (ZeroOccluder zeroOccluder in componentsInChildren)
			{
				zeroOccluder.SerializeOcclusionObjects();
			}
		}

		public static bool CompareTags(string sceneTags, string objectTags)
		{
			if (objectTags == null)
			{
				objectTags = string.Empty;
			}

			if (sceneTags == null)
			{
				sceneTags = string.Empty;
			}

			if (sceneTags == objectTags)
			{
				return true;
			}

			string[] array = objectTags.Split(';');
			string[] array2 = sceneTags.Split(';');
			foreach (string text in array2)
			{
				string[] array3 = array;
				foreach (string text2 in array3)
				{
					if (text == text2)
					{
						return true;
					}
				}
			}

			return false;
		}

		public static void FillEmblems(GameObject sceneRoot, SpaceObjectVessel vessel)
		{
			SceneVesselEmblem[] componentsInChildren =
				sceneRoot.GetComponentsInChildren<SceneVesselEmblem>(includeInactive: true);
			foreach (SceneVesselEmblem item in componentsInChildren)
			{
				vessel.Emblems.Add(item);
			}
		}

		public static void FillDamagePoints(GameObject sceneRoot, SpaceObjectVessel vessel)
		{
			vessel.DamagePoints = new List<DamagePointData>();
			SceneDamagePoint[] componentsInChildren =
				sceneRoot.GetComponentsInChildren<SceneDamagePoint>(includeInactive: true);
			foreach (SceneDamagePoint sceneDamagePoint in componentsInChildren)
			{
				sceneDamagePoint.Effects = sceneDamagePoint.Effects.Where((GameObject m) => m != null).ToList();
				if (sceneDamagePoint.Effects.Count > 0)
				{
					sceneDamagePoint.transform.parent.gameObject.Activate(value: true);
					vessel.DamagePoints.Add(new DamagePointData
					{
						VisibilityThreshold = sceneDamagePoint.VisibilityThreshold,
						UseOcclusion = sceneDamagePoint.UseOcclusion,
						ParentTransform = sceneDamagePoint.transform.parent,
						Position = sceneDamagePoint.transform.localPosition,
						Rotation = sceneDamagePoint.transform.localRotation,
						Scale = sceneDamagePoint.transform.localScale,
						Effects = sceneDamagePoint.Effects
					});
				}

				UnityEngine.Object.Destroy(sceneDamagePoint.gameObject);
			}
		}

		public static void CheckTags(GameObject sceneRoot, string sceneTags)
		{
			SceneObjectTag[] componentsInChildren =
				sceneRoot.GetComponentsInChildren<SceneObjectTag>(includeInactive: true);
			foreach (SceneObjectTag sceneObjectTag in componentsInChildren)
			{
				if (sceneObjectTag.TagAction != 0)
				{
					bool flag = CompareTags(sceneTags, SceneTagObject.TagsToString(sceneObjectTag.Tags));
					sceneObjectTag.gameObject.SetActive(
						(sceneObjectTag.TagAction == TagAction.DisableIfTagIs && !flag) ||
						(sceneObjectTag.TagAction == TagAction.EnableIfTagIs && flag));
				}
			}

			SceneTriggerExecutor[] componentsInChildren2 =
				sceneRoot.GetComponentsInChildren<SceneTriggerExecutor>(includeInactive: true);
			foreach (SceneTriggerExecutor SceneTriggerExecutor in componentsInChildren2)
			{
				if (SceneTriggerExecutor.TagAction == TagAction.None)
				{
					continue;
				}

				bool flag2 = CompareTags(sceneTags, SceneTriggerExecutor.Tags);
				SceneTriggerExecutor.enabled = (SceneTriggerExecutor.TagAction == TagAction.DisableIfTagIs && !flag2) ||
				                               (SceneTriggerExecutor.TagAction == TagAction.EnableIfTagIs && flag2);
				SceneTrigger[] componentsInChildren3 =
					sceneRoot.GetComponentsInChildren<SceneTrigger>(includeInactive: true);
				foreach (SceneTrigger sceneTrigger in componentsInChildren3)
				{
					if (sceneTrigger.Executor == SceneTriggerExecutor)
					{
						Collider[] components = sceneTrigger.GetComponents<Collider>();
						foreach (Collider collider in components)
						{
							collider.enabled = SceneTriggerExecutor.enabled;
						}

						sceneTrigger.enabled = SceneTriggerExecutor.enabled;
					}
				}
			}

			SceneSpawnPoint[] componentsInChildren4 =
				sceneRoot.GetComponentsInChildren<SceneSpawnPoint>(includeInactive: true);
			foreach (SceneSpawnPoint sceneSpawnPoint in componentsInChildren4)
			{
				if (sceneSpawnPoint.TagAction != 0)
				{
					bool flag3 = CompareTags(sceneTags, sceneSpawnPoint.Tags);
					sceneSpawnPoint.enabled = (sceneSpawnPoint.TagAction == TagAction.DisableIfTagIs && !flag3) ||
					                          (sceneSpawnPoint.TagAction == TagAction.EnableIfTagIs && flag3);
				}
			}
		}
	}
}
