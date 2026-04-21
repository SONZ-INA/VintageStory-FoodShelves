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

        TransformationData td = new() {
            preRotate = be.Block.GetRotationAngle()
        };

        for (int shelf = 0; shelf < be.ShelfCount; shelf++) {
            int shelfOffset = shelf * be.SegmentsPerShelf * be.ItemsPerSegment;

            for (int segment = 0; segment < be.SegmentsPerShelf; segment++) {
                int segmentOffset = shelfOffset + (segment * be.ItemsPerSegment);

                for (int item = 0; item < be.ItemsPerSegment; item++) {
                    int index = segmentOffset + item;
                    var slot = be.inv[index];
                    
                    if (slot.Empty) {
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
                        var itemStack = slot.Itemstack;
                        LayoutRegistry.GetLayout(itemStack)?.Apply(td, itemStack);
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
    public static float[][] GenerateExplicit(ExplicitTransform transform, Action<TransformationData>? modifier = null) {
        int count = transform.Length;
        float[][] tfMatrices = new float[count][];
        TransformationData td = new();

        for (int i = 0; i < count; i++) {
            td.Reset();
            td.index = i;

            td.x = transform.X[i];
            td.y = transform.Y[i];
            td.z = transform.Z[i];

            td.offsetRotX = transform.RX[i];
            td.offsetRotY = transform.RY[i];
            td.offsetRotZ = transform.RZ[i];

            modifier?.Invoke(td);

            tfMatrices[i] = td.BuildMatrix();
        }

        return tfMatrices;
    }
}
