namespace FoodShelves;

public class BEBarrelRack : BEBaseFSContainer {
    protected new BlockBarrelRack block;

    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByBlock;

    protected override float PerishMultiplier => 0.5f;
    protected override float CuringMultiplier => 0.8f;

    public override int SlotCount => 2;
    private readonly int capacityLitres = 50;

    public BEBarrelRack() {
        inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (id, inv) => {
            if (id == 0) return new ItemSlotFSUniversal(inv, AttributeCheck);
            else return new ItemSlotLiquidOnly(inv, capacityLitres);
        });
    }

    public override void Initialize(ICoreAPI api) {
        block = api.World.BlockAccessor.GetBlock(Pos) as BlockBarrelRack;
        InitMesh();

        base.Initialize(api);

        (inv[1] as ItemSlotLiquidOnly).CapacityLitres = capacityLitres;
    }

    protected override void InitMesh() {
        var stack = new ItemStack(block);
        if (VariantAttributes.Count != 0) {
            stack.Attributes[BaseFSContainer.FSAttributes] = VariantAttributes;
        }

        blockMesh = GenBlockVariantMesh(Api, stack);
    }

    public override bool OnInteract(IPlayer byPlayer, BlockSelection blockSel) {
        ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;

        if (slot.Empty) { // Take barrel
            if (inv[1].Empty) {
                return TryTake(byPlayer);
            }
            else {
                ItemStack owncontentStack = block.GetContent(blockSel.Position);
                if (owncontentStack?.Collectible?.Code.Path.StartsWith("rot") == true) {
                    return TryTake(byPlayer, 1);
                }

                (Api as ICoreClientAPI)?.TriggerIngameError(this, "canttake", Lang.Get("foodshelves:The barrel must be emptied before it can be picked up."));
                return false;
            }
        }
        else {
            if (inv.Empty && slot.CanStoreInSlot(AttributeCheck)) { // Put barrel in rack
                AssetLocation sound = slot.Itemstack?.Block?.Sounds?.Place;

                if (TryPut(slot)) {
                    Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                    MarkDirty();
                    return true;
                }
            }
            else if (!inv.Empty) { // Put/Take liquid
                return block.BaseOnBlockInteractStart(Api.World, byPlayer, blockSel);
            }
        }

        (Api as ICoreClientAPI)?.TriggerIngameError(this, "cantplace", Lang.Get("foodshelves:Only barrels can be placed on this rack."));
        return false;
    }

    private bool TryPut(ItemSlot slot) {
        if (inv[0].Empty) {
            int moved = slot.TryPutInto(Api.World, inv[0]);
            (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);

            return moved > 0;
        }

        return false;
    }

    private bool TryTake(IPlayer byPlayer, int rotTakeout = 0) {
        for (int i = rotTakeout; i < SlotCount; i++) {
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

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
        InitMesh();

        MeshData currentMesh = blockMesh.Clone();

        ItemStack[] stack = GetContentStacks();
        if (stack[0]?.Block != null) {
            MeshData substituteBarrelShape = SubstituteBlockShape(Api, tesselator, ShapeReferences.HorizontalBarrel, stack[0].Block);
            currentMesh.AddMeshData(substituteBarrelShape.BlockYRotation(block));
        }

        mesher.AddMeshData(currentMesh);
        return true;
    }

    protected override float[][] genTransformationMatrices() { return null; } // Unneeded
}
