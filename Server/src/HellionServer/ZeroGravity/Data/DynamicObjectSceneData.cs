namespace ZeroGravity.Data;

public class DynamicObjectSceneData : ISceneData
{
	public short ItemID;

	public float[] Position;

	public float[] Forward;

	public float[] Up;

	public int AttachPointInSceneId;

	public DynamicObjectAuxData AuxData;

	public DynaminObjectSpawnSettings[] SpawnSettings;
}
