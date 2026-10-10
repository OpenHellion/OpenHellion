using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class VesselObjectID
{
	public long VesselGUID;

	public int InSceneID;

	// Used by ProtoBuf for deserialisation.
	public VesselObjectID()
	{
	}

	public VesselObjectID(long vesselGUID, int inSceneID)
	{
		VesselGUID = vesselGUID;
		InSceneID = inSceneID;
	}

	public override string ToString()
	{
		return "[" + VesselGUID + ", " + InSceneID + "]";
	}

	public override bool Equals(object obj)
	{
		if (obj is not VesselObjectID other)
		{
			return false;
		}

		return VesselGUID == other.VesselGUID && InSceneID == other.InSceneID;
	}

	public override int GetHashCode()
	{
		long result = 17L;
		result = result * 23 + VesselGUID;
		result = result * 23 + InSceneID;
		return (int)(result & 0xFFFFFFF);
	}
}
