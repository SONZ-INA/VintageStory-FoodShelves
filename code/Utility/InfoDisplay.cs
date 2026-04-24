using System.Linq;
using System.Text.RegularExpressions;

namespace FoodShelves;

public static class InfoDisplay {
    public enum InfoDisplayOptions {
        AllSegments,
        ByBlock,
        ByShelf,
        BySegment,
        BySegmentGrouped,
        ByBlockAverageAndSoonest,
        Cycle
    }

    #region // Public Methods -------------------------------------------------------------------------------------------------------------------------------

    public static string GetNameAndStackSize(ItemStack stack) => stack.GetName() + " x" + stack.StackSize;
    public static string GetAmountOfLiters(ItemStack stack) => stack.GetName() + " (" + (float)stack.StackSize / 100 + " L)";

    public static void DisplayPerishMultiplier(float perishMul, StringBuilder dsc, InWorldContainer? container = null) {
        container?.ReloadRoom();
        dsc.AppendLine(Lang.Get("Stored food perish speed: {0}x", Math.Round(perishMul, 2)));
    }

    public static void DisplayInfo(IPlayer forPlayer, StringBuilder sb, InventoryGeneric inv, InfoDisplayOptions displaySelection, int slotCount, int segmentsPerShelf = 0, int itemsPerSegment = 0, bool skipLine = true, int skipSlotsFrom = -1) {
        if (skipLine) sb.AppendLine(); // Space in between to be in line with vanilla

        IWorldAccessor world = inv.Api.World;

        int selectionIndex = forPlayer.CurrentBlockSelection.SelectionBoxIndex;

        switch (displaySelection) {
            case InfoDisplayOptions.AllSegments:
                ProcessAllSegmentsDisplay(forPlayer, sb, inv, slotCount, itemsPerSegment, skipSlotsFrom);
                return;

            case InfoDisplayOptions.ByBlockAverageAndSoonest:
                sb.Append(PerishableInfoAverageAndSoonest([.. inv], world));
                return;

            case InfoDisplayOptions.BySegmentGrouped:
                int fromSlot = selectionIndex * itemsPerSegment;
                sb.Append(PerishableInfoGrouped(inv, fromSlot, fromSlot + itemsPerSegment));
                return;

            case InfoDisplayOptions.Cycle:
                ProcessCycleDisplay(forPlayer, sb, inv, slotCount, itemsPerSegment, skipSlotsFrom);
                return;

            case InfoDisplayOptions.ByBlock:
            case InfoDisplayOptions.ByShelf:
            case InfoDisplayOptions.BySegment:
                ProcessStandardDisplay(forPlayer, sb, inv, displaySelection, slotCount, segmentsPerShelf, itemsPerSegment, skipSlotsFrom, selectionIndex);
                return;
        }
    }

