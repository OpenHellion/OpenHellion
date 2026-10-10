namespace ZeroGravity.Network;

public enum CharacterAnimationStats
{
	Crouch = 1,
	Jump = 2,
	ZeroG = 4,
	InStance = 8,
	Reloading = 0x10,
	Grounded = 0x20,
	Holster = 0x40,
	Draw = 0x80,
	CancelInteract = 0x100,
	Turning = 0x200,
	IsFalling = 0x400,
	EquipItem = 0x800,
	TouchingFloor = 0x1000,
	UsingTool = 0x2000,
	Emote = 0x4000,
	Melee = 0x8000,
	UsingLadder = 0x10000,
	UseConsumable = 0x20000,
	WeaponActivated = 0x40000
}
