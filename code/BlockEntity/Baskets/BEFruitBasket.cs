namespace FoodShelves;

public class BEFruitBasket : BEFSContainer {
    protected override string CantPlaceMessage => "foodshelves:Only fruit can be placed in this basket.";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByBlockAverageAndSoonest;

    public override int SlotCount => 22;

    public float MeshAngle { get; set; }
    public bool IsCeilingAttached { get; set; }

    public BEFruitBasket() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck)); }

    public override void Initialize(ICoreAPI api) {
        base.Initialize(api);
        inv.OnAcquireTransitionSpeed += Inventory_OnAcquireTransitionSpeed;
    }

    public override void OnBlockPlaced(ItemStack byItemStack = null) {
        base.OnBlockPlaced(byItemStack);

        Block attachingBlock = Api.World.BlockAccessor.GetBlock(Pos.UpCopy());
        IsCeilingAttached = attachingBlock.CanAttachBlockAt(Api.World.BlockAccessor, Block, Pos, BlockFacing.DOWN);
    }

    protected override bool TryPut(IPlayer byPlayer, ItemSlot slot, BlockSelection blockSel) {
        int moved = 0;

        for (int i = 0; i < SlotCount; i++) {
            if (inv[i].Empty) {
                moved += slot.TryPutInto(Api.World, inv[i]);
                if (!byPlayer.Entity.Controls.CtrlKey) break;
            }
        }

        if (moved > 0) (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
        return moved > 0;
    }

    protected override bool TryTake(IPlayer byPlayer, BlockSelection blockSel) {
        ItemStack stack = null;

        if (byPlayer.Entity.Controls.CtrlKey) { // Take all "same" items
            for (int i = SlotCount - 1; i >= 0; i--) {
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
            for (int i = SlotCount - 1; i >= 0; i--) {
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

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
        bool skipmesh = base.BaseRenderContents(mesher, tesselator);

        if (!skipmesh) {
            var stack = new ItemStack(block);
            if (VariantAttributes.Count != 0) {
                stack.Attributes[BlockFSContainer.FSAttributes] = VariantAttributes;
            }

            MeshData blockMesh = GenBlockVariantMesh(Api, block, stack);

            if (IsCeilingAttached) {
                Shape basketRope = Api.Assets.TryGet(ShapeReferences.utilFruitBasket)?.ToObject<Shape>();
                if (basketRope != null) {
                    tesselator.TesselateShape(block, basketRope, out MeshData ropeMesh);

                    float scale = block.Shape.Scale;
                    ropeMesh.Scale(new Vec3f(0.5f, 0, 0.5f), scale, scale, scale);

                    blockMesh.AddMeshData(ropeMesh);
                }
            }

            if (blockMesh == null) return false;

            mesher.AddMeshData(blockMesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, MeshAngle, 0));
        }

        return true;
    }

    protected override float[][] genTransformationMatrices() {
        float[,] transformationMatrix = BlockFruitBasket.GetTransformationMatrix();
        float[][] tfMatrices = new float[SlotCount][];

        for (int item = 0; item < SlotCount; item++) {
            tfMatrices[item] =
                new Matrixf()
                .Translate(0.5f, 0, 0.5f)
                .RotateYDeg(block.Shape.rotateY + MeshAngle * GameMath.RAD2DEG)
                .RotateXDeg(transformationMatrix[3, item])
                .RotateYDeg(transformationMatrix[4, item])
                .RotateZDeg(transformationMatrix[5, item])
                .Scale(0.5f, 0.5f, 0.5f)
                .Translate(transformationMatrix[0, item] - 0.84375f, transformationMatrix[1, item] + 0.1f, transformationMatrix[2, item] - 0.8125f)
                .Values;
        }

        return tfMatrices;
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
}
