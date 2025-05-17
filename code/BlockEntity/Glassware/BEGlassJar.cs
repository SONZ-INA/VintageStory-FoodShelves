namespace FoodShelves;

public class BEGlassJar : BEBaseFSContainer {
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByBlockMerged;
    public override int SlotCount => 2;

    public BEGlassJar() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, "fsLiquidyStuff")); }

    public override bool OnInteract(IPlayer byPlayer, BlockSelection blockSel) {
        return false;
    }

    protected override float[][] genTransformationMatrices() { return null; } // Unneeded
}
