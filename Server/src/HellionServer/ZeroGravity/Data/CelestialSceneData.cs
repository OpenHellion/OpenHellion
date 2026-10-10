using System.Collections.Generic;

namespace ZeroGravity.Data;

public class CelestialSceneData : ISceneData
{
	public short ItemID;

	public string ScenePath;

	public string SceneName;

	public string GameName;

	public float Radius;

	public float RadarSignature;

	public List<AsteroidMiningPointData> MiningPoints;

	public string Collision;

	public ServerCollisionData Colliders;
}
