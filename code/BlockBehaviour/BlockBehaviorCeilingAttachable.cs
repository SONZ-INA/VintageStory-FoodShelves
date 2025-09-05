namespace FoodShelves;

public class BlockBehaviorCeilingAttachable(Block block) : BlockBehavior(block) {
    public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode) {
        handling = EnumHandling.PreventDefault;

        if (blockSel.Face == BlockFacing.DOWN) {
            if (TryAttachTo(world, byPlayer, blockSel, itemstack, ref failureCode)) return true;
        }

        (world.Api as ICoreClientAPI)?.TriggerIngameError(this, "cantplace", Lang.Get("foodshelves:placefailure-requireceilingattachable"));
        failureCode = "__ignore__";

        return false;
    }

    private bool TryAttachTo(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack itemstack, ref string failureCode) {
        BlockPos attachingBlockPos = blockSel.Position.AddCopy(blockSel.Face.Opposite);
        Block attachingBlock = world.BlockAccessor.GetBlock(attachingBlockPos);

        if (attachingBlock is BlockChisel) return false;

        if (blockSel.Face == BlockFacing.DOWN) {
            if (attachingBlock.SideSolid[BlockFacing.DOWN.Index]) {
                block.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
                return true;
            }
        }

        return false;
    }

    public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handled) {
        handled = EnumHandling.PreventDefault;

        if (!CanBlockStay(world, pos)) {
            world.BlockAccessor.BreakBlock(pos, null);
        }
    }

    private bool CanBlockStay(IWorldAccessor world, BlockPos pos) {
        Block attachingBlock = world.BlockAccessor.GetBlock(pos.UpCopy());
        return attachingBlock.CanAttachBlockAt(world.BlockAccessor, block, pos, BlockFacing.DOWN);
    }

    public override bool CanAttachBlockAt(IBlockAccessor world, Block block, BlockPos pos, BlockFacing blockFace, ref EnumHandling handled, Cuboidi attachmentArea = null) {
        handled = EnumHandling.PreventDefault;
        return false;
    }
}