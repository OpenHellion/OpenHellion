using System.Collections.Generic;
using ZeroGravity.Data;
using ZeroGravity.Network;

namespace ZeroGravity;

public class PersistenceObjectDataAsteroid : PersistenceObjectData
{
	public OrbitData OrbitData;

	public string Name;

	public string Tag;

	public GameScenes.SceneId SceneID;

	public bool IsAlwaysVisible;

	public double[] Rotation;

	public double[] AngularVelocity;

	public List<AsteroidMiningPointDetails> MiningPoints;
}
