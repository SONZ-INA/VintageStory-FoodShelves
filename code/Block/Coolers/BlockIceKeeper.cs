namespace FoodShelves;

public class BlockIceKeeper : BaseFSContainer {
    private WorldInteraction? openCloseDoor;

    private static readonly Cuboidf Skip = new(); // Skip selectionBox, to keep consistency between selectionBox indexes (0-3-shelves 4-door, 5-cabinet)

    public override void OnLoaded(ICoreAPI api) {
        base.OnLoaded(api);

        openCloseDoor = new() {
            ActionLangCode = "blockhelp-door-openclose",
            MouseButton = EnumMouseButton.Right
        };
    }

    public override WorldInteraction[]? GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer) {
        if (selection.SelectionBoxIndex is 4 or 5) {
            return [openCloseDoor!];
        }

        return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
    }

    public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos) {
        // Selection Box indexes:
        // Cabinet - 1
        // Door    - 0
        var boxes = base.GetSelectionBoxes(blockAccessor, pos);

        BEIceKeeper? be = blockAccessor.GetBlockEntityExt<BEIceKeeper>(pos);
        if (be == null) return boxes;

        return boxes;
        // TODO

        if (be.DoorOpen) {
            return [boxes[0].Clone(), boxes[1].Clone(), boxes[2].Clone(), boxes[3].Clone(), boxes[4].Clone()];
        }

        return [Skip, Skip, Skip, Skip, Skip, boxes[5].Clone()];
    }
}
