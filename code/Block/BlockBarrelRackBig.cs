using System.Linq;

namespace FoodShelves;

public class BlockBarrelRackBig : BlockLiquidContainerBase, IMultiBlockColSelBoxes {
    public override bool AllowHeldLiquidTransfer => false;
    public override int GetContainerSlotId(BlockPos pos) => 1;
    public override int GetContainerSlotId(ItemStack containerStack) => 1;

    public override void OnLoaded(ICoreAPI api) {
        base.OnLoaded(api);
        PlacedPriorityInteract = true; // Needed to call OnBlockInteractStart when shifting with an item in hand
    }

    public override int GetRetention(BlockPos pos, BlockFacing facing, EnumRetentionType type) {
        return 0; // To prevent the block reducing the cellar rating
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
        if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityBarrelRackBig hbrb) return hbrb.OnInteract(byPlayer, blockSel);
        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public bool BaseOnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer) {
        if (world.BlockAccessor.GetBlockEntity(selection.Position) is BlockEntityBarrelRackBig be && be.Inventory.Empty) return null;
        else return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
    }

    public override string GetHeldItemName(ItemStack itemStack) {
        string variantName = itemStack.GetMaterialNameLocalized(new[] { "type" }, new[] { "normal", "top" });
        return base.GetHeldItemName(itemStack) + " " + variantName;
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        dsc.AppendLine("");
        dsc.AppendLine(Lang.Get("foodshelves:helddesc-barrelrackbig"));
    }

    public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1) {
        // First, check for behaviors preventing default, for example Reinforcement system
        bool preventDefault = false;
        foreach (BlockBehavior behavior in BlockBehaviors) {
            EnumHandling handled = EnumHandling.PassThrough;

            behavior.OnBlockBroken(world, pos, byPlayer, ref handled);
            if (handled == EnumHandling.PreventDefault) preventDefault = true;
            if (handled == EnumHandling.PreventSubsequent) return;
        }

        if (preventDefault) return;

        // Drop barrel
        BlockEntityBarrelRackBig be = GetBlockEntity<BlockEntityBarrelRackBig>(pos);
        be?.Inventory.DropAll(pos.ToVec3d());

        // Spawn liquid particles
        if (world.Side == EnumAppSide.Server && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)) {
            ItemStack[] array = new ItemStack[1] { OnPickBlock(world, pos) };
            for (int j = 0; j < array.Length; j++) {
                world.SpawnItemEntity(array[j], new Vec3d(pos.X + 0.5, pos.Y + 0.5, pos.Z + 0.5));
            }

            world.PlaySoundAt(Sounds.GetBreakSound(byPlayer), pos.X, pos.Y, pos.Z, byPlayer);
        }

        world.BlockAccessor.SetBlock(0, pos);
    }

    // Selection box for master block
    public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos) {
        return base.GetSelectionBoxes(blockAccessor, pos);
    }

    // Selection boxes for multiblock parts
    public Cuboidf[] MBGetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset) {
        BlockEntityBarrelRackBig be = blockAccessor.GetBlockEntityExt<BlockEntityBarrelRackBig>(pos);
        if (be != null) {
            Cuboidf currentSelBox = base.GetSelectionBoxes(blockAccessor, pos).FirstOrDefault().Clone();
            currentSelBox.MBNormalizeSelectionBox(offset);

            return new Cuboidf[] { currentSelBox };
        }

        return base.GetSelectionBoxes(blockAccessor, pos);
    }

    public Cuboidf[] MBGetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset) {
        return base.GetCollisionBoxes(blockAccessor, pos);
    }

    public override void TryFillFromBlock(EntityItem byEntityItem, BlockPos pos) {
        // Don't fill when dropped as item in water
    }

    public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer) {
        StringBuilder dsc = new();

        BlockEntityBarrelRackBig be = GetBlockEntity<BlockEntityBarrelRackBig>(pos);
        if (be != null && be.Inventory.Empty) dsc.Append(Lang.Get("foodshelves:Missing barrel."));
        else dsc.Append(base.GetPlacedBlockInfo(world, pos, forPlayer));

        return dsc.ToString();
    }
}
