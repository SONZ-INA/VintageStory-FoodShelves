namespace FoodShelves;

public class BEFruitBasket : BEBaseFSBasket {
    protected override string CeilingAttachedUtil => ShapeReferences.utilFruitBasket;
    protected override string CantPlaceMessage => "foodshelves:Only fruit can be placed in this basket.";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByBlockAverageAndSoonest;

    public override int SlotCount => 22;

    public BEFruitBasket() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck)); }

    protected override float[][] genTransformationMatrices() {
        float[,] transformationMatrix = block.GetTransformationMatrix();
        float[][] tfMatrices = new float[SlotCount][];

        for (int item = 0; item < SlotCount; item++) {
            tfMatrices[item] =
                new Matrixf()
                .Translate(0.5f, 0, 0.5f)
                .RotateYDeg(block.Shape.rotateY + MeshAngle * GameMath.RAD2DEG)
                .RotateXDeg(transformationMatrix[3, item])
                .RotateYDeg(transformationMatrix[4, item])
                .RotateZDeg(transformationMatrix[5, item])
                .Scale(0.5f, 0.5f, 0.5f)
                .Translate(transformationMatrix[0, item] - 0.84375f, transformationMatrix[1, item] + 0.1f, transformationMatrix[2, item] - 0.8125f)
                .Values;
        }

        return tfMatrices;
    }
}
