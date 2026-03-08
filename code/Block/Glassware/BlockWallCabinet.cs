namespace FoodShelves;

public class BlockWallCabinet : BaseFSContainer {
    private WorldInteraction? openCloseDoor;

    private static readonly Cuboidf Skip = new(); // Skip selectionBox, to keep consistency between selectionBox indexes (0-3-shelves 4-cabinet)

    public override void OnLoaded(ICoreAPI api) {
        base.OnLoaded(api);

        openCloseDoor = new() {
            ActionLangCode = "blockhelp-door-openclose",
            MouseButton = EnumMouseButton.Right,
            HotKeyCode = "shift"
        };
    }

    public override WorldInteraction[]? GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer) {
        return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer)
            .Append(openCloseDoor)!;
    }

    public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos) {
        BEWallCabinet? be = blockAccessor.GetBlockEntityExt<BEWallCabinet>(pos);
        var boxes = base.GetSelectionBoxes(blockAccessor, pos);

        if (be == null) return boxes;
        
        if (be.DoorOpen) {
            return [boxes[0].Clone(), boxes[1].Clone(), boxes[2].Clone(), boxes[3].Clone(), Skip];
        }

        Cuboidf cabinetSelBox = boxes[4].Clone();

        return [Skip, Skip, Skip, Skip, cabinetSelBox];
    }
}
