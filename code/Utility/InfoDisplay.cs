using System.Linq;

namespace FoodShelves;

public static class InfoDisplay {
    public enum InfoDisplayOptions {
        ByBlock,
        ByShelf,
        BySegment,
        BySegmentGrouped,
        ByBlockAverageAndSoonest,
        ByBlockMerged
    }

    public static string GetNameAndStackSize(ItemStack stack) => stack.GetName() + " x" + stack.StackSize;
    public static string GetAmountOfLiters(ItemStack stack) => stack.GetName() + " (" + (float)stack.StackSize / 100 + " L)";

    public static void DisplayPerishMultiplier(float perishMul, StringBuilder dsc, InWorldContainer container = null) {
        container?.ReloadRoom();
        dsc.AppendLine(Lang.Get("Stored food perish speed: {0}x", Math.Round(perishMul, 2)));
    }

    public static void DisplayInfo(IPlayer forPlayer, StringBuilder sb, InventoryGeneric inv, InfoDisplayOptions displaySelection, int slotCount, int segmentsPerShelf = 0, int itemsPerSegment = 0, bool skipLine = true, int skipSlotsFrom = -1, int selectedSegment = -1) {
        if (skipLine) sb.AppendLine(); // Space in between to be in line with vanilla

        IWorldAccessor world = inv.Api.World;

        List<ItemSlot> itemSlotList = new();
        foreach (var slot in inv) {
            itemSlotList.Add(slot);
        }
        
        switch (displaySelection) {
            case InfoDisplayOptions.ByBlockAverageAndSoonest:
                sb.Append(PerishableInfoAverageAndSoonest(itemSlotList.ToArray(), world));
                return;
            case InfoDisplayOptions.ByBlockMerged:
                ByBlockMerged(itemSlotList.ToArray(), sb, world);
                return;
            case InfoDisplayOptions.BySegmentGrouped:
                int fromSlot = forPlayer.CurrentBlockSelection.SelectionBoxIndex * itemsPerSegment;
                sb.Append(PerishableInfoGrouped(inv, world, fromSlot, fromSlot + itemsPerSegment));
                return;
        }

        if (selectedSegment == -1 && forPlayer.CurrentBlockSelection != null)
            selectedSegment = forPlayer.CurrentBlockSelection.SelectionBoxIndex;

        if (displaySelection != InfoDisplayOptions.ByBlock && selectedSegment == -1) return;

        int start = 0, end = slotCount;

        switch (displaySelection) {
            case InfoDisplayOptions.ByBlock:
                start = slotCount - 1;
                end = -1;
                break;
            case InfoDisplayOptions.ByShelf:
                int itemsPerShelf = segmentsPerShelf * itemsPerSegment;
                int selectedShelf = selectedSegment / segmentsPerShelf * itemsPerShelf;
                start = selectedShelf;
                end = selectedShelf + itemsPerShelf;
                break;
            case InfoDisplayOptions.BySegment:
                start = selectedSegment * itemsPerSegment;
                end = start + itemsPerSegment;
                break;
        }

        for (int i = start; i != end; i = displaySelection == InfoDisplayOptions.ByBlock ? i - 1 : i + 1) {
            if (i >= slotCount) break;
            if (skipSlotsFrom != -1 && i >= skipSlotsFrom) break;
            if (inv[i].Empty) continue;

            ItemStack stack = inv[i].Itemstack;
            float ripenRate = stack.Collectible.GetTransitionRateMul(world, inv[i], EnumTransitionType.Ripen); // Get ripen rate

            if (stack.Collectible.TransitionableProps?.Length > 0) {
                if (IsSmallItem(stack)) {
                    sb.Append(PerishableInfoGrouped(inv, world, i, end));
                    return;
                }

                sb.Append(PerishableInfoCompact(world, inv[i], ripenRate));
            }
            else if (stack.Collectible is BlockCrock) {
                sb.Append(CrockInfoCompact(inv, world, inv[i]));
            }
            else if (stack.Collectible is BaseFSBasket) {
                sb.AppendLine(stack.GetName());
                ItemStack[] contents = GetContents(world, stack);
                float perishMul = inv.GetTransitionSpeedMul(EnumTransitionType.Perish, new ItemSlot(inv).Itemstack);
                sb.AppendLine("<font color=\"#989898\">" + PerishableInfoAverageAndSoonest(contents.ToDummySlots(), world, perishMul) + "</font>");
            }
            else {
                sb.Append(stack.GetName());
                if (stack.StackSize > 1) sb.Append(" x" + stack.StackSize);
                sb.AppendLine();
            }
        }
    }

