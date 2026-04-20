using System.Linq;

namespace FoodShelves;

public static class InfoDisplay {
    public enum InfoDisplayOptions {
        ByBlock,
        ByShelf,
        BySegment,
        BySegmentGrouped,
        ByBlockAverageAndSoonest
    }

    public static string GetNameAndStackSize(ItemStack stack) => stack.GetName() + " x" + stack.StackSize;
    public static string GetAmountOfLiters(ItemStack stack) => stack.GetName() + " (" + (float)stack.StackSize / 100 + " L)";

    public static void DisplayPerishMultiplier(float perishMul, StringBuilder dsc, InWorldContainer? container = null) {
        container?.ReloadRoom();
        dsc.AppendLine(Lang.Get("Stored food perish speed: {0}x", Math.Round(perishMul, 2)));
    }

    public static void DisplayInfo(IPlayer forPlayer, StringBuilder sb, InventoryGeneric inv, InfoDisplayOptions displaySelection, int slotCount, int segmentsPerShelf = 0, int itemsPerSegment = 0, bool skipLine = true, int skipSlotsFrom = -1, int selectedSegment = -1) {
        if (skipLine) sb.AppendLine(); // Space in between to be in line with vanilla

        IWorldAccessor world = inv.Api.World;
        List<ItemSlot> itemSlotList = [.. inv];

        switch (displaySelection) {
            case InfoDisplayOptions.ByBlockAverageAndSoonest:
                sb.Append(PerishableInfoAverageAndSoonest([.. itemSlotList], world));
                return;

            case InfoDisplayOptions.BySegmentGrouped:
                int fromSlot = forPlayer.CurrentBlockSelection.SelectionBoxIndex * itemsPerSegment;
                sb.Append(PerishableInfoGrouped(inv, world, fromSlot, fromSlot + itemsPerSegment));
                return;

            case InfoDisplayOptions.ByBlock:
            case InfoDisplayOptions.ByShelf:
            case InfoDisplayOptions.BySegment:
                ProcessStandardDisplay(forPlayer, sb, inv, displaySelection, slotCount, segmentsPerShelf, itemsPerSegment, skipSlotsFrom, selectedSegment);
                return;
        }
    }

    private static (int start, int end) GetIterationBounds(IPlayer forPlayer, InfoDisplayOptions displaySelection, int slotCount, int segmentsPerShelf, int itemsPerSegment, int selectedSegment) {
        if (selectedSegment == -1 && forPlayer.CurrentBlockSelection != null)
            selectedSegment = forPlayer.CurrentBlockSelection.SelectionBoxIndex;

        if (displaySelection != InfoDisplayOptions.ByBlock && selectedSegment == -1)
            return (0, 0);

        return displaySelection switch {
            InfoDisplayOptions.ByBlock => (slotCount - 1, -1),
            InfoDisplayOptions.ByShelf => CalculateShelfBounds(selectedSegment, segmentsPerShelf, itemsPerSegment),
            InfoDisplayOptions.BySegment => (selectedSegment * itemsPerSegment, (selectedSegment * itemsPerSegment) + itemsPerSegment),
            _ => (0, slotCount)
        };
    }

    private static (int start, int end) CalculateShelfBounds(int selectedSegment, int segmentsPerShelf, int itemsPerSegment) {
        int itemsPerShelf = segmentsPerShelf * itemsPerSegment;
        int selectedShelf = selectedSegment / segmentsPerShelf * itemsPerShelf;
        return (selectedShelf, selectedShelf + itemsPerShelf);
    }

    private static void ProcessStandardDisplay(IPlayer forPlayer, StringBuilder sb, InventoryGeneric inv, InfoDisplayOptions displaySelection, int slotCount, int segmentsPerShelf, int itemsPerSegment, int skipSlotsFrom, int selectedSegment) {
        var (start, end) = GetIterationBounds(forPlayer, displaySelection, slotCount, segmentsPerShelf, itemsPerSegment, selectedSegment);

        if (start == end && displaySelection != InfoDisplayOptions.ByBlock)
            return;

        IWorldAccessor world = inv.Api.World;
        int step = displaySelection == InfoDisplayOptions.ByBlock ? -1 : 1;

        for (int i = start; i != end; i += step) {
            if (i >= slotCount) continue;
            if (skipSlotsFrom != -1 && i >= skipSlotsFrom) continue;

            ItemSlot slot = inv[i];
            if (slot.Empty || slot.Itemstack == null) continue;

            if (!ProcessSingleSlot(world, inv, i, slot, slot.Itemstack, end, sb)) {
                break;
            }
        }
    }

