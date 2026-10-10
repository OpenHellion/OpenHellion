namespace ZeroGravity.Data;

public class PrimitiveColliderData : ISceneData
{
	public ColliderDataType Type;

	public float[] Center;

	public float[] Size;

	public bool AffectingCenterOfMass;
}
