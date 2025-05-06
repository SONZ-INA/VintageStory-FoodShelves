namespace FoodShelves;

public class BlockEntityVegetableBasket : BlockEntityDisplay {
    private readonly InventoryGeneric inv;
    private BlockVegetableBasket block;

    public override InventoryBase Inventory => inv;
    public override string InventoryClassName => Block?.Attributes?["inventoryClassName"].AsString();
    public override string AttributeTransformCode => Block?.Attributes?["attributeTransformCode"].AsString();

    public float MeshAngle { get; set; }
    public bool IsCeilingAttached { get; set; }

    private const int slotCount = 36;
    private float globalPerishMultiplier = 1f;

    public BlockEntityVegetableBasket() { 
        inv = new InventoryGeneric(slotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, "fsVegetableBasket")); 
    }

    public override void Initialize(ICoreAPI api) {
        block = api.World.BlockAccessor.GetBlock(Pos) as BlockVegetableBasket;
        globalPerishMultiplier = api.World.Config.GetFloat("FoodShelves.GlobalPerishMultiplier", 1f);

        base.Initialize(api);

        inv.OnAcquireTransitionSpeed += Inventory_OnAcquireTransitionSpeed;
    }

    private float GetPerishRate() {
        return container.GetPerishRate() * globalPerishMultiplier;
    }

    private float Inventory_OnAcquireTransitionSpeed(EnumTransitionType transType, ItemStack stack, float baseMul) {
        if (transType == EnumTransitionType.Dry || transType == EnumTransitionType.Melt) return container.Room?.ExitCount == 0 ? 2f : 0.5f;
        if (Api == null) return 0;

        if (transType == EnumTransitionType.Ripen) {
            return GameMath.Clamp((1 - container.GetPerishRate() - 0.5f) * 3, 0, 1);
        }

        return 1 * globalPerishMultiplier;
    }

    public override void OnBlockPlaced(ItemStack byItemStack = null) {
        base.OnBlockPlaced(byItemStack);

        Block attachingBlock = Api.World.BlockAccessor.GetBlock(Pos.UpCopy());
        IsCeilingAttached = attachingBlock.CanAttachBlockAt(Api.World.BlockAccessor, Block, Pos, BlockFacing.DOWN);
    }

    internal bool OnInteract(IPlayer byPlayer) {
        ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;

        if (slot.Empty) {
            return TryTake(byPlayer);
        }
        else {
            if (slot.CanStoreInSlot("fsVegetableBasket")) {
                AssetLocation sound = slot.Itemstack?.Block?.Sounds?.Place;

                if (TryPut(slot, byPlayer)) {
                    Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                    MarkDirty(true);
                    return true;
                }
            }

            (Api as ICoreClientAPI).TriggerIngameError(this, "cantplace", Lang.Get("foodshelves:Only vegetables can be placed in this basket."));
            return false;
        }
    }

    private bool TryPut(ItemSlot slot, IPlayer byPlayer) {
        float[,] transformationMatrix = BlockVegetableBasket.GetTransformationMatrix(inv[0]?.Itemstack?.Collectible?.Code);
        int offset = transformationMatrix.GetLength(1);

        int moved = 0;

        for (int i = 0; i < offset; i++) {
            if (inv[i].Empty && (inv[0].Empty || slot?.Itemstack?.Collectible?.Code == inv[0]?.Itemstack?.Collectible?.Code)) {
                moved += slot.TryPutInto(Api.World, inv[i]);
                if (!byPlayer.Entity.Controls.CtrlKey) break;
            }
        }

        if (moved > 0) (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
        return moved > 0;
    }

    private bool TryTake(IPlayer byPlayer) {
        ItemStack stack = null;

        if (byPlayer.Entity.Controls.CtrlKey) { // Take all "same" items
            for (int i = slotCount - 1; i >= 0; i--) {
                if (inv[i].Empty) continue;

                if (stack == null) {
                    stack = inv[i].TakeOut(1);
                }
                else {
                    if (inv[i].Itemstack?.Item.Code == stack.Item.Code) {
                        inv[i].TakeOut(1); // To remove the item from the basket.
                        stack.StackSize += 1;
                    }
                }
            }
        }
        else {
            for (int i = slotCount - 1; i >= 0; i--) {
                if (inv[i].Empty) continue;
                stack = inv[i].TakeOut(1);
                break;
            }
        }

        if (stack != null) {
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

        return false;
    }

    protected override float[][] genTransformationMatrices() {
        float[,] transformationMatrix = BlockVegetableBasket.GetTransformationMatrix(inv[0]?.Itemstack?.Collectible?.Code);

        float[][] tfMatrices = new float[36][];
        int offset = transformationMatrix.GetLength(1);

        for (int i = 0; i < offset; i++) {
            tfMatrices[i] = 
                new Matrixf()
                .Translate(0.5f, 0, 0.5f)
                .RotateYDeg((block != null ? block.Shape.rotateY : 0) + MeshAngle * GameMath.RAD2DEG)
                .RotateXDeg(transformationMatrix[3, i])
                .RotateYDeg(transformationMatrix[4, i])
                .RotateZDeg(transformationMatrix[5, i])
                .Scale(0.5f, 0.5f, 0.5f)
                .Translate(transformationMatrix[0, i] - 0.84375f, transformationMatrix[1, i], transformationMatrix[2, i] - 0.8125f)
                .Values;
        }

        return tfMatrices;
    }

    #region Rotation/Ceiling Render

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
        bool skipmesh = base.OnTesselation(mesher, tesselator);

        if (!skipmesh) {
            MeshData blockMesh = null;

            if (IsCeilingAttached) {
                AssetLocation shapeLocation = new(ShapeReferences.VegetableBasketHanged);
                Shape shape = Api.Assets.TryGet(shapeLocation)?.ToObject<Shape>();
                if (shape != null) {
                    tesselator.TesselateShape(block, shape, out blockMesh);
                    blockMesh.Scale(new Vec3f(0.5f, 0, 0.5f), 0.8f, 0.8f, 0.8f);
                }
            }
            else {
                tesselator.TesselateBlock(block, out blockMesh);
            }

            if (blockMesh == null) return false;

            mesher.AddMeshData(blockMesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, MeshAngle, 0));
        }

        return true;
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving) {
        base.FromTreeAttributes(tree, worldForResolving);
        MeshAngle = tree.GetFloat("meshAngle", 0f);
        IsCeilingAttached = tree.GetBool("isCeilingAttached", false);
        RedrawAfterReceivingTreeAttributes(worldForResolving);
    }

    public override void ToTreeAttributes(ITreeAttribute tree) {
        base.ToTreeAttributes(tree);
        tree.SetFloat("meshAngle", MeshAngle);
        tree.SetBool("isCeilingAttached", IsCeilingAttached);
    }

    #endregion

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb) {
        DisplayPerishMultiplier(GetPerishRate(), sb);
        DisplayInfo(forPlayer, sb, inv, InfoDisplayOptions.ByBlockAverageAndSoonest, slotCount);
    }
}
