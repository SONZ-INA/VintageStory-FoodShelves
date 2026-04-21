namespace FoodShelves;

public static class InteractionExtensions {
    /// <summary>
    /// Puts the 'source' ItemSlot into the 'target' ItemSlot, without accounting for the Collectible's MaxStackSize.
    /// Merges the collectibles together, and handles the transition states merging.
    /// </summary>
    public static int TryPutIntoBulk(this ItemSlot source, IWorldAccessor world, ItemSlot target, int quantity = 1) {
        if (source.Empty) return 0;

        if (!target.CanHold(source))
            return 0;

        if (!target.Empty && !target.Itemstack.Collectible.Equals(source.Itemstack?.Collectible))
            return 0;

        int freeSlotsCount = target.MaxSlotStackSize - target.StackSize;
        if (freeSlotsCount <= 0) return 0;

        int moveAmount = Math.Min(quantity, Math.Min(freeSlotsCount, source.StackSize));

        // Initialize slots
        if (target.Empty) {
            target.Itemstack = source.TakeOut(moveAmount);
            target.OnItemSlotModified(target.Itemstack);
            source.OnItemSlotModified(source.Itemstack);
            return moveAmount;
        }

        ItemStack sink = target.Itemstack;
        ItemStack src = source.Itemstack;

        int sinkSizeBefore = sink.StackSize;

        // Transition States Merge
        var srcStates = src.Collectible.UpdateAndGetTransitionStates(world, source);
        var sinkStates = sink.Collectible.UpdateAndGetTransitionStates(world, target);

        if (srcStates != null && sinkStates != null) {
            float weight = (float)moveAmount / (moveAmount + sinkSizeBefore);

            foreach (var s in srcStates) {
                foreach (var t in sinkStates) {
                    if (s.Props.Type == t.Props.Type) {
                        float merged =
                            s.TransitionedHours * weight +
                            t.TransitionedHours * (1f - weight);

                        sink.Collectible.SetTransitionState(sink, s.Props.Type, merged);
                    }
                }
            }
        }

        sink.StackSize += moveAmount;
        source.TakeOut(moveAmount);

        target.OnItemSlotModified(target.Itemstack);
        source.OnItemSlotModified(source.Itemstack);

        return moveAmount;
    }

    /// <summary>
    /// Handles block placement feedback by triggering the first-person interaction animation, playing the placement sound, and marking the block entity as dirty.
    /// </summary>
    public static bool HandlePlacementEffects(this BlockEntity be, ItemStack? stack, IPlayer byPlayer, bool redraw = false) {
        SoundAttributes? sound = stack?.Block?.Sounds?.Place;

        be.Api.World.PlaySoundAt(sound ?? GlobalConstants.DefaultBuildSound, byPlayer, byPlayer);

        if (be.Api is ICoreClientAPI capi)
            capi.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);

        be.MarkDirty(redraw);

        return true;
    }
}
