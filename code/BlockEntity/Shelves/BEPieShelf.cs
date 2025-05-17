namespace FoodShelves;

public class BEPieShelf : BEBaseFSContainer {
    protected override string CantPlaceMessage => "foodshelves:Only pies or cheese can be placed on this shelf.";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByBlock;
    protected override bool RipeningSpot => true;

    public override int ShelfCount => 3;

    public BEPieShelf() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck)); }

    protected override float[][] genTransformationMatrices() {
        float[][] tfMatrices = new float[SlotCount][];

        for (int i = 0; i < SlotCount; i++) {
            tfMatrices[i] =
                new Matrixf()
                .Translate(0.5f, 0, 0.5f)
                .RotateYDeg(block.Shape.rotateY)
                .Translate(- 0.5f, i * 0.313f + 0.0525f, - 0.5f)
                .Values;
        }

        return tfMatrices;
    }
}
