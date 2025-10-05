namespace FoodShelves;

public class BEPumpkinCase : BEBaseFSContainer {
    protected override string CantPlaceMessage => "foodshelves:Only pumpkins can be placed in this case.";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByBlock;

    public BEPumpkinCase() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck)); }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
        InitMesh(); // Re-meshing the falling block
        return base.OnTesselation(mesher, tesselator);
    }

    protected override float[][] genTransformationMatrices() {
        float[][] tfMatrices = new float[SlotCount][];

        tfMatrices[0] = new Matrixf()
            .Translate(0.5f, 0, 0.5f)
            .RotateYDeg(block.Shape.rotateY)
            .Translate(-0.5f, 0.06f, -0.5f)
            .Values;

        return tfMatrices;
    }
}
