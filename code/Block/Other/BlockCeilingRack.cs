namespace FoodShelves;

public class BlockCeilingRack : BaseFSContainer {

    public override WorldInteraction[]? GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer) {
        if (world.BlockAccessor.GetBlockEntity(selection.Position) is BECeilingRack becr) {
            if (becr.inv[1].Empty) {
                return null;
            }
        }

        return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
    }

    public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos) {
        BECeilingRack? be = blockAccessor.GetBlockEntity<BECeilingRack>(pos);
        if (be != null) {
            if (!be.inv[1].Empty) {
                return [new(0, 0, 0, 1, 1, 1)];
            }
        }

        return base.GetSelectionBoxes(blockAccessor, pos);
    }

    public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos) {
        BECeilingRack? be = blockAccessor.GetBlockEntity<BECeilingRack>(pos);
        if (be != null) {
            if (!be.inv[1].Empty) {
                return [new(0, 0, 0, 1, 1, 1)];
            }
        }

        return base.GetCollisionBoxes(blockAccessor, pos);
    }

    public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer) {
        StringBuilder dsc = new();

        BECeilingRack? be = GetBlockEntity<BECeilingRack>(pos);

        if (be?.Inventory.Empty == true) {
            dsc.Append(Lang.Get("foodshelves:Missing large jar."));
        }
        else {
            dsc.Append(base.GetPlacedBlockInfo(world, pos, forPlayer));
        }

        return dsc.ToString();
    }
}
