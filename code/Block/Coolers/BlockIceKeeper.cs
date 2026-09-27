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
        if (selection.SelectionBoxIndex is 0) {
            return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
        }

        if (selection.SelectionBoxIndex is 1) {
            return [openCloseDoor!];
        }

        return null;
    }

    public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos) {
        // Selection Box indexes:
        // Cabinet - 2
        // Door - 1
        // Cut Ice - 0
        var boxes = base.GetSelectionBoxes(blockAccessor, pos);

        BEIceKeeper? be = blockAccessor.GetBlockEntityExt<BEIceKeeper>(pos);
        if (be == null) return boxes;

        return [Skip, Skip, boxes[3].Clone()]; // Door is at the multiblock part
    }

    public Cuboidf[] MBGetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset) {
        var boxes = base.GetSelectionBoxes(blockAccessor, pos);

        BEIceKeeper? be = blockAccessor.GetBlockEntityExt<BEIceKeeper>(pos);
        if (be == null) return boxes;

        var cutIceBox = boxes[0].Clone();
        var doorBoxClosed = boxes[1].Clone();
        var doorBoxOpen = boxes[2].Clone();
        var iceKeeperBox = boxes[3].Clone();

        cutIceBox.MBNormalizeSelectionBox(offset);
        doorBoxClosed.MBNormalizeSelectionBox(offset);
        doorBoxOpen.MBNormalizeSelectionBox(offset);
        iceKeeperBox.MBNormalizeSelectionBox(offset);

        if (be.DoorOpen) {
            return [cutIceBox, doorBoxOpen, Skip];
        }

        return [Skip, doorBoxClosed, iceKeeperBox];
    }

    public Cuboidf[] MBGetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset) {
        return [new Cuboidf(0, 0, 0, 1, 0.25f, 1)];
    }
}