    public static string GetUntilMelted(ItemSlot slot) {
        if (slot.Empty) return "";

        IWorldAccessor world = slot.Inventory.Api.World;

        TransitionState meltTransitionState = slot.Itemstack.Collectible.UpdateAndGetTransitionState(world, slot, EnumTransitionType.Melt);
        if (meltTransitionState != null) {
            float meltRate = slot.Itemstack.Collectible.GetTransitionRateMul(world, slot, EnumTransitionType.Melt);
            if (meltRate <= 0) return Lang.Get("foodshelves:Will not melt");

            double hoursLeft = meltTransitionState.TransitionHours / meltRate * (1 - meltTransitionState.TransitionLevel);
            return GetTimeRemainingText(world, hoursLeft, EnumTransitionType.Melt);
        }

        return Lang.Get("foodshelves:Will not melt");
    }

    public static string PerishableInfoCompact(IWorldAccessor world, ItemSlot contentSlot, float ripenRate, bool withStackName = true, bool withStackSize = true) {
        if (contentSlot.Empty) return "";

        StringBuilder dsc = new();
        if (withStackName) dsc.Append(contentSlot.Itemstack.GetName());
        if (withStackSize && contentSlot.StackSize > 1) dsc.Append(" x" + contentSlot.StackSize);

        TransitionState[] transitionStates = contentSlot.Itemstack?.Collectible.UpdateAndGetTransitionStates(world, contentSlot);

        bool nowSpoiling = false;

        if (transitionStates != null) {
            for (int i = 0; i < transitionStates.Length; i++) {
                TransitionState state = transitionStates[i];
                TransitionableProperties prop = state.Props;
                float perishRate = contentSlot.Itemstack.Collectible.GetTransitionRateMul(world, contentSlot, prop.Type);

                if (perishRate <= 0) continue;

                float transitionLevel = state.TransitionLevel;
                float freshHoursLeft = state.FreshHoursLeft / perishRate;

                switch (prop.Type) {
                    case EnumTransitionType.Perish:
                        if (transitionLevel > 0) {
                            nowSpoiling = true;
                            dsc.Append(", " + Lang.Get("{0}% spoiled", (int)Math.Round(transitionLevel * 100)));
                        }
                        else {
                            dsc.Append(", " + GetTimeRemainingText(world, freshHoursLeft, prop.Type, transitionLevel));
                        }
                        break;
                    case EnumTransitionType.Ripen:
                        if (nowSpoiling) break;

                        if (transitionLevel > 0) {
                            dsc.Append(", " + Lang.Get("{1:0.#} days left to ripen ({0}%)", (int)Math.Round(transitionLevel * 100), (state.TransitionHours - state.TransitionedHours) / world.Calendar.HoursPerDay / ripenRate));
                        }
                        else {
                            dsc.Append(", " + GetTimeRemainingText(world, freshHoursLeft, prop.Type, transitionLevel));
                        }
                        break;
                }
            }
        }

        dsc.AppendLine();
        return dsc.ToString();
    }

