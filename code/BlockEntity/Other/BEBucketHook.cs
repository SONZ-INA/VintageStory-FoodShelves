namespace FoodShelves;

public class BEBucketHook : BEBaseFSContainer {
    protected override string CantPlaceMessage => "foodshelves:Only buckets can be placed on this hook.";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByBlock;

    public BEBucketHook() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck)); }

    protected override float[][] genTransformationMatrices() {
        return TransformationGenerator.Generate(this, td => {
            td.x = 0.15f;
            td.y = 0.2f;
            td.z = 0.175f;
        });
    }
}
