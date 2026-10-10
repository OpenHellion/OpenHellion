using BulletSharp.Math;
using ZeroGravity.Math;

namespace ZeroGravity.Objects;

public class VesselMeshColliderData : VesselColliderData
{
	public Vector3[] Vertices;

	public int[] Indices;

	public QuaternionD Rotation;

	public Vector3D Scale;
}
