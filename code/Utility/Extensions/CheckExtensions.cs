namespace FoodShelves;

public static class CheckExtensions {
    /// <summary>
    /// Determines whether the given collectible object matches any of the allowed types specified in the <see cref="RestrictionData"/>.<br/>
    /// This check compares the object's runtime class name (e.g. <c>BlockCheese</c>) against entries in the <c>CollectibleTypes</c> list
    /// using reflection.
    /// </summary>
    public static bool CheckTypedRestriction(this CollectibleObject obj, RestrictionData data) 
        => data.CollectibleTypes?.Contains(obj.Code.Domain + ":" + obj.GetType().Name) == true;

    /// <summary>
    /// Determines whether the collectible in the given slot has the specified attribute set to true,
    /// indicating that it is allowed to be stored.<br/> Prevents storage if the slot belongs to a hopper.
    /// </summary>
    public static bool CanStoreInSlot(this ItemSlot slot, string attributeWhitelist) {
        if (slot?.Itemstack?.Collectible?.Attributes?[attributeWhitelist].AsBool() == false) return false;
        if (slot?.Inventory?.ClassName == "hopper") return false;
        return true;
    }

    /// <summary>
    /// Determines whether the collectible has the specified attribute set to true,
    /// allowing it to be stored in compatible containers.
    /// </summary>
    public static bool CanStoreInSlot(this CollectibleObject obj, string attributeWhitelist) {
        return obj?.Attributes?[attributeWhitelist].AsBool() == true;
    }

    /// <summary>
    /// Determines if the item is considered a large item, based on baking properties, "shelvable" attribute, or specific known basket block types.
    /// </summary>
    public static bool IsLargeItem(this ItemStack stack) {
        if (BakingProperties.ReadFrom(stack)?.LargeItem == true) return true;

        if (stack?.ItemAttributes["shelvable"]?.ToString() == "SingleCenter") return true;
        if (stack?.Collectible?.Code.Path.StartsWith("claypot-") == true) return true;
        
        string[] validTypes = ["BlockFruitBasket", "BlockVegetableBasket", "BlockEggBasket"];
        if (validTypes.Contains(stack?.Collectible?.GetType().Name)) return true;

        return false;
    }

    /// <summary>
    /// Determines if the item is considered a small item, such as bars or patties, using wildcard matches for known modded food items.
    /// </summary>
    public static bool IsSmallItem(this ItemStack stack) {
        string stackCode = stack?.Collectible.Code.ToString() ?? "";

        if (WildcardUtil.Match("wildcraftfruit:nut-*bar", stackCode)) return true;
        if (WildcardUtil.Match("expandedfoods:fruitbar-*", stackCode)) return true;
        if (stack?.Collectible.Code == "pemmican:pemmican-pack") return false;
        if (WildcardUtil.Match("*pemmican-*", stackCode)) return true;
        if (stack?.Collectible.Code == "pemmican:mushroompatebar") return true;
        if (WildcardUtil.Match("*vegetable-pumpkin", stackCode)) return true;
        if (WildcardUtil.Match("expandedfoods:cookedveggie-*", stackCode)) return true;

        return false;
    }

    /// <summary>
    /// Checks if two item stacks can coexist in the same slot.<br/>
    /// Returns true unless one of them is a solitary item, in which case their codes must match.
    /// </summary>
    public static bool IsSolitaryMatch(this ItemStack checkSlot, ItemStack currSlot) {
        if (checkSlot?.Collectible == null || currSlot?.Collectible == null) return true;

        string[] solitaryItems = ["pemmican:pemmican-pack"];

        bool solitary = false;
        foreach (string item in solitaryItems) {
            if (checkSlot.Collectible.Code == item || currSlot.Collectible.Code == item) {
                solitary = true;
                break;
            }
        }

        if (solitary) { // Return false only if it should be solitary and the codes don't match.
            return checkSlot.Collectible.Code == currSlot.Collectible.Code;
        }

        return true;
    }
}