    private static bool ProcessSingleSlot(IWorldAccessor world, InventoryGeneric inv, int index, ItemSlot slot, ItemStack stack, int end, StringBuilder sb) {
        if (stack.Collectible.TransitionableProps?.Length > 0) {
            if (stack.IsSmallItem()) {
                sb.Append(PerishableInfoGrouped(inv, world, index, end));
                return false;
            }

            float ripenRate = stack.Collectible.GetTransitionRateMul(world, slot, EnumTransitionType.Ripen);
            sb.Append(PerishableInfoCompact(world, slot, ripenRate));
        }
        else if (stack.Collectible is BlockCrock) {
            sb.Append(CrockInfoCompact(inv, world, slot));
        }
        else if (stack.Collectible is BaseFSBasket) {
            AppendBasketInfo(world, inv, slot, stack, sb);
        }
        else {
            AppendGenericItemInfo(world, slot, stack, sb);
        }

        return true;
    }

    private static void AppendBasketInfo(IWorldAccessor world, InventoryGeneric inv, ItemSlot slot, ItemStack stack, StringBuilder sb) {
        sb.AppendLine(stack.GetName());
        ItemStack[] contents = GetContents(world, stack);

        float containerMul = inv.GetTransitionSpeedMul(EnumTransitionType.Perish, stack);

        float basketMul = (stack.Collectible as BaseFSBasket)?
            .GetContainingTransitionModifierContained(world, slot, EnumTransitionType.Perish) ?? 1f;

        float totalPerishMul = containerMul * basketMul;

        sb.AppendLine("<font color=\"#989898\">" + PerishableInfoAverageAndSoonest(contents.ToDummySlots(), world, totalPerishMul) + "</font>");
    }

    private static void AppendGenericItemInfo(IWorldAccessor world, ItemSlot slot, ItemStack stack, StringBuilder sb) {
        sb.Append(stack.GetName());
        if (stack.StackSize > 1) sb.Append(" x" + stack.StackSize);
        sb.AppendLine();

        ItemStack[] contents = GetContents(world, stack);

        if (contents.Length > 0 && contents[0] != null) {
            AppendStackContentInfo(world, slot, sb);
        }
    }

