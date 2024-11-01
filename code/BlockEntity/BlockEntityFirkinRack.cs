﻿namespace FoodShelves;

public class BlockEntityFirkinRack : BlockEntityDisplay {
    private readonly InventoryGeneric inv;
    private BlockFirkinRack block;

    public override InventoryBase Inventory => inv;
    public override string InventoryClassName => Block?.Attributes?["inventoryClassName"].AsString();

    private int CapacityLitres { get; set; } = 10;
    static readonly int slotCount = 8;

    public BlockEntityFirkinRack() {
        inv = new InventoryGeneric(slotCount, InventoryClassName + "-0", Api, (id, inv) => {
            if (id / (slotCount / 2) == 0) return new ItemSlotFirkinRack(inv);
            else return new ItemSlotLiquidOnly(inv, CapacityLitres);
        });
    }

    public override void Initialize(ICoreAPI api) {
        base.Initialize(api);
        block = api.World.BlockAccessor.GetBlock(Pos) as BlockFirkinRack;

        if (block?.Attributes?["capacityLitres"].Exists == true) {
            CapacityLitres = block.Attributes["capacityLitres"].AsInt(10);
            for (int i = slotCount / 2; i < slotCount; i++) {
                (inv[i] as ItemSlotLiquidOnly).CapacityLitres = CapacityLitres;
            }
        }
    }

    internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel) {
        ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;

        if (slot.Empty) { // Take firkin
            if (inv[blockSel.SelectionBoxIndex + 4].Empty) {
                return TryTake(byPlayer, blockSel);
            }
            else {
                ItemStack owncontentStack = block.GetContent(blockSel.Position);
                if (owncontentStack?.Collectible?.Code.Path.StartsWith("rot") == true) {
                    return TryTake(byPlayer, blockSel, blockSel.SelectionBoxIndex + 4);
                }

                (Api as ICoreClientAPI)?.TriggerIngameError(this, "canttake", Lang.Get("foodshelves:The firkin must be emptied before it can be picked up."));
                return false;
            }
        }
        else {
            if (inv[blockSel.SelectionBoxIndex].Empty && slot.FirkinRackCheck()) { // Put firkin in rack
                AssetLocation sound = slot.Itemstack?.Block?.Sounds?.Place;

                if (TryPut(slot, blockSel)) {
                    Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                    MarkDirty();
                    return true;
                }
            }
            else if (!inv[blockSel.SelectionBoxIndex].Empty) { // Put/Take liquid
                if (block == null) return false;
                else return block.BaseOnBlockInteractStart(Api.World, byPlayer, blockSel);
            }
        }

        (Api as ICoreClientAPI)?.TriggerIngameError(this, "cantplace", Lang.Get("foodshelves:Only firkins can be placed on this rack."));
        return false;
    }

    private bool TryPut(ItemSlot slot, BlockSelection blockSel) {
        int index = blockSel.SelectionBoxIndex;

        if (inv[index].Empty) {
            int moved = slot.TryPutInto(Api.World, inv[index]);
            MarkDirty(true);
            (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);

            return moved > 0;
        }

        return false;
    }

    private bool TryTake(IPlayer byPlayer, BlockSelection blockSel, int rotTakeout = 0) {
        int index = blockSel.SelectionBoxIndex + rotTakeout;

        for (int i = index; i < slotCount; i++) {
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
                MarkDirty(true);
                return true;
            }
        }

        return false;
    }

    protected override float Inventory_OnAcquireTransitionSpeed(EnumTransitionType transType, ItemStack stack, float baseMul) {
        if (transType == EnumTransitionType.Perish) return base.Inventory_OnAcquireTransitionSpeed(transType, stack, 0.5f);
        else return base.Inventory_OnAcquireTransitionSpeed(transType, stack, 0.8f); // Expanded Foods curing compitability
    }

    protected override float[][] genTransformationMatrices() {
        float[][] tfMatrices = new float[slotCount][];

        for (int i = 0; i < slotCount; i++) {
            float x = i % 2;
            float z = i / 2;

            tfMatrices[i] =
                new Matrixf()
                .Translate(0.5f, 0, 0.5f)
                .RotateYDeg(this.Block.Shape.rotateY + 90)
                .RotateZDeg(90)
                .RotateYDeg(-90)
                .Translate(x * 0.469f - 0.735f, -0.5f, -z * 0.469 - 0.765f)
                .Values;
        }

        return tfMatrices;
    }
}