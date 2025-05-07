namespace FoodShelves;

public static class InfoDisplay {
    public enum InfoDisplayOptions {
        ByBlock,
        ByShelf,
        BySegment,
        ByBlockAverageAndSoonest,
        ByBlockMerged
    }

    public static void DisplayPerishMultiplier(float perishMul, StringBuilder dsc, InWorldContainer container = null) {
        container?.ReloadRoom();
        dsc.AppendLine(Lang.Get("Stored food perish speed: {0}x", Math.Round(perishMul, 2)));
    }

    public static void DisplayInfo(IPlayer forPlayer, StringBuilder sb, InventoryGeneric inv, InfoDisplayOptions displaySelection, int slotCount, int segmentsPerShelf = 0, int itemsPerSegment = 0, bool skipLine = true, int skipSlotsFrom = -1) {
        if (skipLine) sb.AppendLine(); // Space in between to be in line with vanilla

        IWorldAccessor world = inv.Api.World;

        List<ItemSlot> itemSlotList = new();
        foreach (var slot in inv) {
            itemSlotList.Add(slot);
        }
        
        if (displaySelection == InfoDisplayOptions.ByBlockAverageAndSoonest) {
            PerishableInfoAverageAndSoonest(itemSlotList.ToArray(), sb, world);
            return;
        }

        if (displaySelection == InfoDisplayOptions.ByBlockMerged) {
            ByBlockMerged(itemSlotList.ToArray(), sb, world);
            return;
        }

        int selectedSegment = -1;
        if (forPlayer.CurrentBlockSelection != null)
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
            if (inv[i].Empty) continue;
            //if (skipSlots?.Contains(i) == true) continue;
            if (skipSlotsFrom != -1 && i >= skipSlotsFrom) break;

            ItemStack stack = inv[i].Itemstack;
            float ripenRate = stack.Collectible.GetTransitionRateMul(world, inv[i], EnumTransitionType.Ripen); // Get ripen rate

            if (stack.Collectible.TransitionableProps != null && stack.Collectible.TransitionableProps.Length > 0) {
                sb.Append(PerishableInfoCompact(world, inv[i], ripenRate));
            }
            else if (stack.Collectible is BlockCrock) {
                sb.Append(CrockInfoCompact(inv, world, inv[i]));
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
            double daysLeft = hoursLeft / world.Calendar.HoursPerDay;

            if (daysLeft >= world.Calendar.DaysPerYear) {
                return Lang.Get("foodshelves:Will melt in {0} years", Math.Round(daysLeft / world.Calendar.DaysPerYear, 1));
            }
            else if (daysLeft > 1) {
                return Lang.Get("foodshelves:Will melt in {0} days", Math.Round(daysLeft, 1));
            }
            else {
                return Lang.Get("foodshelves:Will melt in {0} hours", Math.Round(hoursLeft, 1));
            }
        }

        return Lang.Get("foodshelves:Will not melt");
    }

    public static string GetNameAndStackSize(ItemStack stack) {
        return stack.GetName() + " x" + stack.StackSize;
    }

    public static string GetAmountOfLiters(ItemStack stack) {
        return stack.GetName() + " (" + (float)stack.StackSize / 100 + " L)";
    }

    public static string PerishableInfoCompact(IWorldAccessor world, ItemSlot contentSlot, float ripenRate, bool withStackName = true) {
        if (contentSlot.Empty) return "";

        StringBuilder dsc = new();
        if (withStackName) dsc.Append(contentSlot.Itemstack.GetName());

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
                            double hoursPerday = world.Calendar.HoursPerDay;

                            if (freshHoursLeft / hoursPerday >= world.Calendar.DaysPerYear) {
                                dsc.Append(", " + Lang.Get("fresh for {0} years", Math.Round(freshHoursLeft / hoursPerday / world.Calendar.DaysPerYear, 1)));
                            }
                            else if (freshHoursLeft > hoursPerday) {
                                dsc.Append(", " + Lang.Get("fresh for {0} days", Math.Round(freshHoursLeft / hoursPerday, 1)));
                            }
                            else {
                                dsc.Append(", " + Lang.Get("fresh for {0} hours", Math.Round(freshHoursLeft, 1)));
                            }
                        }
                        break;
                    case EnumTransitionType.Ripen:
                        if (nowSpoiling) break;