    public static string PerishableInfoGrouped(InventoryGeneric inv, IWorldAccessor world, int start, int end) {
        if (inv.Empty) return "";

        StringBuilder dsc = new();
        Dictionary<string, List<ItemSlot>> grouped = new();

        // Group items by their name
        for (int i = start; i < end; i++) {
            if (i >= inv.Count || inv[i].Empty) continue;

            ItemStack stack = inv[i].Itemstack;
            if (stack == null) continue;

            string itemKey = stack.GetName();

            if (!grouped.TryGetValue(itemKey, out List<ItemSlot> value)) {
                value = new List<ItemSlot>();
                grouped[itemKey] = value;
            }

            value.Add(inv[i]);
        }

        // Display grouped items with their count and average perish rate
        foreach (var group in grouped) {
            string itemName = group.Key;
            List<ItemSlot> slots = group.Value;
            int totalCount = 0;

            foreach (var slot in slots) {
                totalCount += slot.Itemstack.StackSize;
            }

            dsc.Append(itemName + " x" + totalCount);

            // Calculate and display perish information based on the first item's transition properties
            if (slots.Count > 0 && slots[0].Itemstack.Collectible.TransitionableProps != null &&
                slots[0].Itemstack.Collectible.TransitionableProps.Length > 0) {

                Dictionary<EnumTransitionType, List<double>> timeLeftByType = new();

                foreach (var slot in slots) {
                    TransitionState[] states = slot.Itemstack.Collectible.UpdateAndGetTransitionStates(world, slot);

                    if (states != null) {
                        foreach (TransitionState state in states) {
                            TransitionableProperties prop = state.Props;
                            float perishRate = slot.Itemstack.Collectible.GetTransitionRateMul(world, slot, prop.Type);

                            if (perishRate <= 0) continue;

                            float transitionLevel = state.TransitionLevel;
                            float freshHoursLeft = state.FreshHoursLeft / perishRate;

                            if (!timeLeftByType.TryGetValue(prop.Type, out List<double> value)) {
                                value = new List<double>();
                                timeLeftByType[prop.Type] = value;
                            }

                            if (prop.Type == EnumTransitionType.Perish || prop.Type == EnumTransitionType.Ripen) {
                                value.Add(freshHoursLeft);
                            }
                        }
                    }
                }

                // Display average perish info
                foreach (var entry in timeLeftByType) {
                    EnumTransitionType type = entry.Key;
                    List<double> hoursLeftList = entry.Value;

                    if (hoursLeftList.Count > 0) {
                        double avgHoursLeft = hoursLeftList.Average();

                        if (type == EnumTransitionType.Perish) {
                            dsc.Append(", " + GetTimeRemainingText(world, avgHoursLeft, type, 0, "foodshelves:Average freshness"));
                        }
                    }
                }
            }

            dsc.AppendLine();
        }

        return dsc.ToString();
    }

    public static string TransitionInfoCompact(IWorldAccessor world, ItemSlot contentSlot, EnumTransitionType transitionType) {
        if (contentSlot.Empty) return "";

        TransitionState[] transitionStates = contentSlot.Itemstack?.Collectible.UpdateAndGetTransitionStates(world, contentSlot);
        if (transitionStates == null) return "";

        TransitionState state = transitionStates.FirstOrDefault(s => s.Props.Type == transitionType);

        if (state != null) {
            float rateMul = contentSlot.Itemstack.Collectible.GetTransitionRateMul(world, contentSlot, transitionType);
            if (rateMul > 0) {
                if (state.TransitionLevel > 0) {
                    double hoursLeft = state.TransitionHours / rateMul * (1 - state.TransitionLevel);
                    return GetTimeRemainingText(world, hoursLeft, transitionType, state.TransitionLevel);

                }
                else {
                    float hoursLeft = state.FreshHoursLeft / rateMul;
                    return GetTimeRemainingText(world, hoursLeft, transitionType);
                }
            }
        }

        return "";
    }

