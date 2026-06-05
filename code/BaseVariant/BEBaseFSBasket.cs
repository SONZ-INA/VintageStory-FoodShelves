namespace FoodShelves;

public abstract class BEBaseFSBasket : BEBaseFSContainer {
    protected new BaseFSBasket block = null!;
    protected MeshData? ropeMesh;

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

        var areasDict = TryGetAttachmentAreas(Block);
        Cuboidi? attachmentArea = GetAreaForFace(areasDict, "up");

        IsCeilingAttached = attachingBlock.CanAttachBlockAt(Api.World.BlockAccessor, Block, Pos.UpCopy(), BlockFacing.DOWN, attachmentArea);
    }

    public override bool OnInteract(IPlayer byPlayer, BlockSelection blockSel, string? overrideAttrCheck = null) {
        ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;

        if (!byPlayer.Entity.Controls.ShiftKey)
            return false;

        if (slot.Empty)
            return TryTake(byPlayer, blockSel);

        if (slot.CanStoreInSlot(overrideAttrCheck ?? AttributeCheck) && TryPut(byPlayer, slot, blockSel))
            return this.HandlePlacementEffects(slot.Itemstack, byPlayer);

        if (CantPlaceMessage != "")
            (Api as ICoreClientAPI)?.TriggerIngameError(this, "cantplace", Lang.Get(CantPlaceMessage));

        return true;
    }

    protected override ItemStack? TryTakeFromSegment(IPlayer byPlayer, int startIndex) {
        ItemStack? stack = null;
        bool takeAllMatching = byPlayer.Entity.Controls.CtrlKey;

        for (int i = ItemsPerSegment - 1; i >= 0; i--) {
            int idx = startIndex + i;
            if (inv[idx].Empty) continue;

            if (stack == null) {
                stack = inv[idx].TakeOut(1);

                if (!takeAllMatching)
                    break;

                continue;
            }

            if (inv[idx].Itemstack?.Collectible?.Code == stack.Collectible?.Code) {
                inv[idx].TakeOut(1);
                stack.StackSize++;
            }
        }

        return stack;
    }

    protected virtual string? GetTransformationPath() {
        return null;
    }

    protected override float[][] genTransformationMatrices() {
        ExplicitTransform transformationMatrix = block.GetTransformationMatrix(GetTransformationPath());
        
        var modifier = block.GetTransformationModifier();
        int blockRotation = Block.GetRotationAngle();

        return TransformationGenerator.GenerateExplicit(transformationMatrix, (t) => {
            t.preRotate = blockRotation + MeshAngle * GameMath.RAD2DEG;
            modifier?.Invoke(t);
        });
    }

    private MeshData? GenerateRopeMesh(ITesselatorAPI tesselator) {
        Shape? basketRope = (Api.Assets.TryGet(CeilingAttachedUtil)?.ToObject<Shape>())
            ?? throw new InvalidOperationException($"No shape util found for {CeilingAttachedUtil}");
        
        tesselator.TesselateShape(block, basketRope, out MeshData ropeMesh);

        float scale = block?.Shape.Scale ?? 0;
        ropeMesh.Scale(new Vec3f(0.5f, 0, 0.5f), scale, scale, scale);

        return ropeMesh;
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
        bool skipmesh = base.BaseRenderContents(mesher, tesselator);

        if (!skipmesh) {
            if (IsCeilingAttached) {
                ropeMesh ??= GenerateRopeMesh(tesselator);
                mesher.AddMeshData(ropeMesh?.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, MeshAngle, 0));
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
