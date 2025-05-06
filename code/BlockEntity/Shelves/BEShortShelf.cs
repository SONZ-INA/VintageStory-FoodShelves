namespace FoodShelves;

public class BEShortShelf : BEBaseFSContainer {
    public override string InventoryClassName => "shelf";
    public override string AttributeTransformCode => "onshelfTransform";

    public override string AttributeCheck => "shelvable";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByBlock;
    protected override bool RipeningSpot => true;

    public override int SlotCount => 4; // Check if proper

    public BEShortShelf() { inv = new InventoryGeneric(SlotCount, "shelf-0", null, null); }

    public override void Initialize(ICoreAPI api) {
        base.Initialize(api);
        inv.OnAcquireTransitionSpeed += Inventory_OnAcquireTransitionSpeed;
    }

    protected override float[][] genTransformationMatrices() {
        float[][] tfMatrices = new float[SlotCount][];

        for (int index = 0; index < SlotCount; index++) {
            float x = index % 2 == 0 ? 0.25f : 0.75f;
            float y = index >= 2 ? 0.625f : 0.125f;
            float z = 0.25f;

            tfMatrices[index] =
                new Matrixf()
                .Translate(0.5f, 0, 0.5f)
                .RotateYDeg(block.Shape.rotateY)
                .Translate(x - 0.5f, y, z - 0.5f)
                .Translate(-0.5f, 0, -0.5f)
                .Values;
        }

        return tfMatrices;
    }
}
