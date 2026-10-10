namespace ZeroGravity.Objects;

public interface IItemSlot
{
	Item Item { get; }

	SpaceObject Parent { get; }

	bool CanFitItem(Item item);
}
