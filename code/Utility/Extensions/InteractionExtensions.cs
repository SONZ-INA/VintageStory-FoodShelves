namespace FoodShelves;

public static class InteractionExtensions {
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
