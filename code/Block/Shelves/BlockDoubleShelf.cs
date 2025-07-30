using System.Linq;

namespace FoodShelves;

public class BlockDoubleShelf : BaseFSContainer, IMultiBlockColSelBoxes {
    // Selection box for master block
    public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos) {
        Cuboidf segment1 = base.GetSelectionBoxes(blockAccessor, pos).ElementAt(0).Clone();
        Cuboidf segment2 = base.GetSelectionBoxes(blockAccessor, pos).ElementAt(1).Clone();
        Cuboidf skip = new();

        return [segment1, segment2, skip];
    }

    // Selection boxes for multiblock parts
    public Cuboidf[] MBGetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset) {
        Cuboidf segment2 = base.GetSelectionBoxes(blockAccessor, pos).ElementAt(1).Clone();
        Cuboidf segment3 = base.GetSelectionBoxes(blockAccessor, pos).ElementAt(2).Clone();
        Cuboidf skip = new();

        segment2.MBNormalizeSelectionBox(offset);
        segment3.MBNormalizeSelectionBox(offset);

        return [skip, segment2, segment3];
    }

    public Cuboidf[] MBGetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset) {
        return base.GetCollisionBoxes(blockAccessor, pos);
    }
}
