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
    /// Determines whether the collectible in the given slot has the specified attribute set to true, indicating that it is allowed to be stored.
    /// </summary>
    public static bool CanStoreInSlot(this ItemSlot slot, string attributeWhitelist)
        => slot?.Itemstack?.Collectible?.Attributes?[attributeWhitelist].AsBool() == true;

    /// <summary>
    /// Determines whether the collectible has the specified attribute set to true, allowing it to be stored in compatible containers.
    /// </summary>
    public static bool CanStoreInSlot(this CollectibleObject obj, string attributeWhitelist) 
        => obj?.Attributes?[attributeWhitelist].AsBool() == true;

    /// <summary>
    /// Determines if the item is considered a large item, based on baking properties, "shelvable" attribute, or specific known basket block types.
    /// </summary>
    public static bool IsLargeItem(this ItemStack stack) {
        var collectible = stack?.Collectible;
        if (collectible == null) return false;

        if (collectible.GetCollectibleInterface<IShelvable>()?.GetShelvableType(stack) == EnumShelvableLayout.SingleCenter) return true;
        if (collectible.GetCollectibleInterface<IShelvable>()?.GetShelvableType(stack) == EnumShelvableLayout.Halves) return true;
        if (stack.ItemAttributes["shelvable"]?.ToString() == "SingleCenter") return true;
        if (stack.ItemAttributes["shelvable"]?.ToString() == "Halves") return true;

        if (collectible.Code.Path.StartsWith("claypot-") == true) return true;
        
        if (collectible is BaseFSBasket) return true;

        return false;
    }

    /// <summary>
    /// Determines if the item is considered a small item, such as bars or patties, using wildcard matches for known modded food items.
    /// </summary>
    public static bool IsSmallItem(this ItemStack stack) {
        string stackCode = stack?.Collectible.Code.ToString() ?? "";

        if (WildcardUtil.Match("wildcraftfruit:nut-*bar", stackCode)) return true;
        if (WildcardUtil.Match("expandedfoods:fruitbar-*", stackCode)) return true;
        if (WildcardUtil.Match("expandedfoods:cookedveggie-*", stackCode)) return true;

        if (stackCode == "pemmican:pemmican-pack") return false;
        if (stackCode == "pemmican:chips-pack") return false;
        if (stackCode == "pemmican:mushroompatebar") return true;
        
        if (WildcardUtil.Match("*pemmican-*", stackCode)) return true;
        if (WildcardUtil.Match("*vegetable-pumpkin", stackCode)) return true;

        return false;
    }

    /// <summary>
    /// Checks if two item stacks can coexist in the same slot (belong to a same group).<br/>
    /// Returns true unless one of them belongs to a group, in which case their groups must match.
    /// </summary>
    public static bool BelongsToSameGroupAs(this ItemStack checkSlot, ItemStack currSlot) {
        if (checkSlot?.Collectible == null || currSlot?.Collectible == null)
            return true;

        string checkGroup = checkSlot.ItemAttributes?["fsGroup"]?.AsString();
        string currGroup = currSlot.ItemAttributes?["fsGroup"]?.AsString();

        if (string.IsNullOrEmpty(checkGroup) && string.IsNullOrEmpty(currGroup))
            return true;

        if (!string.IsNullOrEmpty(checkGroup) && checkGroup == currGroup)
            return true;

        return false;
    }
}