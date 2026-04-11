namespace FoodShelves;

/// <summary>
/// Helper class for generating transformation matrices used to render items inside containers. <br />
/// Supports both grid-based placement (shelves/segments) and fully manual (explicit) placement.
/// </summary>
public static class TransformationGenerator {
    /// <summary>
    /// Generates transformation matrices using the container's grid structure (shelves, segments, items). <br />
    /// The provided accessor defines how each slot is positioned, and optional item layouts adjust placement per segment, depending on the item inside.
    /// </summary>
    public static float[][] GenerateLayout(BEBaseFSContainer be, Action<TransformationData> accessor, bool useLayouts = false) {
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

                    if (useLayouts) {
                        var itemLayout = LayoutRegistry.GetLayout(be.inv[index].Itemstack);
                        itemLayout?.Apply(td, be.inv[index].Itemstack);
                    }

                    tfMatrices[index] = td.BuildMatrix();
                }
            }
        }

        if (be is BEBaseFSCooler beCooler) {
            tfMatrices[beCooler.CutIceSlot] = new Matrixf().Scale(0.01f, 0.01f, 0.01f).Values;
        }

        return tfMatrices;
    }

    /// <summary>
    /// Generates transformation matrices from a predefined set of positions and rotations. <br />
    /// Each slot is placed exactly according to the given matrix, with an optional modifier for small adjustments.
    /// </summary>
    public static float[][] GenerateExplicit(BEBaseFSContainer be, float[,] matrix, Action<TransformationData>? modifier = null) {
        int count = matrix.GetLength(1);
        float[][] tfMatrices = new float[count][];
        TransformationData td = new(be);

        for (int i = 0; i < count; i++) {
            if (i >= be.SlotCount || be.inv[i].Empty) {
                tfMatrices[i] = new Matrixf().Values;
                continue;
            }

            td.Reset();
            td.index = i;

            td.x = matrix[0, i];
            td.y = matrix[1, i];
            td.z = matrix[2, i];

            td.rotX = matrix[3, i];
            td.rotY = matrix[4, i];
            td.rotZ = matrix[5, i];

            modifier?.Invoke(td);

            tfMatrices[i] = td.BuildMatrix();
        }

        return tfMatrices;
    }
}
