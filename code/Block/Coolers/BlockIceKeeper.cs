namespace FoodShelves;

public class BlockIceKeeper : BaseFSContainer, IMultiBlockColSelBoxes {
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
        // Cabinet - 2
        // Door    - 0, 1
        var boxes = base.GetSelectionBoxes(blockAccessor, pos);

        BEIceKeeper? be = blockAccessor.GetBlockEntityExt<BEIceKeeper>(pos);
        if (be == null) return boxes;

        return [Skip, boxes[2].Clone()]; // Door is at the multiblock part
    }

    public Cuboidf[] MBGetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset) {
        var boxes = base.GetSelectionBoxes(blockAccessor, pos);

        BEIceKeeper? be = blockAccessor.GetBlockEntityExt<BEIceKeeper>(pos);
        if (be == null) return boxes;

        var doorBoxClosed = boxes[0].Clone();
        var doorBoxOpen = boxes[1].Clone();
        var iceKeeperBox = boxes[2].Clone();

        doorBoxClosed.MBNormalizeSelectionBox(offset);
        doorBoxOpen.MBNormalizeSelectionBox(offset);
        iceKeeperBox.MBNormalizeSelectionBox(offset);

        if (be.DoorOpen) {
            return [doorBoxOpen, Skip];
        }

        return [doorBoxClosed, Skip];
    }

    public Cuboidf[] MBGetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset) {
        return [new Cuboidf(0, 0, 0, 1, 0.25f, 1)];
    }
}
