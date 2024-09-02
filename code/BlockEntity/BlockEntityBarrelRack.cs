﻿namespace FoodShelves;

public class BlockEntityBarrelRack : BlockEntityContainer {
    readonly InventoryGeneric inv;
    BlockBarrelRack block;

    public override InventoryBase Inventory => inv;
    public override string InventoryClassName => Block?.Attributes?["inventoryClassName"].AsString();

    private int CapacityLitres { get; set; } = 50;
    static readonly int slotCount = 2;

    public BlockEntityBarrelRack() {
        inv = new InventoryGeneric(slotCount, InventoryClassName + "-0", Api, (id, inv) => {
            if (id == 0) return new ItemSlotBarrelRack(inv);
            else return new ItemSlotLiquidOnly(inv, CapacityLitres);
        });
    }

    public override void Initialize(ICoreAPI api) {
        base.Initialize(api);
        block = api.World.BlockAccessor.GetBlock(Pos) as BlockBarrelRack;

        if (block?.Attributes?["capacityLitres"].Exists == true) {
            CapacityLitres = block.Attributes["capacityLitres"].AsInt(50);
            (inv[1] as ItemSlotLiquidOnly).CapacityLitres = CapacityLitres;
        }
    }

    internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel) {
        ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;

        if (slot.Empty) { // Take barrel
            if (inv[1].Empty) {
                return TryTake(byPlayer);
            }
            else {
                ItemStack owncontentStack = block.GetContent(blockSel.Position);
                if (owncontentStack?.Collectible?.Code?.Path?.StartsWith("rot") == true) {
                    return TryTake(byPlayer, 1);
                }

                (Api as ICoreClientAPI)?.TriggerIngameError(this, "canttake", Lang.Get("foodshelves:The barrel must be emptied before it can be picked up."));
                return false;
            }
        }
        else {
            if (inv.Empty && slot.HorizontalBarrelRackCheck()) { // Put barrel in rack
                AssetLocation sound = slot.Itemstack?.Block?.Sounds?.Place;

                if (TryPut(slot, blockSel)) {
                    Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                    MarkDirty();
                    return true;
                }
            }
            else if (!inv.Empty) { // Put/Take liquid
                if (block == null) return false;
                else return block.BaseOnBlockInteractStart(Api.World, byPlayer, blockSel);
            }
        }

        (Api as ICoreClientAPI)?.TriggerIngameError(this, "cantplace", Lang.Get("foodshelves:Only barrels can be placed on this rack."));
        return false;
    }

    private bool TryPut(ItemSlot slot, BlockSelection blockSel) {
        int index = blockSel.SelectionBoxIndex;
        if (index < 0 || index >= slotCount) return false;

        if (inv[index].Empty) {
            int moved = slot.TryPutInto(Api.World, inv[index]);
            MarkDirty(true);
            (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);

            return moved > 0;
        }

        return false;
    }

    private bool TryTake(IPlayer byPlayer, int index = 0) {
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
        return base.Inventory_OnAcquireTransitionSpeed(transType, stack, 0.5f);
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
        bool skipmesh = base.OnTesselation(mesher, tesselator);

        if (!skipmesh) {
            MeshData meshData = GenBlockMeshUnhashed(Api, this, tesselator);
            if (meshData == null) return false;

            ItemStack[] stack = GetContentStacks();
            if (stack[0] != null && stack[0].Block != null) {
                MeshData substituteBarrelShape = SubstituteBlockShape(Api, tesselator, ShapeReferences.HorizontalBarrel, stack[0].Block);
                meshData.AddMeshData(substituteBarrelShape.BlockYRotation(this));
            }

            mesher.AddMeshData(meshData.Clone());
        }

        return true;
    }
}
