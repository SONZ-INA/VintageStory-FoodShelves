namespace FoodShelves;

public class BEEggShelf : BEBaseFSContainer {
    protected override string CantPlaceMessage => "foodshelves:Only eggs can be placed on this shelf.";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.BySegment;

    public BEEggShelf() {
        ShelfCount = 4;
        SegmentsPerShelf = 5;
        ItemsPerSegment = 4;
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

                    float x = segment * 0.172f;
                    float y = shelf * 0.25f;
                    float z = item * 0.1875f;

                    tfMatrices[index] =
                        new Matrixf()
                        .Translate(0.5f, 0, 0.5f)
                        .RotateYDeg(block.Shape.rotateY)
                        .Translate(x - 0.84375f, y + 0.06f, z - 0.8125f)
                        .Values;
                }
            }
        }

        return tfMatrices;
    }
}
