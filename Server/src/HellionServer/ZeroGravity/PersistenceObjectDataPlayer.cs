using System.Collections.Generic;
using ZeroGravity.Data;
using ZeroGravity.Network;
using ZeroGravity.Objects;

namespace ZeroGravity;

public class PersistenceObjectDataPlayer : PersistenceObjectData
{
	public long FakeGUID;

	public long ParentGUID;

	public SpaceObjectType ParentType;

	public double[] ParentPosition;

	public double[] ParentVelocity;

	public double[] LocalPosition;

	public double[] LocalRotation;

	public bool IsAlive;

	public string Name;

	public string PlayerId;

	public Gender Gender;

	public byte HeadType;

	public byte HairType;

	public float Health;

	public float MaxHealth;

	public CharacterAnimationData AnimationData;

	public int AnimationStatsMask;

	public double[] Velocity;

	public double[] AngularVelocity;

	public int? CurrentRoomID;

	public float CoreTemperature;

	public List<PersistenceObjectData> ChildObjects;

	public List<QuestDetails> Quests;

	public List<ItemCompoundType> Blueprints;
}