    public static string GetUntilMelted(ItemSlot? slot) {
        if (slot == null || slot.Empty) return "";

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

    public static string PerishableInfoCompact(IWorldAccessor world, ItemSlot? contentSlot, float ripenRate, bool withStackName = true, bool withStackSize = true) {
        if (contentSlot == null || contentSlot.Empty) return "";

        StringBuilder dsc = new();
        if (withStackName) dsc.Append(contentSlot.Itemstack.GetName());
        if (withStackSize && contentSlot.StackSize > 1) dsc.Append(" x" + contentSlot.StackSize);

        TransitionState[]? transitionStates = contentSlot.Itemstack?.Collectible.UpdateAndGetTransitionStates(world, contentSlot);
        bool nowSpoiling = false;

        if (transitionStates != null) {
            for (int i = 0; i < transitionStates.Length; i++) {
                TransitionState state = transitionStates[i];
                TransitionableProperties prop = state.Props;
                float perishRate = contentSlot.Itemstack!.Collectible.GetTransitionRateMul(world, contentSlot, prop.Type);

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

    public static string PerishableInfoGrouped(InventoryGeneric? inv, IWorldAccessor world, int start, int end) {
        if (inv == null || inv.Empty) return "";

        StringBuilder dsc = new();
        Dictionary<string, List<ItemSlot>> grouped = [];

        // Group items by their name
        for (int i = start; i < end; i++) {
            if (i >= inv.Count || inv[i].Empty) continue;

            ItemStack? stack = inv[i].Itemstack;
            if (stack == null) continue;

            string itemKey = stack.GetName();

            if (!grouped.TryGetValue(itemKey, out List<ItemSlot>? value)) {
                value = [];
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
                totalCount += slot.Itemstack?.StackSize ?? 0;
            }

            dsc.Append(itemName + " x" + totalCount);

            // Calculate and display perish information based on the first item's transition properties
            if (slots.Count > 0 
                && slots[0].Itemstack?.Collectible.TransitionableProps != null 
                && slots[0].Itemstack?.Collectible.TransitionableProps.Length > 0) 
            {

                Dictionary<EnumTransitionType, List<double>> timeLeftByType = [];

                foreach (var slot in slots) {
                    TransitionState[]? states = slot.Itemstack?.Collectible.UpdateAndGetTransitionStates(world, slot);

                    if (states != null) {
                        foreach (TransitionState state in states) {
                            TransitionableProperties prop = state.Props;

                            float perishRate = slot.Itemstack!.Collectible.GetTransitionRateMul(world, slot, prop.Type);
                            if (perishRate <= 0) continue;

                            float transitionLevel = state.TransitionLevel;
                            float freshHoursLeft = state.FreshHoursLeft / perishRate;

                            if (!timeLeftByType.TryGetValue(prop.Type, out List<double>? value)) {
                                value = [];
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

    public static string TransitionInfoCompact(IWorldAccessor world, ItemSlot? contentSlot, EnumTransitionType transitionType) {
        if (contentSlot == null || contentSlot.Empty) return "";

        TransitionState[]? transitionStates = contentSlot.Itemstack?.Collectible.UpdateAndGetTransitionStates(world, contentSlot);
        if (transitionStates == null) return "";

        TransitionState? state = transitionStates.FirstOrDefault(s => s.Props.Type == transitionType);

        if (state != null) {
            float rateMul = contentSlot.Itemstack!.Collectible.GetTransitionRateMul(world, contentSlot, transitionType);
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
        if (inSlot.Itemstack == null) return ""; 

        BlockMeal? mealblock = world.GetBlock(new AssetLocation("bowl-meal")) as BlockMeal;
        BlockCrock crock = (inSlot.Itemstack.Collectible as BlockCrock)!;
        CookingRecipe? recipe = crock.GetCookingRecipe(world, inSlot.Itemstack);
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

        TransitionState[]? transitionStates = contentSlot.Itemstack?.Collectible.UpdateAndGetTransitionStates(world, contentSlot);
        bool addNewLine = true;

        if (transitionStates != null) {
            // Find perish transition state
            TransitionState? perishState = transitionStates.FirstOrDefault(state =>
                state.Props.Type == EnumTransitionType.Perish &&
                contentSlot.Itemstack!.Collectible.GetTransitionRateMul(world, contentSlot, state.Props.Type) > 0);

            if (perishState != null) {
                float perishRate = contentSlot.Itemstack?.Collectible.GetTransitionRateMul(world, contentSlot, perishState.Props.Type) ?? 1;
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

    public static string PerishableInfoAverageAndSoonest(ItemSlot?[] contentSlots, IWorldAccessor world, float perishMul = 1) {
        StringBuilder dsc = new();

        if (contentSlots == null || contentSlots.Length == 0) {
            dsc.Append(Lang.Get("foodshelves:Empty."));
            return dsc.ToString();
        }

        int itemCount = 0, rotCount = 0, totalCount = 0;
        double totalFreshHours = 0;
        ItemStack? soonestPerishStack = null;
        double soonestPerishHours = double.MaxValue;
        float soonestTransitionLevel = 0;

        foreach (var slot in contentSlots) {
            if (slot!.Empty) continue;

            var stack = slot.Itemstack;
            if (stack.Collectible.Code.Path.StartsWith("rot")) {
                rotCount += stack.StackSize;
            }
            else {
                itemCount += stack.StackSize;
            }

            TransitionState?[] transitionStates = stack.Collectible.UpdateAndGetTransitionStates(world, slot);
            if (transitionStates != null) {
                foreach (var state in transitionStates) {
                    double basePerishRateMul = stack.Collectible.GetTransitionRateMul(world, slot, state!.Props.Type);
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

    public static void AppendStackContentInfo(IWorldAccessor world, ItemSlot? slot, StringBuilder sb) {
        if (slot == null || slot.Empty || slot.Itemstack == null) {
            sb.AppendLine(Lang.Get("foodshelves:Empty."));
            return;
        }

        ItemStack stack = slot.Itemstack;
        ItemStack[] contents = GetContents(world, stack);
        ItemStack targetStack = (contents.Length > 0 && contents[0] != null) ? contents[0] : stack;

        sb.Append(" <font color=\"#ababab\">");

        if (contents.Length > 0 && contents[0] != null)
            sb.Append(" (" + (targetStack.StackSize / 100f) + " L)");

        DummyInventory dummyInv = new(world.Api);
        ItemSlot contentSlot = new ItemSlotSurvival(dummyInv) {
            Itemstack = targetStack
        };

        TransitionState[]? transitionStates = contentSlot.Itemstack?.Collectible.UpdateAndGetTransitionStates(world, contentSlot);
        if (transitionStates != null) {
            foreach (var state in transitionStates) {
                var type = state.Props.Type;
                float rateMul = targetStack.Collectible.GetTransitionRateMul(world, contentSlot, type);
                if (rateMul <= 0) continue;

                float hoursLeft = state.FreshHoursLeft / rateMul;

                if (type == EnumTransitionType.Perish || type == EnumTransitionType.Melt)
                    sb.Append(" " + GetTimeRemainingText(world, hoursLeft, type, state.TransitionLevel));
            }
        }

        sb.Append("</font>");
        sb.AppendLine();
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

    public static string? GetNutrientRequirement(IWorldAccessor world, ItemStack? itemStack, bool withTranslation = true) {
        if (itemStack?.Collectible == null) return null;

        string type = itemStack.Collectible.Variant.TryGetValue("type");
        if (string.IsNullOrEmpty(type)) return null;

        Block? cropBlock = world.GetBlock(new AssetLocation(itemStack.Collectible.Code.Domain, "crop-" + type + "-1"));
        if (cropBlock?.CropProps == null) return null;

        // Required nutrient (N, P, K)
        string label = withTranslation 
            ? ("<font color=#a3a3a3>" + Lang.Get("soil-nutrition-requirement") + "</font>") 
            : "";
        
        string information = 
            "<font color=lightgreen>"
            + cropBlock.CropProps.RequiredNutrient + " ("
            + cropBlock.CropProps.ColdDamageBelow + "°C / "
            + cropBlock.CropProps.HeatDamageAbove + "°C)"
            + "</font>";

        return label + information;
    }
}