    public static string CrockInfoCompact(InventoryGeneric inv, IWorldAccessor world, ItemSlot inSlot) {
        BlockMeal mealblock = world.GetBlock(new AssetLocation("bowl-meal")) as BlockMeal;
        BlockCrock crock = inSlot.Itemstack.Collectible as BlockCrock;
        CookingRecipe recipe = crock.GetCookingRecipe(world, inSlot.Itemstack);
        ItemStack[] stacks = crock.GetNonEmptyContents(world, inSlot.Itemstack);

        if (stacks == null || stacks.Length == 0) {
            return Lang.Get("Empty Crock") + "\n";
        }

        StringBuilder dsc = new();

        if (recipe != null) {
            double servings = inSlot.Itemstack.Attributes.GetDecimal("quantityServings");
            dsc.Append(Lang.Get("{0:0.#}x {1}.", servings, recipe.GetOutputName(world, stacks)));
        }
        else {
            int i = 0;
            foreach (var stack in stacks) {
                if (stack == null) continue;
                if (i++ > 0) dsc.Append(", ");
                dsc.Append(stack.StackSize + "x " + stack.GetName());
            }
            dsc.Append('.');
        }

        DummyInventory dummyInv = new(world.Api);
        ItemSlot contentSlot = BlockCrock.GetDummySlotForFirstPerishableStack(world, stacks, null, dummyInv);
        dummyInv.OnAcquireTransitionSpeed += (transType, stack, mul) => {
            return mul * crock.GetContainingTransitionModifierContained(world, inSlot, transType) * inv.GetTransitionSpeedMul(transType, stack);
        };

        TransitionState[] transitionStates = contentSlot.Itemstack?.Collectible.UpdateAndGetTransitionStates(world, contentSlot);
        bool addNewLine = true;

        if (transitionStates != null) {
            // Find perish transition state
            TransitionState perishState = transitionStates.FirstOrDefault(state =>
                state.Props.Type == EnumTransitionType.Perish &&
                contentSlot.Itemstack.Collectible.GetTransitionRateMul(world, contentSlot, state.Props.Type) > 0);

            if (perishState != null) {
                float perishRate = contentSlot.Itemstack.Collectible.GetTransitionRateMul(world, contentSlot, perishState.Props.Type);
                float freshHoursLeft = perishState.FreshHoursLeft / perishRate;

                addNewLine = false;
                dsc.AppendLine(" " + GetTimeRemainingText(world, freshHoursLeft, EnumTransitionType.Perish, perishState.TransitionLevel));
            }
        }

        if (addNewLine) {
            dsc.AppendLine("");
        }

        return dsc.ToString();
    }

    public static void ByBlockMerged(ItemSlot[] slots, StringBuilder sb, IWorldAccessor world) {
        if (slots == null || slots.Length == 0) return;

        ItemStack firstStack = slots[0].Itemstack?.Clone();
        if (firstStack == null) return;

        int totalStackSize = firstStack.StackSize;
        CollectibleObject collectible = firstStack.Collectible;
        float ripenRate = collectible.GetTransitionRateMul(world, slots[0], EnumTransitionType.Ripen); // Get ripen rate for first slot

        for (int i = 1; i < slots.Length; i++) {
            ItemStack stack = slots[i].Itemstack;
            if (stack == null) break; // Subsequent slots can't have items if the current one is empty.
            totalStackSize += stack.StackSize;
        }

        firstStack.StackSize = totalStackSize;

        sb.Append(firstStack.GetName());
        if (totalStackSize > 1) sb.Append(" x" + totalStackSize);

        if (collectible.TransitionableProps != null && collectible.TransitionableProps.Length > 0) {
            sb.Append(PerishableInfoCompact(world, slots[0], ripenRate, false, false));
        }
    }

