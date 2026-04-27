namespace FoodShelves;

public class BlockDoubleShelf : BaseFSContainer, IMultiBlockColSelBoxes {
    private static readonly Cuboidf Skip = new();

    // Selection box for master block
    public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos) {
        var boxes = base.GetSelectionBoxes(blockAccessor, pos);

        Cuboidf segment1 = boxes[0].Clone();
        Cuboidf segment2 = boxes[1].Clone();

        return [segment1, segment2, Skip];
    }

    // Selection boxes for multiblock parts
    public Cuboidf[] MBGetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset) {
        var boxes = base.GetSelectionBoxes(blockAccessor, pos);

        Cuboidf segment2 = boxes[1].Clone();
        Cuboidf segment3 = boxes[2].Clone();

        segment2.MBNormalizeSelectionBox(offset);
        segment3.MBNormalizeSelectionBox(offset);

        return [Skip, segment2, segment3];
    }

    public Cuboidf[] MBGetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset) {
        return base.GetCollisionBoxes(blockAccessor, pos);
    }
}
