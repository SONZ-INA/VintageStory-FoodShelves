namespace FoodShelves;

public class BESushiShelf : BEBaseFSContainer {
    protected override string CantPlaceMessage => "foodshelves:Only sushi can be placed on this shelf.";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.BySegment;

    public BESushiShelf() {
        ShelfCount = 4;
        SegmentsPerShelf = 2;
        ItemsPerSegment = 6;
        inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck));
    }

    public override void Initialize(ICoreAPI api) {
        if (Block.Variant["type"] == "short") {
            ItemsPerSegment /= 2;
            this.RebuildInventory(api);
        }

        base.Initialize(api);
        inv.OnAcquireTransitionSpeed += Inventory_OnAcquireTransitionSpeed;
    }

    protected override float[][] genTransformationMatrices() {
        float[][] tfMatrices = new float[SlotCount][];

        for (int shelf = 0; shelf < ShelfCount; shelf++) {
            for (int segment = 0; segment < SegmentsPerShelf; segment++) {
                for (int item = 0; item < ItemsPerSegment; item++) {
                    int index = shelf * (SegmentsPerShelf * ItemsPerSegment) + segment * ItemsPerSegment + item;

                    float x = -(segment * 0.5f) - (item % 3) * 0.15f;
                    float y = shelf * 0.28f;
                    float z = -(item / 3) * 0.37f;

                    tfMatrices[index] =
                        new Matrixf()
                        .Translate(0.5f, 0, 0.5f)
                        .RotateYDeg(block.Shape.rotateY - 180f)
                        .Scale(0.9f, 0.9f, 1f)
                        .Translate(x - 0.07f, y + 0.05f, z - 0.24875f)
                        .Values;
                }
            }
        }

        return tfMatrices;
    }
}
