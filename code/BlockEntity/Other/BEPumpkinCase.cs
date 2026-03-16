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
        return TransformationGenerator.Generate(this, td => {
            td.x = 0.155f;
            td.y = 0.06f;
            td.z = 0.205f;
        });
    }
}
