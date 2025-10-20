using System.Linq;

namespace FoodShelves;

public static class InventoryExtensions {
    /// <summary>
    /// Rebuilds the inventory for a block entity container, preserving existing item stacks and
    /// recreating slots using the specified max stack size.<br/> Must be called before inventory initialization!
    /// </summary>
    public static void RebuildInventory(this BEBaseFSContainer be, ICoreAPI api, int maxSlotStackSize = 1) {
        ItemStack[] stack = be.inv.Select(slot => slot.Itemstack).ToArray();

        be.inv = new InventoryGeneric(be.SlotCount, be.inv.ClassName + "-0", api, (_, inv) => new ItemSlotFSUniversal(inv, be.AttributeCheck, maxSlotStackSize));

        for (int i = 0; i < be.SlotCount; i++) {
            if (i >= stack.Length) break;
            be.inv[i].Itemstack = stack[i];
        }

        be.inv.LateInitialize(be.inv.InventoryID, api);
    }

    /// <summary>
    /// Retrieves the contents of an item container by reading its "contents" attribute. Falls back to resolving legacy "ucontents" if necessary.
    /// </summary>
    public static ItemStack[] GetContents(IWorldAccessor world, ItemStack itemstack) {
        ITreeAttribute treeAttr = itemstack?.Attributes?.GetTreeAttribute("contents");
        if (treeAttr == null) {
            return ResolveUcontents(world, itemstack);
        }

        ItemStack[] stacks = new ItemStack[treeAttr.Count];
        foreach (var val in treeAttr) {
            ItemStack stack = (val.Value as ItemstackAttribute).value;
            stack?.ResolveBlockOrItem(world);

            if (int.TryParse(val.Key, out int index)) stacks[index] = stack;
        }

        return stacks;
    }

    /// <summary>
    /// Stores an array of item stacks into an item container by setting its "contents" attribute. If the array is null or empty, the contents attribute is removed.
    /// </summary>
    public static void SetContents(ItemStack containerStack, ItemStack[] stacks) {
        if (stacks == null || stacks.Length == 0) {
            containerStack.Attributes.RemoveAttribute("contents");
            return;
        }

        TreeAttribute stacksTree = new();
        for (int i = 0; i < stacks.Length; i++) {
            stacksTree[i + ""] = new ItemstackAttribute(stacks[i]);
        }

        containerStack.Attributes["contents"] = stacksTree;
    }

    /// <summary>
    /// Converts legacy, serialized "ucontents" stored in a JSON-like format into resolved <see cref="ItemStack"/> objects.<br/>
    /// Intended for loading container contents that were serialized for persistence.
    /// </summary>
    public static ItemStack[] ResolveUcontents(IWorldAccessor world, ItemStack itemstack) {
        if (itemstack?.Attributes.HasAttribute("ucontents") == true) {
            List<ItemStack> stacks = [];

            var attrs = itemstack.Attributes["ucontents"] as TreeArrayAttribute;

            foreach (ITreeAttribute stackAttr in attrs.value) {
                stacks.Add(CreateItemStackFromJson(stackAttr, world, itemstack.Collectible.Code.Domain));
            }
            ItemStack[] stacksAsArray = [.. stacks];
            SetContents(itemstack, stacksAsArray);
            itemstack.Attributes.RemoveAttribute("ucontents");

            return stacksAsArray;
        }

        return [];
    }

    /// <summary>
    /// Converts an attribute-based JSON-like representation of an <see cref="ItemStack"/> into a fully object using the game's item/block registry.<br/>
    /// Used for deserializing data from the "ucontents" attribute.
    /// </summary>
    private static ItemStack CreateItemStackFromJson(ITreeAttribute stackAttr, IWorldAccessor world, string defaultDomain) {
        CollectibleObject collObj;
        var loc = AssetLocation.Create(stackAttr.GetString("code"), defaultDomain);
        if (stackAttr.GetString("type") == "item") {
            collObj = world.GetItem(loc);
        }
        else {
            collObj = world.GetBlock(loc);
        }

        ItemStack stack = new(collObj, (int)stackAttr.GetDecimal("quantity", 1));
        var attr = (stackAttr["attributes"] as TreeAttribute)?.Clone();
        if (attr != null) stack.Attributes = attr;

        return stack;
    }

    /// <summary>
    /// Converts an array of item stacks into dummy slots for GUI display or non-interactive purposes.
    /// </summary>
    public static DummySlot[] ToDummySlots(this ItemStack[] contents) {
        if (contents == null || contents.Length == 0) return [];

        DummySlot[] dummySlots = new DummySlot[contents.Length];
        for (int i = 0; i < contents.Length; i++) {
            dummySlots[i] = new DummySlot(contents[i]?.Clone());
        }

        return dummySlots;
    }

    /// <summary>
    /// Synchronizes a single transition type progress across all slots in the inventory.
    /// </summary>
    public static void SyncTransitionType(this InventoryBase inv, ICoreAPI api, EnumTransitionType type) {
        float totalProgress = 0f;
        int totalCount = 0;
        bool foundAny = false;

        // Collect transition data
        foreach (var slot in inv) {
            if (slot.Empty) continue;

            var stack = slot.Itemstack;
            var collectible = stack.Collectible;
            var state = collectible.UpdateAndGetTransitionState(api.World, slot, type);
            if (state == null) continue;

            foundAny = true;

            float progress = state.TransitionedHours;

            totalProgress += progress * slot.StackSize;
            totalCount += slot.StackSize;
        }

        if (!foundAny || totalCount == 0)
            return;

        float avgProgress = totalProgress / totalCount;

        // Apply averaged values to all stacks
        foreach (var slot in inv) {
            if (slot.Empty) continue;

            var stack = slot.Itemstack;
            var collectible = stack.Collectible;
            var state = collectible.UpdateAndGetTransitionState(api.World, slot, type);
            if (state == null) continue;

            collectible.SetTransitionState(stack, type, avgProgress);
        }
    }
}
