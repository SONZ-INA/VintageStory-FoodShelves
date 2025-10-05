namespace FoodShelves;

public class BEBreadShelf : BEBaseFSContainer {
    protected override string CantPlaceMessage => "foodshelves:Only bread, muffins, dumplings, pacoca, halva or marzipam can be placed on this shelf.";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByShelf;
    protected override float PerishMultiplier => 0.5f;

    public BEBreadShelf() {
        ShelfCount = 4;
        SegmentsPerShelf = 3;
        ItemsPerSegment = 2;
        inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck));
    }

    public override void Initialize(ICoreAPI api) {
        if (Block.Variant["type"] == "short") {
            ItemsPerSegment /= 2;
            this.RebuildInventory(api);
        }

        base.Initialize(api);
    }

    protected override float[][] genTransformationMatrices() {
        float[][] tfMatrices = new float[SlotCount][];

        for (int shelf = 0; shelf < ShelfCount; shelf++) {
            for (int segment = 0; segment < SegmentsPerShelf; segment++) {
                for (int item = 0; item < ItemsPerSegment; item++) {
                    int index = shelf * (SegmentsPerShelf * ItemsPerSegment) + segment * ItemsPerSegment + item;

                    float x = segment * 0.29f;
                    float y = shelf * 0.25f;
                    float z = item * 0.4f;

                    tfMatrices[index] = new Matrixf()
                        .Translate(0.5f, 0, 0.5f)
                        .RotateYDeg(block.Shape.rotateY)
                        .Translate(x - 0.79f, y + 0.06f, z - 0.75f)
                        .Values;
                }
            }
        }

        return tfMatrices;
    }
}
