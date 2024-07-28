﻿namespace FoodShelves;

public class BlockHorizontalBarrel : Block {
    public override void OnLoaded(ICoreAPI api) {
        base.OnLoaded(api);
    }

    public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode) {
        (api as ICoreClientAPI).TriggerIngameError(this, "cantplace", Lang.Get("foodshelves:This barrel needs to be placed in a barrel rack."));
        failureCode = "__ignore__";
        return false;
    }
}
