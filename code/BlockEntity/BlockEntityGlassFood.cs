namespace FoodShelves;

public class BlockEntityGlassFood : BlockEntityDisplay {
    private enum SlotNumber {
        BottomSlot = 0,
        TopSlot = 1
    }

    private InventoryGeneric inv;
    private Block block;
    
    public override InventoryBase Inventory => inv;
    public override string InventoryClassName => Block?.Attributes?["inventoryClassName"].AsString();
    public override string AttributeTransformCode => Block?.Attributes?["attributeTransformCode"].AsString();

    private int shelfCount = 2;
    private const int segmentsPerShelf = 1;
    private const int itemsPerSegment = 4;
    private const float perishMultiplier = 0.75f;

    public BlockEntityGlassFood() { inv = new InventoryGeneric(shelfCount * segmentsPerShelf * itemsPerSegment, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFoodUniversal(inv)); }

    public override void Initialize(ICoreAPI api) {
        block = api.World.BlockAccessor.GetBlock(Pos);
        
        base.Initialize(api);

        if (block.Code.SecondCodePart().Contains("top")) {
            shelfCount = 1;

            List<ItemStack> stack = new();
            foreach (var slot in inv) stack.Add(slot.Itemstack);
            stack.ToArray();

            inv = new InventoryGeneric(shelfCount * segmentsPerShelf * itemsPerSegment, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFoodUniversal(inv));

            for (int i = 0; i < shelfCount * segmentsPerShelf * itemsPerSegment; i++) {
                if (i >= stack.Count) break;
                inv[i].Itemstack = stack[i];
            }

            Inventory.LateInitialize(Inventory.InventoryID, api);
        }

        inv.OnAcquireTransitionSpeed += Inventory_OnAcquireTransitionSpeed;
    }

    private float GetPerishRate() {
        return container.GetPerishRate() * perishMultiplier * Core.ConfigServer.GlobalPerishMultiplier; // Slower perish rate
    }

    private float Inventory_OnAcquireTransitionSpeed(EnumTransitionType transType, ItemStack stack, float baseMul) {
        if (transType == EnumTransitionType.Dry || transType == EnumTransitionType.Melt) return container.Room?.ExitCount == 0 ? 2f : 0.5f;
        if (Api == null) return 0;

        if (transType == EnumTransitionType.Ripen) {
            return GameMath.Clamp((1 - container.GetPerishRate() - 0.5f) * 3, 0, 1);
        }

        return perishMultiplier * Core.ConfigServer.GlobalPerishMultiplier;
    }

    internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel) {
        ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;

        if (slot.Empty) {
            return TryTake(byPlayer, blockSel);
        }
        else {
            if (slot.FoodUniversalCheck()) {
                AssetLocation sound = slot.Itemstack?.Block?.Sounds?.Place;

                if (TryPut(slot, blockSel)) {
                    Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                    MarkDirty();
                    return true;
                }
            }

            return false;
        }
    }

    private bool TryPut(ItemSlot slot, BlockSelection blockSel) {
        int index = blockSel.SelectionBoxIndex;
        if (index >= shelfCount) return false;

        // Bottom Slot
        if (index == (int)SlotNumber.BottomSlot) {
            if (inv[index].Empty) {
                int moved = slot.TryPutInto(Api.World, inv[index]);
                MarkDirty();
                (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                return moved > 0;
            }
            else if (!IsLargeItem(inv[0].Itemstack) && !IsLargeItem(slot.Itemstack)) {
                for (int i = 1; i < itemsPerSegment; i++) {
                    if (inv[i].Empty) {
                        int moved = slot.TryPutInto(Api.World, inv[i]);
                        MarkDirty();
                        (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                        return moved > 0;
                    }
                }
            }
        }

        // Top Slot
        if (block?.Code.SecondCodePart().Contains("top") != true && index == (int)SlotNumber.TopSlot) {
            if (inv[itemsPerSegment].Empty) {
                int moved = slot.TryPutInto(Api.World, inv[itemsPerSegment]);
                MarkDirty();
                (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                return moved > 0;
            }
            else if (!IsLargeItem(inv[itemsPerSegment].Itemstack) && !IsLargeItem(slot.Itemstack)) {
                for (int i = itemsPerSegment; i < shelfCount * itemsPerSegment; i++) {
                    if (inv[i].Empty) {
                        int moved = slot.TryPutInto(Api.World, inv[i]);
                        MarkDirty();
                        (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                        return moved > 0;
                    }
                }
            }
        }

        return false;
    }

    private bool TryTake(IPlayer byPlayer, BlockSelection blockSel) {
        int index = blockSel.SelectionBoxIndex;
        if (index >= shelfCount) return false;

        for (int i = index * itemsPerSegment + itemsPerSegment - 1; i >= index * itemsPerSegment; i--) {
            if (!inv[i].Empty) {
                ItemStack stack = inv[i].TakeOut(1);
                if (byPlayer.InventoryManager.TryGiveItemstack(stack)) {
                    AssetLocation sound = stack.Block?.Sounds?.Place;
                    Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                }

                if (stack.StackSize > 0) {
                    Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
                }

                (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                MarkDirty();

                return true;
            }
        }

        return false;
    }

    protected override float[][] genTransformationMatrices() {
        float[][] tfMatrices = new float[shelfCount * segmentsPerShelf * itemsPerSegment][];

        for (int i = 0; i < shelfCount * segmentsPerShelf * itemsPerSegment; i++) {
            if ((i < itemsPerSegment && IsLargeItem(inv[i].Itemstack)) || (i >= itemsPerSegment && IsLargeItem(inv[i].Itemstack))) {
                tfMatrices[i] =
                    new Matrixf()
                    .Translate(0.5f, 0, 0.5f)
                    .RotateYDeg(block.Shape.rotateY)
                    .Translate(-0.5f, i % (itemsPerSegment - 1) * 0.3725f + 0.2525f, -0.5f)
                    .Values;
            }
            else {
                float x = i % (itemsPerSegment / 2) == 0 ? 0.18f : -0.18f; 
                float z = (i / (itemsPerSegment / 2)) % 2 == 0 ? 0.18f : -0.18f;

                tfMatrices[i] =
                    new Matrixf()
                    .Translate(0.5f, 0, 0.5f)
                    .RotateYDeg(block.Shape.rotateY)
                    .Translate(x - 0.5f, i / itemsPerSegment * 0.3725f + 0.2525f, z - 0.5f)
                    .Values;
            }
        }

        return tfMatrices;
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving) {
        base.FromTreeAttributes(tree, worldForResolving);
        RedrawAfterReceivingTreeAttributes(worldForResolving);
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb) {
        DisplayPerishMultiplier(GetPerishRate(), sb);

        float ripenRate = GameMath.Clamp((1 - GetPerishRate() - 0.5f) * 3, 0, 1);
        if (ripenRate > 0) sb.Append(Lang.Get("Suitable spot for food ripening."));

        DisplayInfo(forPlayer, sb, inv, InfoDisplayOptions.BySegment, shelfCount * segmentsPerShelf * itemsPerSegment, segmentsPerShelf, itemsPerSegment);
    }
}
