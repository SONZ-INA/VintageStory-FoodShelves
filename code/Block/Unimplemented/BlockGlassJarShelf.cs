namespace FoodShelves;

public class BlockGlassJarShelf : Block {
    public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos) {
        return true;
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
        if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityGlassJarShelf beshelf) return beshelf.OnInteract(byPlayer, blockSel);
        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    //public override string GetHeldItemName(ItemStack itemStack) {
    //    string variantType = "";
    //    if (this.Code.SecondCodePart().StartsWith("short"))
    //        variantType = Lang.Get("foodshelves:Short") + " ";

    //    return variantType + base.GetHeldItemName(itemStack) + " " + itemStack.GetMaterialNameLocalizedOLD();
    //}
}
