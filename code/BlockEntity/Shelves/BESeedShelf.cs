namespace FoodShelves;

public class BESeedShelf : BEBaseFSContainer {
    protected override string CantPlaceMessage => "foodshelves:Only seeds can be placed on this shelf.";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.BySegment;

    public BESeedShelf() {
        ShelfCount = 3;
        SegmentsPerShelf = 3;
        ItemsPerSegment = 4;
        inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck, 64));
    }

    public override void Initialize(ICoreAPI api) {
        switch (Block.Variant["type"]) {
            case "short":
                ItemsPerSegment /= 2;
                this.RebuildInventory(api, 64);
                break;
            case "veryshort":
                ItemsPerSegment /= 4;
                this.RebuildInventory(api, 64);
                break;
        }

        base.Initialize(api);
    }

    protected override float[][] genTransformationMatrices() {
        float[][] tfMatrices = new float[SlotCount][];

        for (int shelf = 0; shelf < ShelfCount; shelf++) {
            for (int segment = 0; segment < SegmentsPerShelf; segment++) {
                for (int item = 0; item < ItemsPerSegment; item++) {
                    int index = shelf * (SegmentsPerShelf * ItemsPerSegment) + segment * ItemsPerSegment + item;

                    float x = segment * 0.575f;
                    float y = shelf * 0.9f;
                    float z = item * 0.4125f;

                    tfMatrices[index] = new Matrixf()
                        .Translate(0.5f, 0, 0.5f)
                        .RotateYDeg(block.Shape.rotateY)
                        .Scale(0.44f, 0.35f, 0.44f)
                        .Translate(x - 1.075f, y + 0.175f, z - 1.225f)
                        .Values;
                }
            }
        }

        return tfMatrices;
    }
}
