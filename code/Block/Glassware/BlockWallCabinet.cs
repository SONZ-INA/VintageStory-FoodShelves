using System.Linq;

namespace FoodShelves;

public class BlockWallCabinet : BaseFSContainer {
    public readonly AssetLocation soundCabinetOpen = new(SoundReferences.WallCabinetOpen);
    public readonly AssetLocation soundCabinetClose = new(SoundReferences.WallCabinetClose);

    public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos) {
        BEWallCabinet be = blockAccessor.GetBlockEntityExt<BEWallCabinet>(pos);

        Cuboidf cabinetSelBox = base.GetSelectionBoxes(blockAccessor, pos).ElementAt(4).Clone();
        Cuboidf skip = new(); // Skip selectionBox, to keep consistency between selectionBox indexes (0-3-shelves 4-cabinet)

        if (be != null) {
            if (be.CabinetOpen) {
                List<Cuboidf> segments = base.GetSelectionBoxes(blockAccessor, pos).Take(4).Select(c => c.Clone()).ToList();
                return new Cuboidf[] { segments[0], segments[1], segments[2], segments[3], skip };
            }

            return new Cuboidf[] { skip, skip, skip, skip, cabinetSelBox };
        }

        return base.GetSelectionBoxes(blockAccessor, pos);
    }
}
