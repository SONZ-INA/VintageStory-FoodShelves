namespace FoodShelves;

public class BlockJarStand : BaseFSContainer {
    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer) {
        WorldInteraction[] baseHelp = base.GetPlacedBlockInteractionHelp(world, selection, forPlayer) 
            ?? [];

        if (world.BlockAccessor.GetBlockEntity(selection.Position) is BEJarStand be) {
            int segmentIndex = selection.SelectionBoxIndex;

            if (segmentIndex < be.inv.Count && !be.inv[segmentIndex].Empty) {
                ItemSlot jarSlot = be.inv[segmentIndex];

                if (jarSlot.Itemstack?.Collectible is IContainedInteractable ici) {
                    WorldInteraction[] jarHelp = ici.GetContainedInteractionHelp(be, jarSlot, forPlayer, selection);

                    if (jarHelp != null && jarHelp.Length > 0) {
                        return [..baseHelp, ..jarHelp];
                    }
                }
            }
        }

        return baseHelp;
    }
}
