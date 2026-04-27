namespace FoodShelves;

public class BlockBehaviorShiftRightClickPickup(Block block) : BlockBehavior(block) {
    bool dropsPickupMode = false;

    SoundAttributes pickupSound;

    public override void Initialize(JsonObject properties) {
        base.Initialize(properties);

        dropsPickupMode = properties["dropsPickupMode"].AsBool(false);

        JsonObject? jsonSound = properties["sound"];
        if (!jsonSound.Exists) jsonSound = block.Attributes?["placeSound"];

        pickupSound = jsonSound?.AsObject<SoundAttributes?>(null, block.Code.Domain, true)
            ?? new SoundAttributes();
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling) {
        if (!byPlayer.Entity.Controls.ShiftKey) {
            return false;
        }

        ItemStack[] dropStacks = [block.OnPickBlock(world, blockSel.Position)];
        ItemSlot activeSlot = byPlayer.InventoryManager.ActiveHotbarSlot;

        bool heldSlotSuitable = activeSlot.Empty ||
            (dropStacks.Length >= 1 &&
                activeSlot.Itemstack.Equals(world, dropStacks[0], GlobalConstants.IgnoredStackAttributes));

        if (dropsPickupMode) {
            float dropMul = 1f;

            if (block.Attributes?.IsTrue("forageStatAffected") == true) {
                dropMul *= byPlayer.Entity.Stats.GetBlended("forageDropRate");
            }

            dropStacks = block.GetDrops(world, blockSel.Position, byPlayer, dropMul);
            var alldrops = block.GetDropsForHandbook(new ItemStack(block), byPlayer);

            if (!heldSlotSuitable) {
                foreach (var drop in alldrops) {
                    heldSlotSuitable |= activeSlot.Itemstack!.Equals(
                        world,
                        drop.ResolvedItemstack,
                        GlobalConstants.IgnoredStackAttributes
                    );
                }
            }
        }

        if (!heldSlotSuitable ||
            !world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak)) {
            return false;
        }

        if (world.Side == EnumAppSide.Server &&
            BlockBehaviorReinforcable.AllowRightClickPickup(world, blockSel.Position, byPlayer)) {
            bool blockToBreak = true;

            foreach (var stack in dropStacks) {
                var origStack = stack.Clone();

                if (!byPlayer.InventoryManager.TryGiveItemstack(stack, true)) {
                    world.SpawnItemEntity(
                        stack,
                        blockSel.Position.ToVec3d().AddCopy(0.5, 0.1, 0.5)
                    );
                }

                TreeAttribute tree = new();
                tree["itemstack"] = new ItemstackAttribute(origStack.Clone());
                tree["byentityid"] = new LongAttribute(byPlayer.Entity.EntityId);
                world.Api.Event.PushEvent("onitemcollected", tree);

                if (blockToBreak) {
                    blockToBreak = false;
                    world.BlockAccessor.SetBlock(0, blockSel.Position);
                    world.BlockAccessor.TriggerNeighbourBlockUpdate(blockSel.Position);
                }

                world.PlaySoundAt(
                    pickupSound.Location != null
                        ? pickupSound
                        : block.GetSounds(world.BlockAccessor, blockSel).Place,
                    byPlayer,
                    null
                );
            }
        }

        handling = EnumHandling.PreventDefault;
        return true;
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos, ref EnumHandling handling) {
        return base.OnPickBlock(world, pos, ref handling);
    }

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer, ref EnumHandling handled) {
        return [
            new WorldInteraction() {
                ActionLangCode = "game:blockhelp-behavior-rightclickpickup",
                MouseButton = EnumMouseButton.Right,
                HotKeyCode = "shift",
                RequireFreeHand = true
            }
        ];
    }

    public override int GetPlacedBlockInteractionHelpCount(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer, ref EnumHandling handling) {
        return 1;
    }
}