    public static string PerishableInfoAverageAndSoonest(ItemSlot[] contentSlots, IWorldAccessor world, float perishMul = 1) {
        StringBuilder dsc = new();

        if (contentSlots == null || contentSlots.Length == 0) {
            dsc.Append(Lang.Get("foodshelves:Empty."));
            return dsc.ToString();
        }

        int itemCount = 0, rotCount = 0, totalCount = 0;
        double totalFreshHours = 0;
        ItemStack soonestPerishStack = null;
        double soonestPerishHours = double.MaxValue;
        float soonestTransitionLevel = 0;

        foreach (var slot in contentSlots) {
            if (slot.Empty) continue;

            var stack = slot.Itemstack;
            if (stack.Collectible.Code.Path.StartsWith("rot")) {
                rotCount += stack.StackSize;
            }
            else {
                itemCount += stack.StackSize;
            }

            TransitionState[] transitionStates = stack?.Collectible.UpdateAndGetTransitionStates(world, slot);
            if (transitionStates != null) {
                foreach (var state in transitionStates) {
                    double basePerishRateMul = stack.Collectible.GetTransitionRateMul(world, slot, state.Props.Type);
                    double effectivePerishRateMul = basePerishRateMul * perishMul;
                    double freshHoursLeft = state.FreshHoursLeft / effectivePerishRateMul;

                    if (state.Props.Type == EnumTransitionType.Perish) {
                        totalFreshHours += freshHoursLeft * stack.StackSize;
                        totalCount += stack.StackSize;

                        if (freshHoursLeft < soonestPerishHours) {
                            soonestPerishHours = freshHoursLeft;
                            soonestPerishStack = stack;
                            soonestTransitionLevel = state.TransitionLevel;
                        }
                    }
                }
            }
        }

        if (itemCount > 0) dsc.AppendLine(Lang.Get("foodshelves:Items inside {0}", itemCount));
        if (rotCount > 0) dsc.AppendLine(Lang.Get("Rotten Food: {0}", rotCount));

        // Average perish rate
        if (totalCount > 0) {
            double averageFreshHoursLeft = totalFreshHours / totalCount;
            string averageFreshnessText = GetTimeRemainingText(world, averageFreshHoursLeft, EnumTransitionType.Perish, 0, "foodshelves:Average freshness");
            dsc.AppendLine(averageFreshnessText);
        }

        // Item soonest to perish
        if (soonestPerishStack != null) {
            dsc.Append(Lang.Get("foodshelves:Soonest") + " " + soonestPerishStack.GetName());

            if (soonestTransitionLevel > 0) {
                dsc.AppendLine(", " + Lang.Get("{0}% spoiled", (int)Math.Round(soonestTransitionLevel * 100)));
            }
            else {
                dsc.AppendLine(", " + GetTimeRemainingText(world, soonestPerishHours, EnumTransitionType.Perish));
            }
        }
        else {
            dsc.AppendLine(Lang.Get("foodshelves:No item will perish soon."));
        }

        return dsc.ToString();
    }

    public static string GetTimeRemainingText(IWorldAccessor world, double hoursLeft, EnumTransitionType transitionType, float transitionLevel = 0, string actionVerb = "") {

        if (transitionLevel > 0) {
            switch (transitionType) {
                case EnumTransitionType.Perish:
                    return Lang.Get("{0}% spoiled", (int)Math.Round(transitionLevel * 100));
                case EnumTransitionType.Dry:
                    return Lang.Get("itemstack-dryable-dried", (int)Math.Round(transitionLevel * 100));
                case EnumTransitionType.Cure:
                    return Lang.Get("itemstack-curable-curing", (int)Math.Round(transitionLevel * 100));
                case EnumTransitionType.Ripen:
                    return Lang.Get("itemstack-ripenable-ripening", (int)Math.Round(transitionLevel * 100));
                case EnumTransitionType.Melt:
                    return Lang.Get("itemstack-meltable-melted", (int)Math.Round(transitionLevel * 100));
            }
        }

        string prefix = "";

        switch (transitionType) {
            case EnumTransitionType.Cure:
                prefix = "<font color=\"#bd5424\">" + Lang.Get("Curing") + "</font>: "; break;
            case EnumTransitionType.Dry:
                prefix = "<font color=\"#d6ba7a\">" + Lang.Get("Drying") + "</font>: "; break;
        }

        if (actionVerb == "") {
            switch (transitionType) {
                case EnumTransitionType.Perish: actionVerb = "fresh for"; break;
                case EnumTransitionType.Ripen: actionVerb = "will ripen in"; break;
                case EnumTransitionType.Cure: actionVerb = "foodshelves:Will cure in"; break;
                case EnumTransitionType.Dry: actionVerb = "foodshelves:Will dry in"; break;
                case EnumTransitionType.Melt: actionVerb = "foodshelves:Will melt in"; break;
            }
        }

        if (actionVerb == "") return "";

        double hoursPerDay = world.Calendar.HoursPerDay;
        double daysLeft = hoursLeft / hoursPerDay;

        if (daysLeft >= world.Calendar.DaysPerYear) {
            return prefix + Lang.Get($"{actionVerb} {{0}} years", Math.Round(daysLeft / world.Calendar.DaysPerYear, 1));
        }
        else if (hoursLeft > hoursPerDay) {
            return prefix + Lang.Get($"{actionVerb} {{0}} days", Math.Round(daysLeft, 1));
        }
        else {
            return prefix + Lang.Get($"{actionVerb} {{0}} hours", Math.Round(hoursLeft, 1));
        }
    }
}
