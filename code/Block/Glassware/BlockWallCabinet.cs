using System.Linq;

namespace FoodShelves;

public class BlockWallCabinet : BaseFSContainer {
    public readonly AssetLocation soundCabinetOpen = new(SoundReferences.WallCabinetOpen);
    public readonly AssetLocation soundCabinetClose = new(SoundReferences.WallCabinetClose);

    private WorldInteraction openCloseDoor;

    public override void OnLoaded(ICoreAPI api) {
        base.OnLoaded(api);

        openCloseDoor = new() {
            ActionLangCode = "blockhelp-door-openclose",
            MouseButton = EnumMouseButton.Right,
            HotKeyCode = "shift"
        };
    }

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer) {
        return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer)
            .Append(openCloseDoor);
    }

    public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos) {
        BEWallCabinet be = blockAccessor.GetBlockEntityExt<BEWallCabinet>(pos);

        Cuboidf cabinetSelBox = base.GetSelectionBoxes(blockAccessor, pos).ElementAt(4).Clone();
        Cuboidf skip = new(); // Skip selectionBox, to keep consistency between selectionBox indexes (0-3-shelves 4-cabinet)

        if (be != null) {
            if (be.CabinetOpen) {
                List<Cuboidf> segments = base.GetSelectionBoxes(blockAccessor, pos).Take(4).Select(c => c.Clone()).ToList();
                return [segments[0], segments[1], segments[2], segments[3], skip];
            }

            return [skip, skip, skip, skip, cabinetSelBox];
        }

        return base.GetSelectionBoxes(blockAccessor, pos);
    }
}