    public static string TransitionInfoCompact(IWorldAccessor world, ItemSlot? contentSlot, EnumTransitionType transitionType, TransitionDisplayMode displayMode) {
        if (contentSlot == null || contentSlot.Empty) return "";

        TransitionState? state = contentSlot.Itemstack?.Collectible.UpdateAndGetTransitionState(world, contentSlot, transitionType);
        if (state == null) return "";

        float rateMul = contentSlot.Itemstack!.Collectible.GetTransitionRateMul(world, contentSlot, transitionType);
        if (rateMul <= 0) return "";

        if (displayMode == TransitionDisplayMode.Percentage && state.TransitionLevel > 0) {
            return GetTransitionPercentageText(transitionType, state.TransitionLevel);
        }

        double hoursLeft = state.TransitionLevel > 0
            ? state.TransitionHours / rateMul * (1 - state.TransitionLevel)
            : state.FreshHoursLeft / rateMul;

        return GetTimeRemainingText(world, hoursLeft, transitionType);
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
            string averageFreshnessText = GetTimeRemainingText(world, averageFreshHoursLeft, EnumTransitionType.Perish, "foodshelves:Average freshness");
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

    public static string IceDrawerInfo(IWorldAccessor world, ItemSlot iceSlot, string coolingAttributeCheck) {
        if (iceSlot == null || iceSlot.Empty) return "";

        if (iceSlot.CanStoreInSlot(coolingAttributeCheck)) {
            return GetNameAndStackSize(iceSlot.Itemstack) + " - " + TransitionInfoCompact(world, iceSlot, EnumTransitionType.Melt, TransitionDisplayMode.TimeLeft);
        }
        else {
            return GetNameAndStackSize(iceSlot.Itemstack);
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

    #endregion

    #region // Private Info Methods -------------------------------------------------------------------------------------------------------------------------

    private static void ProcessStandardDisplay(IPlayer forPlayer, StringBuilder sb, InventoryGeneric inv, InfoDisplayOptions displaySelection, int slotCount, int segmentsPerShelf, int itemsPerSegment, int skipSlotsFrom, int selectedSegment) {
        var (start, end) = GetIterationBounds(forPlayer, displaySelection, slotCount, segmentsPerShelf, itemsPerSegment, selectedSegment);

        int step = displaySelection == InfoDisplayOptions.ByBlock ? -1 : 1;

        for (int i = start; i != end; i += step) {
            if (i >= slotCount) continue;
            if (skipSlotsFrom != -1 && i >= skipSlotsFrom) continue;

            ItemSlot slot = inv[i];
            if (slot.Empty || slot.Itemstack == null) continue;

            if (!ProcessSingleSlot(inv, i, slot, slot.Itemstack, end, sb)) {
                break;
            }
        }
    }

    private static bool ProcessSingleSlot(InventoryGeneric inv, int index, ItemSlot slot, ItemStack stack, int end, StringBuilder sb) {
        IWorldAccessor world = inv.Api.World;

        if (stack.Collectible.TransitionableProps?.Length > 0) {
            if (stack.IsSmallItem()) {
                sb.Append(PerishableInfoGrouped(inv, index, end));
                return false;
            }

            float ripenRate = stack.Collectible.GetTransitionRateMul(world, slot, EnumTransitionType.Ripen);
            sb.Append(PerishableInfoCompact(world, slot, ripenRate));
        }
        else if (stack.Collectible is BaseFSBasket) {
            sb.Append(BasketInfo(inv, slot, stack));
        }
        else {
            sb.Append(GenericItemInfo(slot, stack));
        }

        return true;
    }

    private static void ProcessCycleDisplay(IPlayer forPlayer, StringBuilder sb, InventoryGeneric inv, int slotCount, int itemsPerSegment, int skipSlotsFrom) {
        IWorldAccessor world = inv.Api.World;

        int validSlotCount = skipSlotsFrom != -1 ? skipSlotsFrom : slotCount;
        int totalSegments = validSlotCount / itemsPerSegment;
        if (totalSegments <= 0) return;

        int currentSegment = (int)(world.ElapsedMilliseconds / 2000) % totalSegments;

        var currentBlockSelection = forPlayer.CurrentBlockSelection;
        if (currentBlockSelection == null) return;

        Block? selectionBlock = currentBlockSelection.Block;
        string blockCode = selectionBlock?.Code?.FirstCodePart() ?? "unknown";

        if (blockCode == "multiblock") {
            var be = selectionBlock?.GetBlockEntity<BEBaseFSContainer>(currentBlockSelection);
            blockCode = be?.Block?.Code?.FirstCodePart() ?? "unknown";
        }

        sb.AppendLine(Lang.Get("foodshelves:Displaying segment") + " " + Lang.Get($"foodshelves:segmentnum-{blockCode}-{currentSegment}"));

        int startSlot = currentSegment * itemsPerSegment;
        if (inv[startSlot].Empty) {
            sb.AppendLine(Lang.Get("foodshelves:Empty."));
        }
        else {
            ProcessStandardDisplay(forPlayer, sb, inv, InfoDisplayOptions.BySegment, slotCount, 0, itemsPerSegment, skipSlotsFrom, currentSegment);
        }
    }

    private static void ProcessAllSegmentsDisplay(IPlayer forPlayer, StringBuilder sb, InventoryGeneric inv, int slotCount, int itemsPerSegment, int skipSlotsFrom) {
        int validSlotCount = skipSlotsFrom != -1 ? skipSlotsFrom : slotCount;
        int totalSegments = validSlotCount / itemsPerSegment;

        for (int i = 0; i < totalSegments; i++) {
            int startSlot = i * itemsPerSegment;

            if (inv[startSlot].Empty) {
                sb.AppendLine(Lang.Get("foodshelves:Empty."));
            }
            else {
                ProcessStandardDisplay(forPlayer, sb, inv, InfoDisplayOptions.BySegment, slotCount, 0, itemsPerSegment, skipSlotsFrom, i);
            }
        }
    }

    private static string BasketInfo(InventoryGeneric inv, ItemSlot slot, ItemStack stack) {
        StringBuilder sb = new();
        IWorldAccessor world = inv.Api.World;

        sb.AppendLine(stack.GetName());
        ItemStack[] contents = GetContents(world, stack);

        float containerMul = inv.GetTransitionSpeedMul(EnumTransitionType.Perish, stack);

        float basketMul = (stack.Collectible as BaseFSBasket)?
            .GetContainingTransitionModifierContained(world, slot, EnumTransitionType.Perish) ?? 1f;

        float totalPerishMul = containerMul * basketMul;

        sb.AppendLine("<font color=\"#989898\">" + PerishableInfoAverageAndSoonest(contents.ToDummySlots(), world, totalPerishMul) + "</font>");
        
        return sb.ToString();
    }

    private static string GenericItemInfo(ItemSlot slot, ItemStack stack) {
        StringBuilder sb = new();

        var customNameInterface = stack.Collectible.GetCollectibleInterface<IContainedCustomName>();

        if (customNameInterface != null) {
            string contents = customNameInterface.GetContainedInfo(slot)?.Trim() ?? "";

            if (!string.IsNullOrEmpty(contents)) {
                contents = Regex.Replace(contents, @"(\(.*?\))", "\n  <font color=\"#989898\">$1</font>");
                sb.AppendLine(contents);
            }
        }

        return sb.ToString();
    }

    private static string PerishableInfoCompact(IWorldAccessor world, ItemSlot? contentSlot, float ripenRate, bool withStackName = true, bool withStackSize = true) {
        if (contentSlot == null || contentSlot.Empty) return "";

        StringBuilder dsc = new();
        if (withStackName) dsc.Append(contentSlot.Itemstack.GetName());
        if (withStackSize && contentSlot.StackSize > 1) dsc.Append(" x" + contentSlot.StackSize);

        bool nowSpoiling = false;

        string perishText = TransitionInfoCompact(world, contentSlot, EnumTransitionType.Perish, TransitionDisplayMode.Percentage);
        if (!string.IsNullOrEmpty(perishText)) {
            dsc.Append(", " + perishText);

            if (perishText.Contains('%')) {
                nowSpoiling = true;
            }
        }

        if (!nowSpoiling) {
            TransitionState? ripenState = contentSlot.Itemstack?.Collectible.UpdateAndGetTransitionState(world, contentSlot, EnumTransitionType.Ripen);

            if (ripenState != null && contentSlot.Itemstack!.Collectible.GetTransitionRateMul(world, contentSlot, EnumTransitionType.Ripen) > 0) {
                if (ripenState.TransitionLevel > 0) {
                    dsc.Append(", " + Lang.Get("{1:0.#} days left to ripen ({0}%)", (int)Math.Round(ripenState.TransitionLevel * 100), (ripenState.TransitionHours - ripenState.TransitionedHours) / world.Calendar.HoursPerDay / ripenRate));
                }
                else {
                    dsc.Append(", " + TransitionInfoCompact(world, contentSlot, EnumTransitionType.Ripen, TransitionDisplayMode.TimeLeft));
                }
            }
        }

        dsc.AppendLine();
        return dsc.ToString();
    }

    private static string PerishableInfoGrouped(InventoryGeneric? inv, int start, int end) {
        if (inv == null || inv.Empty) return "";

        IWorldAccessor world = inv.Api.World;

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
                            dsc.Append(", " + GetTimeRemainingText(world, avgHoursLeft, type, "foodshelves:Average freshness"));
                        }
                    }
                }
            }

