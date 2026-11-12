// StaticDataGenerator.cs
//
// Copyright (C) 2026, OpenHellion contributors
//
// SPDX-License-Identifier: GPL-3.0-or-later
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using OpenHellion.IO;
using ZeroGravity;
using ZeroGravity.Data;
using ZeroGravity.LevelDesign;
using ZeroGravity.Objects;
using ZeroGravity.ShipComponents;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace OpenHellion
{
	/// <summary>
	/// 	Generates the static data files in Assets/Resources/Data from the scenes in
	/// 	Assets/Scene/Environment. Everything is read from the scenes themselves; nothing
	/// 	is carried over from previously generated files.
	/// </summary>
	public static class StaticDataGenerator
	{
		[MenuItem("OpenHellion/Generate Structures Data")]
		public static void GenerateStructures()
		{
			if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
			{
				return;
			}

			string previousScenePath = SceneManager.GetActiveScene().path;
			List<StructureSceneData> structures = new List<StructureSceneData>();
			foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scene/Environment" }))
			{
				string scenePath = AssetDatabase.GUIDToAssetPath(guid);
				UnityScene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
				foreach (GameObject rootObject in scene.GetRootGameObjects())
				{
					StructureScene structure = rootObject.GetComponentInChildren<StructureScene>(true);
					if (structure != null)
					{
						structures.Add(BuildStructureData(structure, scene));
						break;
					}
				}
			}

			structures.Sort((a, b) => a.ItemID.CompareTo(b.ItemID));
			JsonSerialiser.SerializeDataPath(structures, "Resources/Data/Structures.json");
			AssetDatabase.Refresh();
			if (previousScenePath != string.Empty)
			{
				EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
			}

			Debug.LogFormat("Generated structure data for {0} scenes.", structures.Count);
		}

		private static StructureSceneData BuildStructureData(StructureScene structure, UnityScene scene)
		{
			GameObject root = structure.gameObject;
			Transform frame = structure.transform;

			// State IDs are assigned on demand; do it up front so every state name lookup below resolves.
			foreach (SceneTriggerExecutor executor in root.GetComponentsInChildren<SceneTriggerExecutor>(true))
			{
				executor.ReadDefaultStates();
			}

			// Each component type is collected with the same includeInactive flag SceneHelper uses at
			// runtime, so the data never describes objects the game itself will not find.
			SceneCargoBay cargoBay = root.GetComponentInChildren<SceneCargoBay>();
			return new StructureSceneData
			{
				ItemID = (short)structure.GUID,
				ScenePath = scene.path,
				SceneName = scene.name,
				GameName = structure.GameName,
				Mass = structure.Mass * 1000f,
				HeatCollectionFactor = structure.HeatCollectionFactor,
				HeatDissipationFactor = structure.HeatDissipationFactor,
				StructureConnections = new List<StructureSceneConnectionData>(),
				SpawnPoints = GetSpawnPoints(root, frame),
				DynamicObjects = GetDynamicObjects(root, frame),
				AttachPoints = GetAttachPoints(root),
				SubSystems = GetSubSystems(root),
				Generators = GetGenerators(root),
				Rooms = GetRooms(root),
				ResourceContainers = GetResourceContainers(root),
				Collision = scene.name,
				Doors = GetDoors(root),
				SceneTriggerExecutors = GetExecutors(root),
				DockingPorts = GetDockingPorts(root, frame),
				SpawnObjectChanceData = GetSpawnObjectChances(root),
				CargoBay = cargoBay != null ? cargoBay.GetData() : null,
				NameTags = GetNameTags(root),
				RepairPoints = GetRepairPoints(root),
				HasSecuritySystem = root.GetComponentInChildren<SecuritySystem>(true) != null,
				MaxHealth = structure.MaxHealth,
				Health = structure.Health,
				BaseArmor = structure.BaseArmor,
				InvulnerableWhenDocked = structure.InvulnerableWhenDocked,
				SpawnSettings = structure.SpawnSettings,
				AdditionalTags = structure.AdditionalTags
			};
		}

		private static List<SpawnPointData> GetSpawnPoints(GameObject root, Transform frame)
		{
			List<SpawnPointData> list = new List<SpawnPointData>();
			foreach (SceneSpawnPoint point in root.GetComponentsInChildren<SceneSpawnPoint>(true))
			{
				SpawnPointData data = new SpawnPointData
				{
					InSceneID = point.InSceneID,
					Position = frame.InverseTransformPoint(point.transform.position).ToArray(),
					Rotation = (Quaternion.Inverse(frame.rotation) * point.transform.rotation).ToArray(),
					TagAction = point.TagAction,
					Tags = point.Tags,
					Type = point.SpawnType,
					ExecutorID = 0,
					ExecutorStateID = -1,
					ExecutorOccupiedStateIDs = new List<int>()
				};
				if (point.Executor != null)
				{
					data.ExecutorID = point.Executor.InSceneID;
					data.ExecutorStateID = point.Executor.GetStateID(point.ExecutorState);
					if (!string.IsNullOrEmpty(point.ExecutorOccupiedStates))
					{
						foreach (string stateName in point.ExecutorOccupiedStates.Split(';'))
						{
							data.ExecutorOccupiedStateIDs.Add(point.Executor.GetStateID(stateName));
						}
					}
				}

				list.Add(data);
			}

			return list;
		}

		private static List<DynamicObjectSceneData> GetDynamicObjects(GameObject root, Transform frame)
		{
			List<DynamicObjectSceneData> list = new List<DynamicObjectSceneData>();
			foreach (DynamicSceneObject sceneObject in root.GetComponentsInChildren<DynamicSceneObject>())
			{
				// Some scene objects are bare markers whose item exists only on the referenced prefab.
				Item item = sceneObject.GetComponentInChildren<Item>(true);
				if (item == null && sceneObject.PrefabObject != null)
				{
					item = sceneObject.PrefabObject.GetComponentInChildren<Item>(true);
				}

				if (item == null)
				{
					throw new Exception($"Dynamic scene object {sceneObject.name} in {root.scene.path} has no item.");
				}

				BaseSceneAttachPoint attachPoint = sceneObject.GetComponentInParent<BaseSceneAttachPoint>(true);

				// GetAuxData reads the prefab defaults; the per-instance values authored on the scene
				// object are laid over the top. See DynamicSceneObject for why these overrides exist.
				DynamicObjectAuxData auxData = item.GetAuxData();
				if (auxData != null)
				{
					ApplyOverrides(auxData, sceneObject);
				}

				list.Add(new DynamicObjectSceneData
				{
					ItemID = ResolveItemId(sceneObject, item),
					Position = frame.InverseTransformPoint(sceneObject.transform.position).ToArray(),
					Forward = frame.InverseTransformDirection(sceneObject.transform.forward).ToArray(),
					Up = frame.InverseTransformDirection(sceneObject.transform.up).ToArray(),
					AttachPointInSceneId = attachPoint != null ? attachPoint.InSceneID : 0,
					AuxData = auxData,
					SpawnSettings = sceneObject.SpawnSettings
				});
			}

			return list;
		}

		private static void ApplyOverrides(DynamicObjectAuxData auxData, DynamicSceneObject sceneObject)
		{
			auxData.Tier = sceneObject.Tier;

			if (auxData.Slots != null && sceneObject.SlotOverrides != null)
			{
				foreach (DynamicSceneObject.SlotSpawnOverride slotOverride in sceneObject.SlotOverrides)
				{
					ItemSlotData slot = auxData.Slots.Find(candidate => candidate.ID == slotOverride.SlotID);
					if (slot != null)
					{
						slot.SpawnItem = slotOverride.SpawnItem;
					}
				}
			}

			if (sceneObject.CompartmentOverrides != null)
			{
				foreach (DynamicSceneObject.CompartmentOverride compartmentOverride in sceneObject.CompartmentOverrides)
				{
					foreach (CargoCompartmentData compartment in Compartments(auxData))
					{
						if (compartment.Type == compartmentOverride.Compartment)
						{
							compartment.Resources = new List<CargoResourceData>(compartmentOverride.Resources);
						}
					}
				}
			}

			if (auxData is HelmetData helmet)
			{
				helmet.IsLightActive = sceneObject.LightActive;
			}

			if (auxData is OutfitData outfit && sceneObject.OutfitOverride.Enabled)
			{
				DynamicSceneObject.OutfitStatsOverride stats = sceneObject.OutfitOverride;
				outfit.Category = stats.Category;
				outfit.Armor = stats.Armor;
				outfit.TierMultipliers = stats.TierMultipliers;
				outfit.DamageReductionTorso = stats.DamageReductionTorso;
				outfit.DamageReductionAbdomen = stats.DamageReductionAbdomen;
				outfit.DamageReductionArms = stats.DamageReductionArms;
				outfit.DamageReductionLegs = stats.DamageReductionLegs;
				outfit.DamageResistanceTorso = stats.DamageResistanceTorso;
				outfit.DamageResistanceAbdomen = stats.DamageResistanceAbdomen;
				outfit.DamageResistanceArms = stats.DamageResistanceArms;
				outfit.DamageResistanceLegs = stats.DamageResistanceLegs;
				outfit.CollisionResistance = stats.CollisionResistance;

				if (outfit.InventorySlots != null && stats.SlotItemTypes != null)
				{
					foreach (DynamicSceneObject.SlotItemTypesOverride slotTypes in stats.SlotItemTypes)
					{
						InventorySlotData slot = outfit.InventorySlots.Find(candidate => candidate.SlotID == slotTypes.SlotID);
						if (slot != null)
						{
							slot.ItemTypes = new List<ItemType>(slotTypes.ItemTypes);
						}
					}
				}
			}
		}

		internal static IEnumerable<CargoCompartmentData> Compartments(DynamicObjectAuxData auxData)
		{
			switch (auxData)
			{
				case CanisterData canister when canister.CargoCompartment != null:
					yield return canister.CargoCompartment;
					break;
				case JetpackData jetpack:
					if (jetpack.OxygenCompartment != null)
					{
						yield return jetpack.OxygenCompartment;
					}
					if (jetpack.PropellantCompartment != null)
					{
						yield return jetpack.PropellantCompartment;
					}
					break;
				case RepairToolData repairTool when repairTool.FuelCompartment != null:
					yield return repairTool.FuelCompartment;
					break;
			}
		}

		internal static short ResolveItemId(DynamicSceneObject sceneObject, Item item)
		{
			string prefabPath = sceneObject.PrefabObject != null
				? AssetDatabase.GetAssetPath(sceneObject.PrefabObject)
				: null;
			foreach (DynamicObjectData data in StaticData.DynamicObjectsDataList.Values)
			{
				if (prefabPath != null && string.Equals(prefabPath,
					    "Assets/Resources/" + data.PrefabPath + ".prefab", StringComparison.OrdinalIgnoreCase))
				{
					return data.ItemID;
				}
			}

			foreach (DynamicObjectData data in StaticData.DynamicObjectsDataList.Values)
			{
				if (data.ItemType != item.Type)
				{
					continue;
				}
				if (item is GenericItem generic && (data.DefaultAuxData as GenericItemData)?.SubType != generic.SubType)
				{
					continue;
				}
				if (item is MachineryPart part && (data.DefaultAuxData as MachineryPartData)?.PartType != part.PartType)
				{
					continue;
				}

				return data.ItemID;
			}

			throw new Exception($"No dynamic object data matches item {item.name} in {sceneObject.gameObject.scene.path}.");
		}

		private static List<BaseAttachPointData> GetAttachPoints(GameObject root)
		{
			List<BaseAttachPointData> list = new List<BaseAttachPointData>();
			foreach (BaseSceneAttachPoint attachPoint in root.GetComponentsInChildren<BaseSceneAttachPoint>())
			{
				list.Add(attachPoint.GetData());
			}

			return list;
		}

		private static List<SubSystemData> GetSubSystems(GameObject root)
		{
			List<SubSystemData> list = new List<SubSystemData>();
			foreach (SubSystem subSystem in root.GetComponentsInChildren<SubSystem>())
			{
				list.Add(subSystem.GetData());
			}

			return list;
		}

		private static List<GeneratorData> GetGenerators(GameObject root)
		{
			List<GeneratorData> list = new List<GeneratorData>();
			foreach (Generator generator in root.GetComponentsInChildren<Generator>())
			{
				list.Add(generator.GetData());
			}

			return list;
		}

		private static List<RoomData> GetRooms(GameObject root)
		{
			List<RoomData> list = new List<RoomData>();
			foreach (SceneTriggerRoom room in root.GetComponentsInChildren<SceneTriggerRoom>())
			{
				list.Add(new RoomData
				{
					InSceneID = room.InSceneID,
					UseGravity = room.UseGravity,
					GravityAutoToggle = room.GravityAutoToggle,
					AirFiltering = room.AirFiltering,
					Volume = room.Volume,
					AirQuality = room.AirQuality,
					AirPressure = room.AirPressure,
					PressurizeSpeed = room.PressurizeSpeed,
					DepressurizeSpeed = room.DepressurizeSpeed,
					VentSpeed = room.VentSpeed,
					ParentRoomID = room.ParentRoom != null ? room.ParentRoom.InSceneID : 0
				});
			}

			return list;
		}

		private static List<ResourceContainerData> GetResourceContainers(GameObject root)
		{
			List<ResourceContainerData> list = new List<ResourceContainerData>();
			foreach (ResourceContainer container in root.GetComponentsInChildren<ResourceContainer>())
			{
				list.Add(container.GetData());
			}

			return list;
		}

		private static List<DoorData> GetDoors(GameObject root)
		{
			SceneDockingPort[] ports = root.GetComponentsInChildren<SceneDockingPort>(true);
			List<DoorData> list = new List<DoorData>();
			foreach (SceneDoor door in root.GetComponentsInChildren<SceneDoor>())
			{
				Vector3 passagePoint = DoorPassagePoint(door);
				Vector3 portRelativePosition = Vector3.zero;
				foreach (SceneDockingPort port in ports)
				{
					if (port.Doors != null && port.Doors.Contains(door))
					{
						portRelativePosition = port.transform.InverseTransformPoint(passagePoint);
					}
				}

				list.Add(new DoorData
				{
					InSceneID = door.InSceneID,
					Room1ID = door.Room1 != null ? door.Room1.InSceneID : 0,
					Room2ID = door.Room2 != null ? door.Room2.InSceneID : 0,
					PassageArea = door.PassageArea,
					IsSealable = door.IsSealable,
					HasPower = door.HasPower,
					IsLocked = door.IsLocked,
					IsOpen = door.IsOpen,
					LockedAutoToggle = door.LockedAutoToggle,
					PositionRelativeToDockingPort = portRelativePosition.ToArray()
				});
			}

			return list;
		}

		private static Vector3 DoorPassagePoint(SceneDoor door)
		{
			return door.DoorPassageTrigger != null
				? door.DoorPassageTrigger.transform.position
				: door.transform.position;
		}

		private static List<SceneTriggerExecutorData> GetExecutors(GameObject root)
		{
			SceneTrigger[] triggers = root.GetComponentsInChildren<SceneTrigger>(true);
			List<SceneTriggerExecutorData> list = new List<SceneTriggerExecutorData>();
			foreach (SceneTriggerExecutor executor in root.GetComponentsInChildren<SceneTriggerExecutor>(true))
			{
				List<SceneTriggerProximityData> proximityTriggers = null;
				foreach (SceneTrigger trigger in triggers)
				{
					SceneTriggerExecutor triggerExecutor = trigger.Executor != null
						? trigger.Executor
						: trigger.GetComponentInParent<SceneTriggerExecutor>(true);
					if (!trigger.IsProximity || triggerExecutor != executor)
					{
						continue;
					}

					proximityTriggers ??= new List<SceneTriggerProximityData>();
					proximityTriggers.Add(new SceneTriggerProximityData
					{
						TriggerID = trigger.TriggerID,
						ActiveStateID = executor.GetStateID(trigger.ExecutorStateName.Split(';')[0]),
						InactiveStateID = executor.GetStateID(trigger.ExecutorAltStateName)
					});
				}

				list.Add(new SceneTriggerExecutorData
				{
					InSceneID = executor.InSceneID,
					DefaultStateID = executor.DefaultStateID,
					TagAction = executor.TagAction,
					Tags = executor.Tags,
					States = executor.GetExecuterStatesData(),
					ProximityTriggers = proximityTriggers
				});
			}

			return list;
		}

		private static List<SceneDockingPortData> GetDockingPorts(GameObject root, Transform frame)
		{
			List<SceneDockingPortData> list = new List<SceneDockingPortData>();
			foreach (SceneDockingPort port in root.GetComponentsInChildren<SceneDockingPort>(true))
			{
				if (port.IgnoreThisDockingPort)
				{
					continue;
				}

				List<int> doorIds = new List<int>();
				if (port.Doors != null)
				{
					foreach (SceneDoor door in port.Doors)
					{
						doorIds.Add(door.InSceneID);
					}
				}

				List<SceneDockingPortExecutorMerge> mergeExecutors = new List<SceneDockingPortExecutorMerge>();
				if (port.MergeExecutors != null)
				{
					foreach (SceneTriggerExecutor executor in port.MergeExecutors)
					{
						Vector3 executorPoint = executor.MergePivot != null
							? executor.MergePivot.position
							: executor.transform.position;
						mergeExecutors.Add(new SceneDockingPortExecutorMerge
						{
							InSceneID = executor.InSceneID,
							Position = port.transform.InverseTransformPoint(executorPoint).ToArray()
						});
					}
				}

				list.Add(new SceneDockingPortData
				{
					InSceneID = port.InSceneID,
					OrderID = port.DockingPortOrder,
					Position = frame.InverseTransformPoint(port.transform.position).ToArray(),
					Rotation = (Quaternion.Inverse(frame.rotation) * port.transform.rotation).ToArray(),
					DoorsIDs = doorIds.ToArray(),
					DoorPairingDistance = port.DoorPairingDistance,
					Locked = port.Locked,
					MergeExecutors = mergeExecutors,
					MergeExecutorDistance = port.MergeExecutorDistance
				});
			}

			return list;
		}

		private static List<SpawnObjectsWithChanceData> GetSpawnObjectChances(GameObject root)
		{
			List<SpawnObjectsWithChanceData> list = new List<SpawnObjectsWithChanceData>();
			foreach (SpawnObjectsWithChanceScene spawnObject in root.GetComponentsInChildren<SpawnObjectsWithChanceScene>())
			{
				list.Add(new SpawnObjectsWithChanceData
				{
					InSceneID = spawnObject.InSceneID
				});
			}

			return list;
		}

		private static List<NameTagData> GetNameTags(GameObject root)
		{
			List<NameTagData> list = new List<NameTagData>();
			foreach (SceneNameTag nameTag in root.GetComponentsInChildren<SceneNameTag>(true))
			{
				if (!nameTag.Local)
				{
					list.Add(new NameTagData
					{
						InSceneID = nameTag.InSceneID,
						NameTagText = nameTag.NameTagText
					});
				}
			}

			return list;
		}

		private static List<VesselRepairPointData> GetRepairPoints(GameObject root)
		{
			List<VesselRepairPointData> list = new List<VesselRepairPointData>();
			foreach (VesselRepairPoint repairPoint in root.GetComponentsInChildren<VesselRepairPoint>(true))
			{
				list.Add(repairPoint.GetData());
			}

			return list;
		}
	}
}
