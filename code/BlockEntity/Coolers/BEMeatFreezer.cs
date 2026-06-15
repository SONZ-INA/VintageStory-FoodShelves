namespace FoodShelves;

public class BEMeatFreezer : BEBaseFSCooler {
    protected new BlockMeatFreezer block = null!;

    // Base-Specific ----------------------------
    protected override string CantPlaceMessage => "foodshelves:Only raw meat can be placed in this freezer.";
    
    public override int ShelfCount => 4;
    public override int AdditionalSlots => 1;

    // Cooler-Specific --------------------------
    public override int CutIceSlot => 4;

    protected override InfoDisplayOptions DisplayInfoOpen => InfoDisplayOptions.BySegment;
    protected override InfoDisplayOptions DisplayInfoClosed => InfoDisplayOptions.AllSegments;
    protected override int DisplayInfoIceIndex => (int)SlotType.IceDrawer;

    protected override float BuffedPerishMultiplier => 0.65f;
    protected override float UnbuffedPerishMultiplier => 0.65f;

    protected override AssetLocation DoorOpenSound => SoundReferences.MeatFreezerOpen;
    protected override AssetLocation DoorCloseSound => SoundReferences.MeatFreezerClose;
    protected override AssetLocation DrawerOpenSound => SoundReferences.IceDrawerOpen;
    protected override AssetLocation DrawerCloseSound => SoundReferences.IceDrawerClose;
    // ------------------------------------------
    
    private enum SlotType {
        Segment1 = 0,
        Segment2 = 1,
        Segment3 = 2,
        Segment4 = 3,
        FreezerDoor = 4,
        IceDrawer = 5,
        MeatFreezer = 6
    }

    public BEMeatFreezer() {
        PerishMultiplier = 0.65f; // Needs to be change-able so it's set from within the constructor

        inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (id, inv) => {
            if (id != CutIceSlot) return new ItemSlotFSUniversal(inv, AttributeCheck, 1, true);
            else return new ItemSlotFSUniversal(inv, FSCoolingOnly, 1, true);
        });
    }

    public override void Initialize(ICoreAPI api) {
        block = (api.World.BlockAccessor.GetBlock(Pos) as BlockMeatFreezer)!;
        base.Initialize(api);
    }

    public override bool OnInteract(IPlayer byPlayer, BlockSelection blockSel, string? overrideAttrCheck = null) {
        ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
        
        SlotType aimedAt = (SlotType)blockSel.SelectionBoxIndex;
        bool shift = byPlayer.Entity.Controls.ShiftKey;

        switch (aimedAt) {
            case SlotType.Segment1 or SlotType.Segment2 or SlotType.Segment3 or SlotType.Segment4:
                if (!DoorOpen) return false;
                return base.OnInteract(byPlayer, blockSel);

            case SlotType.FreezerDoor:
                ToggleDoor(!DoorOpen, byPlayer);
                MarkDirty(true);
                return true;

            case SlotType.IceDrawer:
                if (shift) {
                    ToggleDrawer(!DrawerOpen, byPlayer);
                    MarkDirty(true);
                    return true;
                }

                if (!slot.Empty) {
                    if (DrawerOpen && slot.CanStoreInSlot(FSCoolingOnly)) {
                        if (TryPutIce(byPlayer, slot, blockSel)) {
                            return this.HandlePlacementEffects(slot.Itemstack, byPlayer);
                        }
                    }
                    (Api as ICoreClientAPI)?.TriggerIngameError(this, "cantplace", Lang.Get("foodshelves:This item cannot be placed in this container."));
                }
                else if (DrawerOpen) {
                    return TryTakeIceOrSlush(byPlayer);
                }
                break;
        }

        return false;
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
        base.OnTesselation(mesher, tesselator);

        BlockDirection rot = (BlockDirection)block.GetRotationAngle();

        for (int i = 0; i < 4; i++) {
            MeshData? contentMesh = GenLiquidyMesh(capi, inv[i], ShapeReferences.utilMeatFreezer, i == 3 ? 9f : 13f)?.BlockYRotation(block);

            if (contentMesh == null)
                continue;
            
            if (i == 3)
                contentMesh.Translate(new(0, 0.25f, 0));

            const float offset = 0.4375f;

            switch (rot) {
                case BlockDirection.North: contentMesh.Translate(i * offset, 0, 0); break;
                case BlockDirection.West: contentMesh.Translate(0, 0, -i * offset); break;
                case BlockDirection.South: contentMesh.Translate(-i * offset, 0, 0); break;
                case BlockDirection.East: contentMesh.Translate(0, 0, i * offset); break;
            }

            mesher.AddMeshData(contentMesh);
        }

        return true;
    }

    protected override float[][]? genTransformationMatrices() => null; // Unneeded
}
