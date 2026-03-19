namespace FoodShelves;

public abstract class BEBaseFSBasket : BEBaseFSContainer {
    protected new BaseFSBasket block = null!;

    protected abstract string CeilingAttachedUtil { get; }
    public float MeshAngle { get; set; }
    public bool IsCeilingAttached { get; set; }

    public override void Initialize(ICoreAPI api) {
        block ??= (api.World.BlockAccessor.GetBlock(Pos) as BaseFSBasket)!;
        base.Initialize(api);
    }

    public override void OnBlockPlaced(ItemStack byItemStack) {
        base.OnBlockPlaced(byItemStack);

        Block attachingBlock = Api.World.BlockAccessor.GetBlock(Pos.UpCopy());
        IsCeilingAttached = attachingBlock.CanAttachBlockAt(Api.World.BlockAccessor, Block, Pos, BlockFacing.DOWN);
    }

    public override bool OnInteract(IPlayer byPlayer, BlockSelection blockSel, string? overrideAttrCheck = null) {
        ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;

        bool shift = byPlayer.Entity.Controls.ShiftKey;

        if (!shift && slot.Empty) // Take basket
            return false;

        if (shift) {
            if (!slot.Empty) {
                if (slot.CanStoreInSlot(overrideAttrCheck ?? AttributeCheck) && TryPut(byPlayer, slot, blockSel)) {
                    return this.HandlePlacementEffects(slot.Itemstack, byPlayer);
                }

                if (CantPlaceMessage != "") {
                    (Api as ICoreClientAPI)?.TriggerIngameError(this, "cantplace", Lang.Get(CantPlaceMessage));
                }

                return true;
            }

            return TryTake(byPlayer, blockSel);
        }

        return false;
    }

    protected override ItemStack? TryTakeFromSegment(IPlayer byPlayer, int startIndex) {
        ItemStack? stack = null;

        if (byPlayer.Entity.Controls.CtrlKey) {
            for (int i = ItemsPerSegment - 1; i >= 0; i--) {
                int idx = startIndex + i;
                if (inv[idx].Empty) continue;

                if (stack == null) {
                    stack = inv[idx].TakeOut(1);
                }
                else if (inv[idx].Itemstack?.Collectible?.Code == stack.Collectible?.Code) {
                    inv[idx].TakeOut(1);
                    stack.StackSize += 1;
                }
            }
        }
        else {
            for (int i = ItemsPerSegment - 1; i >= 0; i--) {
                int idx = startIndex + i;
                if (inv[idx].Empty) continue;

                stack = inv[idx].TakeOut(1);
                break;
            }
        }

        return stack;
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
        bool skipmesh = base.BaseRenderContents(mesher, tesselator);

        if (!skipmesh) {
            InitMesh();

            if (IsCeilingAttached) {
                Shape? basketRope = Api.Assets.TryGet(CeilingAttachedUtil)?.ToObject<Shape>();
                if (basketRope != null) {
                    tesselator.TesselateShape(block, basketRope, out MeshData ropeMesh);

                    float scale = block?.Shape.Scale ?? 0;
                    ropeMesh.Scale(new Vec3f(0.5f, 0, 0.5f), scale, scale, scale);

                    blockMesh?.AddMeshData(ropeMesh);
                }
            }

            mesher.AddMeshData(blockMesh?.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, MeshAngle, 0));
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
