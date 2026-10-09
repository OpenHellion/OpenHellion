using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using OpenHellion;
using UnityEngine;
using ZeroGravity.Data;
using ZeroGravity.LevelDesign;
using ZeroGravity.Network;

namespace ZeroGravity.Objects
{
	// TODO test if asteroid data is updated when needed
	public class Asteroid : SpaceObjectVessel
	{
		public Dictionary<int, AsteroidMiningPoint> MiningPoints = new Dictionary<int, AsteroidMiningPoint>();

		public override SpaceObjectType Type => SpaceObjectType.Asteroid;

		public override void ChangeStats(Vector3? thrust = null, Vector3? rotation = null,
			Vector3? autoStabilize = null, float? engineThrustPercentage = null, SubSystemDetails subSystem = null,
			GeneratorDetails distributionSystem = null, RoomDetails roomTrigger = null, DoorDetails door = null,
			SceneTriggerExecutorDetails sceneTriggerExecutor = null, SceneDockingPortDetails dockingPort = null,
			AttachPointDetails attachPoint = null, long? stabilizationTarget = null, SpawnPointStats spawnPoint = null,
			float? selfDestructTime = null, string emblemId = null)
		{
		}

		public override void DestroyGeometry()
		{
			base.DestroyGeometry();
			SceneObjectsLoaded = false;
		}

		/// <summary>
		/// Creates and loads an asteroid asyncronously.
		/// </summary>
		public static async UniTask<Asteroid> Create(World world, long guid, Vector3 position, Quaternion rotation, string vesselRegistration,
			string vesselName, string tag, GameScenes.SceneId sceneId, bool isDebrisFragment, bool isAlwaysVisible, double radius,
			AsteroidMiningPointDetails[] miningPoints, bool isMainObject)
		{
			Asteroid asteroid = InitialiseArtificialBody(world, guid, SpaceObjectType.Asteroid, position, rotation) as Asteroid;
			asteroid.VesselRegistration = vesselRegistration;
			asteroid.VesselName = vesselName;
			asteroid.Tag = tag;
			asteroid.SceneId = sceneId;
			asteroid.IsDebrisFragment = isDebrisFragment;
			asteroid.IsAlwaysVisible = isAlwaysVisible;
			asteroid.Radius = radius;
			asteroid.SceneObjectsLoaded = false;
			asteroid.gameObject.SetActive(true);
			await asteroid.LoadInternalAsync(miningPoints);
			return asteroid;
		}

		private void Update()
		{
		}

		private void OnDestroy()
		{
			World.RemoveArtificialBody(this);
			SceneHelper.RemoveCubemapProbes(gameObject, World);
			World.ActiveVessels.TryRemove(Guid, out _);
		}

		private async UniTask LoadInternalAsync(AsteroidMiningPointDetails[] miningPoints)
		{
			World.InGameGUI.ToggleBusyLoading(true);
			await Globals.SceneLoader.LoadSceneAsync(SceneLoader.SceneType.CelestialBody, (long)SceneId);
			GameObject sceneRoot =
				Globals.SceneLoader.GetLoadedScene(SceneLoader.SceneType.CelestialBody, SceneId);
			sceneRoot.transform.SetParent(GeometryRoot.transform);
			sceneRoot.transform.Reset();
			RootObject = sceneRoot;
			if (GeometryRoot != null)
			{
				DestructionEffects = GeometryRoot.GetComponentInChildren<VesselDestructionEffects>(true);
				if (DestructionEffects != null)
				{
					DestructionEffects.gameObject.SetActive(false);
				}
			}

			if (TargetRotation.HasValue)
			{
				SetTargetPositionAndRotation(null, TargetRotation.Value, true);
			}
			World.ActiveVessels.TryAdd(Guid, this);
			sceneRoot.SetActive(true);
			SceneHelper.FillCubemapProbes(sceneRoot, World);
			SceneHelper.CheckTags(sceneRoot, Tag);
			SceneHelper.FillMiningPoints(this, gameObject, MiningPoints, miningPoints);
			SceneObjectsLoaded = true;
			World.InGameGUI.ToggleBusyLoading(false);
		}
	}
}
