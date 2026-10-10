using System.Collections.Generic;

namespace ZeroGravity.Data;

public class SpawnPointData : ISceneData
{
	public int InSceneID;

	public float[] Position;

	public float[] Rotation;

	public TagAction TagAction;

	public string Tags;

	public SpawnPointType Type;

	public int ExecutorID;

	public int ExecutorStateID;

	public List<int> ExecutorOccupiedStateIDs;
}