            dsc.AppendLine();
        }

        return dsc.ToString();
    }

    #endregion

    #region // Private Helpers ------------------------------------------------------------------------------------------------------------------------------

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

    private static string GetTransitionPercentageText(EnumTransitionType transitionType, float transitionLevel) {
        int percent = (int)Math.Round(transitionLevel * 100);

        return transitionType switch {
            EnumTransitionType.Perish => Lang.Get("{0}% spoiled", percent),
            EnumTransitionType.Dry => Lang.Get("itemstack-dryable-dried", percent),
            EnumTransitionType.Cure => Lang.Get("itemstack-curable-curing", percent),
            EnumTransitionType.Ripen => Lang.Get("itemstack-ripenable-ripening", percent),
            EnumTransitionType.Melt => Lang.Get("itemstack-meltable-melted", percent),
            _ => ""
        };
    }

    private static string GetTimeRemainingText(IWorldAccessor world, double hoursLeft, EnumTransitionType transitionType, string? actionVerb = null) {
        string prefix = transitionType switch {
            EnumTransitionType.Cure => "<font color=\"#bd5424\">" + Lang.Get("Curing") + "</font>: ",
            EnumTransitionType.Dry => "<font color=\"#d6ba7a\">" + Lang.Get("Drying") + "</font>: ",
            _ => ""
        };

        if (string.IsNullOrEmpty(actionVerb)) {
            actionVerb = transitionType switch {
                EnumTransitionType.Perish => "fresh for",
                EnumTransitionType.Ripen => "will ripen in",
                EnumTransitionType.Cure => "foodshelves:Will cure in",
                EnumTransitionType.Dry => "foodshelves:Will dry in",
                EnumTransitionType.Melt => "foodshelves:Will melt in",
                _ => ""
            };
        }

        if (string.IsNullOrEmpty(actionVerb)) return "";

        double hoursPerDay = world.Calendar.HoursPerDay;
        double daysLeft = hoursLeft / hoursPerDay;

        if (daysLeft >= world.Calendar.DaysPerYear) {
            return prefix + Lang.Get($"{actionVerb} {{0}} years", Math.Round(daysLeft / world.Calendar.DaysPerYear, 1));
        }

        if (hoursLeft > hoursPerDay) {
            return prefix + Lang.Get($"{actionVerb} {{0}} days", Math.Round(daysLeft, 1));
        }

        return prefix + Lang.Get($"{actionVerb} {{0}} hours", Math.Round(hoursLeft, 1));
    }

    #endregion
}
