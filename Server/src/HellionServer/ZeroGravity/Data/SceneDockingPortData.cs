using System.Collections.Generic;

namespace ZeroGravity.Data;

public class SceneDockingPortData : ISceneData
{
	public int InSceneID;

	public int OrderID;

	public float[] Position;

	public float[] Rotation;

	public int[] DoorsIDs;

	public float DoorPairingDistance;

	public bool Locked;

	public List<SceneDockingPortExecutorMerge> MergeExecutors;

	public float MergeExecutorDistance;
}
