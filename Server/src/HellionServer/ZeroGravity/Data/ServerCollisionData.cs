using System.Collections.Generic;

namespace ZeroGravity.Data;

public class ServerCollisionData : ISceneData
{
	public List<PrimitiveColliderData> PrimitiveCollidersData;

	public List<MeshData> MeshCollidersData;
}
