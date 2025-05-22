using System.Linq;

namespace FoodShelves;

public class BECoolingCabinet : BEBaseFSContainer {
    protected new BlockCoolingCabinet block;

    public override string AttributeTransformCode => "onHolderUniversalTransform";
    public override string AttributeCheck => "fsHolderUniversal";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.BySegment;
    protected override bool RipeningSpot => true;

    public bool CabinetOpen { get; set; }
    public bool DrawerOpen { get; set; }

    private readonly string CoolingOnly = "fsCoolingOnly";
    private float perishMultiplierBuffed = 0.3f;
    private float perishMultiplierUnBuffed = 0.75f;
    public readonly int cutIceSlot = 216;

    public BECoolingCabinet() {
        ShelfCount = 3;
        SegmentsPerShelf = 3;
        ItemsPerSegment = 24;
        AdditionalSlots = 1;
        PerishMultiplier = 0.75f;

        inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (id, inv) => {
            if (id != cutIceSlot) return new ItemSlotFSUniversal(inv, AttributeCheck);
            else return new ItemSlotFSUniversal(inv, CoolingOnly, 64);
        });
    }

    public override void Initialize(ICoreAPI api) {
        block = api.World.BlockAccessor.GetBlock(Pos) as BlockCoolingCabinet;

        base.Initialize(api);
        
        perishMultiplierBuffed = api.World.Config.GetFloat("FoodShelves.CooledBuff", perishMultiplierBuffed);
        perishMultiplierUnBuffed = globalBlockBuffs ? 0.75f : 1f;

        if (!DrawerOpen && !inv[cutIceSlot].Empty && inv[cutIceSlot].CanStoreInSlot(CoolingOnly)) PerishMultiplier = perishMultiplierBuffed;
        if (CabinetOpen) PerishMultiplier = 1f;
    }

    protected override float GetPerishRate() {
        return container.GetPerishRate() * globalPerishMultiplier * PerishMultiplier;
    }

    public override float Inventory_OnAcquireTransitionSpeed(EnumTransitionType transType, ItemStack stack, float baseMul) {
        if (!inv[cutIceSlot].Empty && PerishMultiplier < perishMultiplierUnBuffed && !inv[cutIceSlot].CanStoreInSlot(CoolingOnly)) {
            if (CabinetOpen) PerishMultiplier = 1f;
            else PerishMultiplier = perishMultiplierUnBuffed;
            SetWaterHeight(true);
            MarkDirty(true);
        }

        if (transType == EnumTransitionType.Dry) return container.Room?.ExitCount == 0 ? 2f : 0.5f;
        if (transType == EnumTransitionType.Perish) return PerishMultiplier * globalPerishMultiplier;

        if (Api == null) return 0;

        if (transType == EnumTransitionType.Ripen) {
            return GameMath.Clamp((1 - container.GetPerishRate() - 0.5f) * 3, 0, 1);
        }

        if (transType == EnumTransitionType.Melt) {
            // Single cut ice will last for ~12 hours. However a stack of them will also last ~12 hours, so a multiplier depending on them is needed.
            // A stack should last about 32 days which is 8 ice blocks
            return (float)((float)1 / inv[cutIceSlot].Itemstack?.StackSize ?? 1) * 4;
        }

        return PerishMultiplier * globalPerishMultiplier;
    }

    #region Interactions

    public override bool OnInteract(IPlayer byPlayer, BlockSelection blockSel) {
        ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;

        // Open/Close cabinet or drawer
        if (byPlayer.Entity.Controls.ShiftKey) {
            switch (blockSel.SelectionBoxIndex) {
                case 9:
                    if (!DrawerOpen) ToggleCabinetDrawer(true, byPlayer);
                    else ToggleCabinetDrawer(false, byPlayer);
                    break;
                default:
                    if (!CabinetOpen) ToggleCabinetDoor(true, byPlayer);
                    else ToggleCabinetDoor(false, byPlayer);
                    break;
            }

            MarkDirty(true);
            return true;
        }

        // Take/Put items
        if (slot.Empty) {
            if (CabinetOpen && blockSel.SelectionBoxIndex <= 8) {
                return TryTake(byPlayer, blockSel);
            }
            else if (DrawerOpen && blockSel.SelectionBoxIndex == 9) {
                return TryTakeIceOrSlush(byPlayer);
            }

            return false;
        }
        else {
            if (CabinetOpen && slot.CanStoreInSlot(AttributeCheck)) {
                AssetLocation sound = slot.Itemstack?.Block?.Sounds?.Place;

                if (TryPut(byPlayer, slot, blockSel)) {
                    Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                    MarkDirty();
                    return true;
                }
            }

            if (DrawerOpen && slot.CanStoreInSlot(CoolingOnly)) {
                AssetLocation sound = slot.Itemstack?.Block?.Sounds?.Place;

                if (TryPutIce(byPlayer, slot, blockSel)) {
                    Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                    MarkDirty();
                    return true;
                }
            }

            (Api as ICoreClientAPI)?.TriggerIngameError(this, "cantplace", Lang.Get("foodshelves:This item cannot be placed in this container."));
            return false;
        }
    }

    protected override bool TryPut(IPlayer byPlayer, ItemSlot slot, BlockSelection blockSel) {
        int startIndex = blockSel.SelectionBoxIndex;
        if (startIndex > 8) return false; // If it's cabinet or drawer selection box, return

        startIndex *= ItemsPerSegment;
        if (!inv[startIndex].Empty) {
            if (!IsSolitaryMatch(inv[startIndex].Itemstack, slot.Itemstack)) return false;
            if (IsLargeItem(slot.Itemstack) || IsLargeItem(inv[startIndex].Itemstack)) return false;
            if (IsSmallItem(inv[startIndex].Itemstack) != IsSmallItem(slot.Itemstack)) return false; 
        }

        for (int i = 0; i < ItemsPerSegment; i++) {
            int currentIndex = startIndex + i;
            if (currentIndex == startIndex + 4 && !IsSmallItem(slot.Itemstack)) return false;

            if (inv[currentIndex].Empty) {
                int moved = slot.TryPutInto(Api.World, inv[currentIndex]);
                MarkDirty();
                (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                return moved > 0;
            }
        }

        return false;
    }

    protected override bool TryTake(IPlayer byPlayer, BlockSelection blockSel) {
        int startIndex = blockSel.SelectionBoxIndex;
        startIndex *= ItemsPerSegment;

        for (int i = ItemsPerSegment - 1; i >= 0; i--) {
            int currentIndex = startIndex + i;
            if (!inv[currentIndex].Empty) {
                ItemStack stack = inv[currentIndex].TakeOut(1);
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

    private bool TryPutIce(IPlayer byPlayer, ItemSlot slot, BlockSelection selection) {
        if (selection.SelectionBoxIndex != 9) return false;
        if (slot.Empty) return false;
        ItemStack stack = inv[cutIceSlot].Itemstack;

        if (inv[cutIceSlot].Empty || (stack.StackSize < stack.Collectible.MaxStackSize && inv[cutIceSlot].CanStoreInSlot(CoolingOnly))) {
            int quantity = byPlayer.Entity.Controls.CtrlKey ? slot.Itemstack.StackSize : 1;
            int moved = slot.TryPutInto(Api.World, inv[cutIceSlot], quantity);

            if (moved == 0 && slot.Itemstack != null) { // Attempt to merge if it fails
                ItemStackMergeOperation op = new(Api.World, EnumMouseButton.Left, 0, EnumMergePriority.ConfirmedMerge, quantity) {
                    SourceSlot = new DummySlot(slot.Itemstack),
                    SinkSlot = new DummySlot(stack)
                };
                stack.Collectible.TryMergeStacks(op);
            }

            if (inv[cutIceSlot].Itemstack?.StackSize < 20) SetIceHeight(1);
            else if (inv[cutIceSlot].Itemstack?.StackSize < 40) SetIceHeight(2);
            else if (inv[cutIceSlot].Itemstack?.StackSize >= 40) SetIceHeight(3);

            MarkDirty(true);
            (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);

            return moved > 0;
        }

        return false;
    }

    private bool TryTakeIceOrSlush(IPlayer byPlayer) {
        if (!inv[cutIceSlot].Empty) {
            ItemStack stack = inv[cutIceSlot].TakeOutWhole();
            if (byPlayer.InventoryManager.TryGiveItemstack(stack)) {
                AssetLocation sound = stack.Block?.Sounds?.Place;
                Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
            }

            if (stack.StackSize > 0) {
                Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            }

            (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
            SetIceHeight(0);
            SetWaterHeight(false);

            MarkDirty(true);
            return true;
        }

        return false;
    }

    #endregion

    #region Animation & Meshing

    private MeshData ownMesh;

    BlockEntityAnimationUtil animUtil {
        get { return GetBehavior<BEBehaviorAnimatable>()?.animUtil; }
    }

    #region Animation

    private void HandleAnimations() {
        if (animUtil != null) {
            if (CabinetOpen) ToggleCabinetDoor(true);
            else ToggleCabinetDoor(false);

            if (DrawerOpen) ToggleCabinetDrawer(true);
            else ToggleCabinetDrawer(false);

            if (!inv[cutIceSlot].Empty) {
                if (inv[cutIceSlot].CanStoreInSlot(CoolingOnly)) {
                    if (inv[cutIceSlot].Itemstack?.StackSize < 20) SetIceHeight(1);
                    else if (inv[cutIceSlot].Itemstack?.StackSize < 40) SetIceHeight(2);
                    else if (inv[cutIceSlot].Itemstack?.StackSize >= 40) SetIceHeight(3);
                }
                else {
                    SetWaterHeight(true);
                }
            }
            else {
                SetIceHeight(0);
                SetWaterHeight(false);
            }
        }
    }

    private void ToggleCabinetDoor(bool open, IPlayer byPlayer = null) {
        if (!inv[cutIceSlot].Empty && !inv[cutIceSlot].CanStoreInSlot(CoolingOnly)) {
            SetWaterHeight(true); // Unfortunately inside Inventory_OnAcquireTransitionSpeed this updates only when you look at it. Forcing it here too.
        }


        if (open) {
            if (animUtil.activeAnimationsByAnimCode.ContainsKey("cabinetopen") == false) {
                animUtil.StartAnimation(new AnimationMetaData() {
                    Animation = "cabinetopen",
                    Code = "cabinetopen",
                    AnimationSpeed = 3f,
                    EaseOutSpeed = 1,
                    EaseInSpeed = 2
                });
            }

            if (byPlayer != null) Api.World.PlaySoundAt(block.soundCabinetOpen, byPlayer.Entity, byPlayer, true, 16, 0.3f);
            PerishMultiplier = 1f;
        }
        else {
            if (animUtil.activeAnimationsByAnimCode.ContainsKey("cabinetopen") == true)
                animUtil.StopAnimation("cabinetopen");

            PerishMultiplier = perishMultiplierUnBuffed;
            
            if (!DrawerOpen && !inv[cutIceSlot].Empty && inv[cutIceSlot].CanStoreInSlot(CoolingOnly))
                PerishMultiplier = perishMultiplierBuffed;
            
            if (byPlayer != null) Api.World.PlaySoundAt(block.soundCabinetClose, byPlayer.Entity, byPlayer, true, 16, 0.3f);
        }

        CabinetOpen = open;
    }

    private void ToggleCabinetDrawer(bool open, IPlayer byPlayer = null) {
        if (!inv[cutIceSlot].Empty && !inv[cutIceSlot].CanStoreInSlot(CoolingOnly)) {
            SetWaterHeight(true); // Unfortunately inside Inventory_OnAcquireTransitionSpeed this updates only when you look at it. Forcing it here too.
        }


        if (open) {
            if (animUtil.activeAnimationsByAnimCode.ContainsKey("draweropen") == false) {
                animUtil.StartAnimation(new AnimationMetaData() {
                    Animation = "draweropen",
                    Code = "draweropen",
                    AnimationSpeed = 3f,
                    EaseOutSpeed = 1,
                    EaseInSpeed = 2
                });
            }

            if (byPlayer != null) Api.World.PlaySoundAt(block.soundDrawerOpen, byPlayer.Entity, byPlayer, true, 16);
            if (!CabinetOpen) PerishMultiplier = perishMultiplierUnBuffed;
        }
        else {
            if (animUtil?.activeAnimationsByAnimCode.ContainsKey("draweropen") == true)
                animUtil?.StopAnimation("draweropen");

            if (!CabinetOpen && !inv[cutIceSlot].Empty && inv[cutIceSlot].CanStoreInSlot(CoolingOnly))
                PerishMultiplier = perishMultiplierBuffed;

            if (byPlayer != null) Api.World.PlaySoundAt(block.soundDrawerClose, byPlayer.Entity, byPlayer, true, 16);
        }

        DrawerOpen = open;
    }

    private void SetIceHeight(int heightLevel) {
        string[] iceAnimations = { "iceheight1", "iceheight2", "iceheight3" };

        foreach (string anim in iceAnimations) {
            if (animUtil?.activeAnimationsByAnimCode.ContainsKey(anim) == true) {
                animUtil?.StopAnimation(anim);
            }
        }

        if (heightLevel > 0) {
            SetWaterHeight(false);
        }

        if (heightLevel > 0 && heightLevel <= 3) {
            string animation = "iceheight" + heightLevel;
            float speed = heightLevel == 1 ? 3f : (heightLevel == 2 ? 8f : 6f);

            if (animUtil?.activeAnimationsByAnimCode.ContainsKey(animation) == false) {
                animUtil?.StartAnimation(new AnimationMetaData() {
                    Animation = animation,
                    Code = animation,
                    AnimationSpeed = speed,
                    EaseOutSpeed = 1,
                    EaseInSpeed = 2
                });
            }
        }
    }

    private void SetWaterHeight(bool up) {
        if (up) {
            SetIceHeight(0);

            if (animUtil?.activeAnimationsByAnimCode.ContainsKey("waterheight") == false) {
                animUtil?.StartAnimation(new AnimationMetaData() {
                    Animation = "waterheight",
                    Code = "waterheight",
                    AnimationSpeed = 6f,
                    EaseOutSpeed = 1,
                    EaseInSpeed = 2
                });
            }
        }
        else {
            if (animUtil?.activeAnimationsByAnimCode.ContainsKey("waterheight") == true) {
                animUtil?.StopAnimation("waterheight");
            }
        }
    }

    #endregion

    #region Meshing

    private MeshData GenMesh(ITesselatorAPI tesselator) {
        string[] parts = VariantAttributes.Values.Select(attr => attr.ToString()).ToArray();
        
        string key = "coolingCabinetMeshes" + Block.Code.ToShortString();
        Dictionary<string, MeshData> meshes = ObjectCacheUtil.GetOrCreate(Api, key, () => {
            return new Dictionary<string, MeshData>();
        });

        Shape shape = null;
        if (animUtil != null) {
            string skeydict = "coolingCabinetMeshes";
            Dictionary<string, Shape> shapes = ObjectCacheUtil.GetOrCreate(Api, skeydict, () => {
                return new Dictionary<string, Shape>();
            });

            string sKey = "coolingCabinetShape" + '-' + block.Code.ToShortString() + '-' + string.Join('-', parts);
            if (!shapes.TryGetValue(sKey, out shape)) {
                AssetLocation shapeLocation = new(ShapeReferences.CoolingCabinet);
                shape = Shape.TryGet(capi, shapeLocation);
                shapes[sKey] = shape;
            }
        }

        string meshKey = "coolingCabinetAnim" + '-' + block.Code.ToShortString() + '-' + string.Join('-', parts);
        if (meshes.TryGetValue(meshKey, out MeshData mesh)) {
            if (animUtil != null && animUtil.renderer == null) {
                animUtil.InitializeAnimator(key, mesh, shape, new Vec3f(0, GetRotationAngle(block), 0));
            }

            return mesh;
        }

        if (animUtil != null) {
            if (animUtil.renderer == null) {
                shape.ApplyVariantTextures(this);

                ITexPositionSource texSource = new ShapeTextureSource(capi, shape, "FS-CoolingCabinetAnimation");
                mesh = animUtil.InitializeAnimator(key, shape, texSource, new Vec3f(0, GetRotationAngle(block), 0));
            }

            return meshes[meshKey] = mesh;
        }

        return null;
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
        bool skipmesh = BaseRenderContents(mesher, tesselator);

        if (!skipmesh) {
            if (ownMesh == null) {
                ownMesh = GenMesh(tesselator);
                if (ownMesh == null) return false;
            }

            mesher.AddMeshData(ownMesh.Clone().BlockYRotation(block));
            HandleAnimations();
        }

        return true;
    }

    protected override float[][] genTransformationMatrices() {
        float[][] tfMatrices = new float[SlotCount][];

        for (int shelf = 0; shelf < ShelfCount; shelf++) {
            for (int segment = 0; segment < SegmentsPerShelf; segment++) {
                for (int item = 0; item < ItemsPerSegment; item++) {
                    int index = shelf * (SegmentsPerShelf * ItemsPerSegment) + segment * ItemsPerSegment + item;

                    float x, y = shelf * 0.4921875f, z;
                    float scale = 0.95f;

                    if (IsLargeItem(inv[index].Itemstack)) {
                        x = segment * 0.65f;
                        z = item * 0.65f;
                    }
                    else if (!IsSmallItem(inv[index].Itemstack)) {
                        x = segment * 0.65f + (index % 2 == 0 ? -0.16f : 0.16f);
                        z = (index / 2) % 2 == 0 ? -0.18f : 0.18f;
                    }
                    else {
                        x = segment * 0.763f + (item % 4) * 0.19f - 0.314f;
                        y = y * 1.16f + (item / 8) * 0.10f + 0.103f;
                        z = ((item / 4) % 2) * 0.45f - 0.25f;
                        scale = 0.82f;
                    }
                    
                    if (inv[index].Itemstack?.Collectible.Code == "pemmican:pemmican-pack") {
                        y += item / 2 * 0.13f;
                        z = -0.18f;
                    }

                    tfMatrices[index] =
                        new Matrixf()
                        .Translate(0.5f, 0, 0.5f)
                        .RotateYDeg(block.Shape.rotateY)
                        .Scale(scale, scale, scale)
                        .Translate(x - 0.625f, y + 0.66f, z - 0.5325f)
                        .Values;
                }
            }
        }

        tfMatrices[cutIceSlot] = new Matrixf().Scale(0.01f, 0.01f, 0.01f).Values; // Hide original cut ice shape, can't bother to custom mesh it out

        return tfMatrices;
    }

    #endregion

    #endregion

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving) {
        base.FromTreeAttributes(tree, worldForResolving);
        CabinetOpen = tree.GetBool("cabinetOpen", false);
        DrawerOpen = tree.GetBool("drawerOpen", false);

        HandleAnimations();
        RedrawAfterReceivingTreeAttributes(worldForResolving);
    }

    public override void ToTreeAttributes(ITreeAttribute tree) {
        base.ToTreeAttributes(tree);
        tree.SetBool("cabinetOpen", CabinetOpen);
        tree.SetBool("drawerOpen", DrawerOpen);
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb) {
        base.GetBlockInfo(forPlayer, sb);

        // For ice & water
        if (forPlayer.CurrentBlockSelection.SelectionBoxIndex == 9 && !inv[cutIceSlot].Empty) {
            if (inv[cutIceSlot].CanStoreInSlot(CoolingOnly)) {
                sb.AppendLine(GetNameAndStackSize(inv[cutIceSlot].Itemstack) + " - " + GetUntilMelted(inv[cutIceSlot]));
            }
            else {
                sb.AppendLine(GetNameAndStackSize(inv[cutIceSlot].Itemstack));
            }
        }

        // Cycle segments when cabinet is closed
        if (!CabinetOpen && forPlayer.CurrentBlockSelection.SelectionBoxIndex == 10) {
            int currentSegment = (int)(Api.World.ElapsedMilliseconds / 2000) % 9;
            sb.AppendLine(Lang.Get("foodshelves:Displaying segment") + " " + currentSegment);

            if (inv[currentSegment * ItemsPerSegment].Empty) {
                sb.AppendLine(Lang.Get("foodshelves:Empty."));
            }
            else {
                DisplayInfo(forPlayer, sb, inv, InfoDisplayOptions.BySegment, SlotCount, SegmentsPerShelf, ItemsPerSegment, false, -1, currentSegment);
            }
        }
    }
}
