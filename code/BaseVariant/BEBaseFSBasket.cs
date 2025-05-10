namespace FoodShelves;

public abstract class BEBaseFSBasket : BEBaseFSContainer {
    protected new BaseFSBasket block;

    protected abstract string CeilingAttachedUtil { get; }
    public float MeshAngle { get; set; }
    public bool IsCeilingAttached { get; set; }

    public override void Initialize(ICoreAPI api) {
        block ??= api.World.BlockAccessor.GetBlock(Pos) as BaseFSBasket;
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
            InitMesh();

            if (IsCeilingAttached) {
                Shape basketRope = Api.Assets.TryGet(CeilingAttachedUtil)?.ToObject<Shape>();
                if (basketRope != null) {
                    tesselator.TesselateShape(block, basketRope, out MeshData ropeMesh);

                    float scale = block.Shape.Scale;
                    ropeMesh.Scale(new Vec3f(0.5f, 0, 0.5f), scale, scale, scale);

                    blockMesh.AddMeshData(ropeMesh);
                }
            }

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
}
