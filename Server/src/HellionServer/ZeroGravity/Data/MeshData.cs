namespace ZeroGravity.Data;

public class MeshData : ISceneData
{
	public float[] CenterPosition;

	public float[] Vertices;

	public int[] Indices;

	public float[] Bounds;

	public float[] Rotation;

	public float[] Scale;

	public bool AffectingCenterOfMass = true;
}
