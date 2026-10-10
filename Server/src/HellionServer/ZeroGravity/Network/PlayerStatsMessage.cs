using System.Collections.Generic;
using ProtoBuf;

namespace ZeroGravity.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class PlayerStatsMessage : NetworkData
{
	public long GUID;

	public bool? AnimationMaskChanged;

	public int Health;

	public int AnimationStatesMask;

	public int ReloadType;

	public List<PlayerDamage> DamageList = new List<PlayerDamage>();

	public float[] ShotDirection;

	public CharacterSoundData SoundData;

	public VesselObjectID LockedToTriggerID;

	public bool IsPilotingVessel;
}
