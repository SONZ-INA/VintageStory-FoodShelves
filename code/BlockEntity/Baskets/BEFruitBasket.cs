namespace FoodShelves;

public class BEFruitBasket : BEBaseFSBasket {
    protected override string CeilingAttachedUtil => ShapeReferences.utilFruitBasket;
    protected override string CantPlaceMessage => "foodshelves:Only fruit can be placed in this basket.";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByBlockAverageAndSoonest;

    public override int ItemsPerSegment => 22;

    public BEFruitBasket() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck)); }

    protected override float[][] genTransformationMatrices() {
        float[,] transformationMatrix = block.GetTransformationMatrix();

        return TransformationGenerator.GenerateExplicit(this, transformationMatrix, t => {
            t.preRotate = MeshAngle * GameMath.RAD2DEG;
            t.scaleX = t.scaleY = t.scaleZ = 0.5f;
            t.offsetY = 0.05f;
        });
    }
}
