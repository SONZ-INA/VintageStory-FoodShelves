namespace FoodShelves;

/// <summary>
/// A static orchestrator that iterates through a block entity's inventory to build a flat array of transformation matrices. <br />
/// It separates concerns by first calculating the "grid" position (shelf/segment) via a delegate, then applying item-specific logic fetched from the <see cref="LayoutRegistry"/>.
/// </summary>
public static class TransformationGenerator {
    /// <summary>
    /// Generates all transformation matrices for a <see cref="BEBaseFSContainer"/>.
    /// </summary>
    public static float[][] Generate(BEBaseFSContainer be, Action<TransformationData> accessor) {
        float[][] tfMatrices = new float[be.SlotCount][];
        TransformationData td = new(be);

        for (int shelf = 0; shelf < be.ShelfCount; shelf++) {
            for (int segment = 0; segment < be.SegmentsPerShelf; segment++) {
                for (int item = 0; item < be.ItemsPerSegment; item++) {
                    int index = shelf * be.SegmentsPerShelf * be.ItemsPerSegment + segment * be.ItemsPerSegment + item;

                    if (be.inv[index].Empty) {
                        tfMatrices[index] = new Matrixf().Values;
                        continue;
                    }

                    td.index = index;
                    td.shelf = shelf;
                    td.segment = segment;
                    td.item = item;

                    td.Reset();
                    accessor(td);

                    var itemLayout = LayoutRegistry.GetLayout(be.inv[index].Itemstack);
                    itemLayout?.Apply(td, be.inv[index].Itemstack);

                    tfMatrices[index] = td.BuildMatrix();
                }
            }
        }

        if (be is BEBaseFSCooler beCooler) {
            tfMatrices[beCooler.CutIceSlot] = new Matrixf().Scale(0.01f, 0.01f, 0.01f).Values;
        }

        return tfMatrices;
    }
}
