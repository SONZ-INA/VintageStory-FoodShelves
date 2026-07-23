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
        bool takeAllMatching = byPlayer.Entity.Controls.CtrlKey;

        int bestIdx = -1;
        double lowestFreshHours = double.MaxValue;
        float highestRotLevel = -1f;
        bool activelyRotting = false;

        // Find the slot index with the item closest to perishing
        for (int i = 0; i < ItemsPerSegment; i++) {
            int idx = startIndex + i;
            if (inv[idx].Empty) continue;

            // Default to the first item we find, if not perishable
            if (bestIdx == -1) bestIdx = idx;

            ItemStack stack = inv[idx].Itemstack!;

            // Rotted items
            if (stack.Collectible.Code.Path.StartsWith("rot")) {
                bestIdx = idx;
                break;
            }

            TransitionState[]? states = stack.Collectible.UpdateAndGetTransitionStates(Api.World, inv[idx]);
            if (states != null) {
                foreach (var state in states) {
                    if (state.Props.Type == EnumTransitionType.Perish) {

                        // Items actively spoiling
                        if (state.TransitionLevel > 0) {
                            if (state.TransitionLevel > highestRotLevel) {
                                highestRotLevel = state.TransitionLevel;
                                bestIdx = idx;
                                activelyRotting = true;
                            }
                        }
                        // Still fresh, check timers
                        else if (!activelyRotting) {
                            float rate = stack.Collectible.GetTransitionRateMul(Api.World, inv[idx], EnumTransitionType.Perish);
                            double effectiveFreshness = rate > 0 ? state.FreshHoursLeft / rate : state.FreshHoursLeft;

                            if (effectiveFreshness < lowestFreshHours) {
                                lowestFreshHours = effectiveFreshness;
                                bestIdx = idx;
                            }
                        }
                    }
                }
            }
        }

        if (bestIdx == -1) return null;

        // Take the most perishable item out of its slot
        ItemStack stackToTake = inv[bestIdx].TakeOut(1);

        // If holding CTRL, take out identical items from the rest of the basket
        if (takeAllMatching) {
            for (int i = 0; i < ItemsPerSegment; i++) {
                int idx = startIndex + i;
                if (inv[idx].Empty || idx == bestIdx) continue;

                if (inv[idx].Itemstack!.Collectible.Code == stackToTake.Collectible.Code) {
                    inv[idx].TakeOut(1);
                    stackToTake.StackSize++;
                }
            }
        }

        return stackToTake;
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

    protected MeshData? GenerateRopeMesh(ITesselatorAPI tesselator) {
        Shape? basketRope = (Api.Assets.TryGet(CeilingAttachedUtil)?.ToObject<Shape>())
            ?? throw new InvalidOperationException($"No shape util found for {CeilingAttachedUtil}");
        
        tesselator.TesselateShape(block, basketRope, out MeshData ropeMesh);

        float scale = block?.Shape.Scale ?? 0;
        ropeMesh.Scale(new Vec3f(0.5f, 0, 0.5f), scale, scale, scale);

        return ropeMesh;
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
        if (base.BaseRenderContents(mesher, tesselator))
            return true;

        if (IsCeilingAttached) {
            ropeMesh ??= GenerateRopeMesh(tesselator);
            mesher.AddMeshData(ropeMesh?.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, MeshAngle, 0));
        }

        mesher.AddMeshData(blockMesh?.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, MeshAngle, 0));

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
