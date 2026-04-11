namespace FoodShelves;

public class BEEggBasket : BEBaseFSBasket {
    protected override string CeilingAttachedUtil => ShapeReferences.utilEggBasket;
    protected override string CantPlaceMessage => "foodshelves:Only eggs can be placed in this basket.";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByBlockAverageAndSoonest;
    
    public override int ItemsPerSegment => 12;

    public BEEggBasket() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck)); }

    protected override float[][] genTransformationMatrices() {
        float[,] transformationMatrix = block.GetTransformationMatrix();

        return TransformationGenerator.GenerateExplicit(this, transformationMatrix, (t) => {
            t.preRotate = MeshAngle * GameMath.RAD2DEG;
            t.offsetY = 0.0275f;
        });
    }
}
