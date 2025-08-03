namespace FoodShelves;

public class BEFlourSack : BEBaseFSContainer {
    protected override string CantPlaceMessage => "foodshelves:Only flour can be placed in this sack.";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByBlockMerged;

    protected override float PerishMultiplier => 0.6f;
    public override int SlotCount => 4;

    public BEFlourSack() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck, 64)); }

    protected override void InitMesh() {
        if (capi == null) return;

        if (!inv[0].Empty) {
            string flourtype = inv[0].Itemstack.Collectible.Variant["type"];
            VariantAttributes.SetString("seed", flourtype);
            base.InitMesh();
        }
        else {
            blockMesh = GenBlockVariantMesh(capi, this.GetVariantStack(), ["sackicon"]);
        }

        MeshData contentMesh = GenLiquidyMesh(capi, GetContentStacks(), ShapeReferences.utilFlourSack, 13f);
        if (contentMesh != null) blockMesh.AddMeshData(contentMesh);
    }

    protected override bool TryPut(IPlayer byPlayer, ItemSlot slot, BlockSelection blockSel) {
        if (inv[0].Empty || inv[0].Itemstack.Collectible.Equals(slot.Itemstack.Collectible)) {
            int moved = 0;

            if (byPlayer.Entity.Controls.ShiftKey) {
                for (int i = 0; i < inv.Count; i++) {
                    int availableSpace = inv[i].MaxSlotStackSize - inv[i].StackSize;
                    moved += slot.TryPutInto(Api.World, inv[i], availableSpace);

                    if (slot.StackSize == 0) break;
                }
            }
            else {
                for (int i = 0; i < inv.Count; i++) {
                    if (inv[i].StackSize < inv[i].MaxSlotStackSize) {
                        moved = slot.TryPutInto(Api.World, inv[i], 1);
                        if (moved > 0) break;
                    }
                }
            }

            if (moved > 0) {
                InitMesh();
                MarkDirty();
                (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                return true;
            }
        }

        return false;
    }

    protected override bool TryTake(IPlayer byPlayer, BlockSelection blockSel) {
        for (int i = inv.Count - 1; i >= 0; i--) {
            if (!inv[i].Empty) {
                ItemStack stack;

                if (byPlayer.Entity.Controls.ShiftKey) stack = inv[i].TakeOutWhole();
                else stack = inv[i].TakeOut(1);

                if (stack?.StackSize > 0 && byPlayer.InventoryManager.TryGiveItemstack(stack)) {
                    AssetLocation sound = stack.Block?.Sounds?.Place;
                    Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                    (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);

                    InitMesh();
                    MarkDirty();
                    return true;
                }
            }
        }

        return false;
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
        InitMesh(); // Re-meshing the falling block
        mesher.AddMeshData(blockMesh);
        return true;
    }

    protected override float[][] genTransformationMatrices() { return null; } // Unneeded
}
