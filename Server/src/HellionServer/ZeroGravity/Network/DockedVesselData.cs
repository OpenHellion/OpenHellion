using ProtoBuf;
using ZeroGravity.Data;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class DockedVesselData
{
	public long Guid;

	public string VesselRegistration;

	public string VesselName;

	public string Tag;

	public long SpawnRuleId;

	public GameScenes.SceneId SceneId;

	public float[] CollidersCenterOffset;

	public bool IsDebrisFragment;

	public double CreationSolarSystemTime;

	public double RadarSignature;

	public bool IsDistressSignalActive;

	public bool IsAlwaysVisible;

	public VesselObjects VesselObjects;

	public bool DockingControlsDisabled;

	public bool SecurityPanelsLocked;
}
