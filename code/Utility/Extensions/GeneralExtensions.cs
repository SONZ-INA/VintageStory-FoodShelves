namespace FoodShelves;

public static class GeneralExtensions {
    /// <summary>
    /// Gets the upper storage limit for the per-segment item placement.
    /// </summary>
    public static int GetSegmentLimit(this BEBaseFSContainer be, ItemStack? stack) {
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

    /// <summary>
    /// Counts the occupied slots in the Segment of an Inventory.
    /// </summary>
    public static int CountItemsInSegment(this BEBaseFSContainer be, int startIndex) {
        int count = 0;

        for (int i = 0; i < be.ItemsPerSegment; i++) {
            if (!be.inv[startIndex + i].Empty) {
                count += be.inv[startIndex + i].Itemstack?.StackSize ?? 0;
            }
        }

        return count;
    }
}

