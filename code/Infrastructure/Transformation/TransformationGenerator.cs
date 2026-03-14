using System.Linq;

namespace FoodShelves;

/// <summary>
/// Helper class to generate the transformation matrices for the BlockEntity's contents.
/// </summary>
public static class TransformationGenerator {
    // Registry of all exceptions
    private static readonly Dictionary<string, ITransformationException> AllExceptions = new() {
        ["medium"] = new MediumItemTransform(),
        ["standard"] = new StandardItemTransform(),
        ["coolingCabinet"] = new CoolingCabinetTransform(),
    };

    /// <summary>
    /// Generates all transformation matrices for a BEBaseFSContainer.
    /// </summary>
    public static float[][] Generate(BEBaseFSContainer be, Action<TransformationData> accessor, params string[] applyExceptions) {
        float[][] tfMatrices = new float[be.SlotCount][];
        TransformationData td = new(be);

        // Build the list of exceptions to apply
        var exceptionsToApply = applyExceptions
            .Where(name => AllExceptions.ContainsKey(name))
            .Select(name => AllExceptions[name])
            .ToList();

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

                    foreach (var ex in exceptionsToApply)
                        ex.Apply(be, td);

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
