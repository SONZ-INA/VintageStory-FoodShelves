namespace FoodShelves;

public class BECeilingRack : BEBaseFSContainer {
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByBlockMerged;

    protected override float PerishMultiplier => 0.74f;
    protected override float DryingMultiplier => 4.5f; // Vanilla transition calculations are so fucked

    public override int ItemsPerSegment => 12;
    public override int AdditionalSlots => 1;

    public BECeilingRack() {
        inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (id, inv) => {
            if (id == 0) return new ItemSlotFSUniversal(inv, "fsLiquidyStuff", 1, true);
            else return new ItemSlotFSUniversal(inv, AttributeCheck);
        });
    }

    public override void Initialize(ICoreAPI api) {
        inv.PerishableFactorByFoodCategory = new Dictionary<EnumFoodCategory, float>() {
            [EnumFoodCategory.Grain] = 0.5f
        };

        base.Initialize(api);

        // To apply the uniform transition rates across all slots
        inv.SlotModified += OnSlotModified;
    }

    public override bool OnInteract(IPlayer byPlayer, BlockSelection blockSel) {
        ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;

        if (slot.Empty) { // Take barrel
            if (inv[12].Empty) {
                return TryTake(byPlayer, blockSel);
            }
            else {
                //ItemStack? owncontentStack = block.GetContent(blockSel.Position);
                //if (owncontentStack?.Collectible?.Code.Path.StartsWith("rot") == true) {
                //    return TryTake(byPlayer, blockSel);
                //}

                (Api as ICoreClientAPI)?.TriggerIngameError(this, "canttake", Lang.Get("foodshelves:The barrel must be emptied before it can be picked up."));
                return false;
            }
        }
        else {
            if (inv.Empty && slot.CanStoreInSlot(AttributeCheck)) { // Put barrel in rack
                if (TryPut(byPlayer, slot, blockSel)) {
                    return this.HandlePlacementEffects(slot.Itemstack, byPlayer);
                }
            }
            else if (!inv.Empty) { // Put/Take liquid
                return block.BaseOnBlockInteractStart(Api.World, byPlayer, blockSel);
            }
        }

        (Api as ICoreClientAPI)?.TriggerIngameError(this, "cantplace", Lang.Get("foodshelves:Only barrels can be placed on this rack."));
        return false;
    }

    private void OnSlotModified(int slotId) {
        inv.SyncTransitionType(Api, EnumTransitionType.Perish);
        inv.SyncTransitionType(Api, EnumTransitionType.Dry);
        inv.SyncTransitionType(Api, EnumTransitionType.Cure);
        MarkDirty();
    }

    protected override void InitMesh() {
        base.InitMesh();

        if (capi == null) return;

        MeshData? contentMesh = GenLiquidyMesh(capi, inv[0], ShapeReferences.utilCeilingJar, 8.5f);
        if (contentMesh != null) blockMesh?.AddMeshData(contentMesh);
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
        mesher.AddMeshData(blockMesh);
        return true;
    }

    protected override float[][]? genTransformationMatrices() { return null; } // Unneeded

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb) {
        base.GetBlockInfo(forPlayer, sb);
        sb.AppendLine(TransitionInfoCompact(Api.World, inv[0], EnumTransitionType.Dry));
    }
}