                        if (transitionLevel > 0) {
                            dsc.Append(", " + Lang.Get("{1:0.#} days left to ripen ({0}%)", (int)Math.Round(transitionLevel * 100), (state.TransitionHours - state.TransitionedHours) / world.Calendar.HoursPerDay / ripenRate));
                        }
                        else {
                            double hoursPerday = world.Calendar.HoursPerDay;

                            if (freshHoursLeft / hoursPerday >= world.Calendar.DaysPerYear) {
                                dsc.Append(", " + Lang.Get("will ripen in {0} years", Math.Round(freshHoursLeft / hoursPerday / world.Calendar.DaysPerYear, 1)));
                            }
                            else if (freshHoursLeft > hoursPerday) {
                                dsc.Append(", " + Lang.Get("will ripen in {0} days", Math.Round(freshHoursLeft / hoursPerday, 1)));
                            }
                            else {
                                dsc.Append(", " + Lang.Get("will ripen in {0} hours", Math.Round(freshHoursLeft, 1)));
                            }
                        }
                        break;
                }
            }

            dsc.AppendLine();
        }

        return dsc.ToString();
    }

    public static string CuringInfoCompact(IWorldAccessor world, ItemSlot contentSlot, float curingRate = 1.0f) {
        if (contentSlot.Empty) return "";

        TransitionState[] transitionStates = contentSlot.Itemstack?.Collectible.UpdateAndGetTransitionStates(world, contentSlot);
        if (transitionStates == null) return "";

        string text = "<font color=\"#bd5424\">" + Lang.Get("Curing") + "</font>: ";

        foreach (TransitionState state in transitionStates) {
            TransitionableProperties prop = state.Props;

            if (prop.Type != EnumTransitionType.Cure) continue;

            float curingRateMul = contentSlot.Itemstack.Collectible.GetTransitionRateMul(world, contentSlot, prop.Type);
            if (curingRateMul <= 0) continue;

            float hoursLeft = state.FreshHoursLeft / curingRateMul;
            double daysLeft = hoursLeft / world.Calendar.HoursPerDay;

            if (daysLeft >= world.Calendar.DaysPerYear) {
                text += Lang.Get("foodshelves:Will cure in {0} years", Math.Round(daysLeft / world.Calendar.DaysPerYear, 1));
            }
            else if (hoursLeft > world.Calendar.HoursPerDay) {
                text += Lang.Get("foodshelves:Will cure in {0} days", Math.Round(daysLeft, 1));
            }
            else {
                text += Lang.Get("foodshelves:Will cure in {0} hours", Math.Round(hoursLeft, 1));
            }
        }

        return text;
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

            if (recipe != null) {
                if (servings == 1) {
                    dsc.Append(Lang.Get("{0:0.#}x {1}.", servings, recipe.GetOutputName(world, stacks)));
                }
                else {
                    dsc.Append(Lang.Get("{0:0.#}x {1}.", servings, recipe.GetOutputName(world, stacks)));
                }
            }
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
            for (int i = 0; i < transitionStates.Length; i++) {
                TransitionState state = transitionStates[i];

                TransitionableProperties prop = state.Props;
                float perishRate = contentSlot.Itemstack.Collectible.GetTransitionRateMul(world, contentSlot, prop.Type);

                if (perishRate <= 0) continue;

                addNewLine = false;
                float transitionLevel = state.TransitionLevel;
                float freshHoursLeft = state.FreshHoursLeft / perishRate;

                if (prop.Type == EnumTransitionType.Perish) {
                    if (transitionLevel > 0) {
                        dsc.AppendLine(" " + Lang.Get("{0}% spoiled", (int)Math.Round(transitionLevel * 100)));
                    }
                    else {
                        double hoursPerday = world.Calendar.HoursPerDay;

                        if (freshHoursLeft / hoursPerday >= world.Calendar.DaysPerYear) {
                            dsc.AppendLine(" " + Lang.Get("Fresh for {0} years", Math.Round(freshHoursLeft / hoursPerday / world.Calendar.DaysPerYear, 1)));
                        }
                        else if (freshHoursLeft > hoursPerday) {
                            dsc.AppendLine(" " + Lang.Get("Fresh for {0} days", Math.Round(freshHoursLeft / hoursPerday, 1)));
                        }
                        else {
                            dsc.AppendLine(" " + Lang.Get("Fresh for {0} hours", Math.Round(freshHoursLeft, 1)));
                        }
                    }
                }
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
            sb.Append(PerishableInfoCompact(world, slots[0], ripenRate, false));
        }

        sb.AppendLine();
    }

    public static void PerishableInfoAverageAndSoonest(ItemSlot[] contentSlots, StringBuilder dsc, IWorldAccessor world) {
        if (contentSlots == null || contentSlots.Length == 0) {
            dsc.Append(Lang.Get("foodshelves:Empty."));
            return;
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
                    double perishRateMultiplier = stack.Collectible.GetTransitionRateMul(world, slot, state.Props.Type);
                    double freshHoursLeft = state.FreshHoursLeft / perishRateMultiplier;

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
            double hoursPerDay = world.Calendar.HoursPerDay;

            if (averageFreshHoursLeft / hoursPerDay >= world.Calendar.DaysPerYear) {
                dsc.AppendLine(Lang.Get("foodshelves:Average perish rate {0} years", Math.Round(averageFreshHoursLeft / hoursPerDay / world.Calendar.DaysPerYear, 1)));
            }
            else if (averageFreshHoursLeft > hoursPerDay) {
                dsc.AppendLine(Lang.Get("foodshelves:Average perish rate {0} days", Math.Round(averageFreshHoursLeft / hoursPerDay, 1)));
            }
            else {
                dsc.AppendLine(Lang.Get("foodshelves:Average perish rate {0} hours", Math.Round(averageFreshHoursLeft, 1)));
            }
        }

        // Item soonest to perish
        if (soonestPerishStack != null) {
            dsc.Append(Lang.Get("foodshelves:Soonest") + " " + soonestPerishStack.GetName());
            double hoursPerDay = world.Calendar.HoursPerDay;

            if (soonestTransitionLevel > 0) {
                dsc.AppendLine(", " + Lang.Get("{0}% spoiled", (int)Math.Round(soonestTransitionLevel * 100)));
            }
            else {
                if (soonestPerishHours / hoursPerDay >= world.Calendar.DaysPerYear) {
                    dsc.AppendLine(", " + Lang.Get("foodshelves:will perish in {0} years", Math.Round(soonestPerishHours / hoursPerDay / world.Calendar.DaysPerYear, 1)));
                }
                else if (soonestPerishHours > hoursPerDay) {
                    dsc.AppendLine(", " + Lang.Get("foodshelves:will perish in {0} days", Math.Round(soonestPerishHours / hoursPerDay, 1)));
                }
                else {
                    dsc.AppendLine(", " + Lang.Get("foodshelves:will perish in {0} hours", Math.Round(soonestPerishHours, 1)));
                }
            }
        }
        else {
            dsc.AppendLine(Lang.Get("foodshelves:No item will perish soon."));
        }
    }
}
