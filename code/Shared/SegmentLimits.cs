namespace FoodShelves;

/// <summary>
/// Utility class that provides dynamic rules for determining how many items or stacks
/// can fit in a container which has a higher segment storage limit, for the purposes
/// of using that limit with special rules.
/// </summary>
public static class SegmentLimits {
    /// <summary>
    /// LTF Packed Food - 5 <br />
    /// Large Item      - 1 <br />
    /// Medium Item     - 2 <br />
    /// Small Item      - be.ItemsPerSegment <br />
    /// Other           - 4
    /// </summary>
    public static int Mixed(BEBaseFSContainer be, ItemStack? stack) {
        if (stack == null) return 0;

        string itemCode = stack.Collectible?.Code.Path ?? "";

        string[] collectibleCodes = ["pemmican-pack", "chips-pack", "mushroom-pack", "nutri-pack"];
        if (collectibleCodes.Contains(itemCode)) return 5;

        if (stack.IsLargeItem()) return 1;
        if (stack.IsMediumItem()) return 2;
        if (stack.IsSmallItem()) return be.ItemsPerSegment;

        // Other items, like crocks
        return 4;
    }
}
