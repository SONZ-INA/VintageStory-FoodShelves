namespace FoodShelves;

public class BlockSeedBins : BaseFSContainer {
    public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos) {
        //if (blockAccessor.GetBlockEntity(pos) is BESeedBins be) {
        //    if (!be.Section1Open) {
        //        be.Section1Open = true;
        //        be.MarkDirty();
        //    }
        //}

        return base.GetSelectionBoxes(blockAccessor, pos);
    }
}
