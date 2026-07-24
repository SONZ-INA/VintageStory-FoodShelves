namespace FoodShelves;

public class BlockJarStand : BaseFSContainer {
    public override void OnLoaded(ICoreAPI api) {
        base.OnLoaded(api);

        itemSlottableInteractions = [
            new() {
                ActionLangCode = "game:blockhelp-behavior-rightclickpickup",
                MouseButton = EnumMouseButton.Right,
                HotKeyCode = "shift",
                RequireFreeHand = true
            },
        ];
    }

    public override WorldInteraction[]? GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer) {
        if (world.BlockAccessor.GetBlockEntity(selection.Position) is BEJarStand be) {
            int segmentIndex = selection.SelectionBoxIndex;

            if (segmentIndex < be.inv.Count && !be.inv[segmentIndex].Empty) {
                ItemSlot jarSlot = be.inv[segmentIndex];

                if (jarSlot.Itemstack?.Collectible is IContainedInteractable ici) {
                    WorldInteraction[] jarHelp = itemSlottableInteractions.Append(ici.GetContainedInteractionHelp(be, jarSlot, forPlayer, selection));

                    if (jarHelp != null && jarHelp.Length > 0) {
                        return jarHelp;
                    }
                }
            }
        }

        return null;
    }
}
