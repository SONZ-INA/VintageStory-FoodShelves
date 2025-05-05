using System.Linq;
using Vintagestory.API.Common.Entities;
using static Vintagestory.GameContent.BlockLiquidContainerBase;

namespace FoodShelves;

public class BECoolingCabinet : BEFSContainer {
    protected new BlockCoolingCabinet block;

    public override string AttributeTransformCode => "onHolderUniversalTransform";
    public override string AttributeCheck => "fsHolderUniversal";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.BySegment;

    public bool CabinetOpen { get; set; }
    public bool DrawerOpen { get; set; }

    private readonly string CoolingOnly = "fsCoolingOnly";

    public BECoolingCabinet() {
        ShelfCount = 3;
        SegmentsPerShelf = 3;
        ItemsPerSegment = 4;
        AdditionalSlots = 1;
        PerishMultiplier = 0.75f;

        inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (id, inv) => {
            if (id != 36) return new ItemSlotFSUniversal(inv, AttributeCheck);
            else return new ItemSlotFSUniversal(inv, CoolingOnly, 64);
        });
    }

    public override void Initialize(ICoreAPI api) {
        block = api.World.BlockAccessor.GetBlock(Pos) as BlockCoolingCabinet;

        base.Initialize(api);

        if (!DrawerOpen && !inv[36].Empty && inv[36].CanStoreInSlot(CoolingOnly)) PerishMultiplier = 0.4f;
        if (CabinetOpen) PerishMultiplier = 1f;
        inv.OnAcquireTransitionSpeed += Inventory_OnAcquireTransitionSpeed;
    }

    public override float Inventory_OnAcquireTransitionSpeed(EnumTransitionType transType, ItemStack stack, float baseMul) {
        if (!inv[36].Empty && PerishMultiplier < 0.75f && !inv[36].CanStoreInSlot(CoolingOnly)) {
            if (CabinetOpen) PerishMultiplier = 1f;
            else PerishMultiplier = 0.75f;
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
            // A stack would last about 32 days which is 8 ice blocks
            return (float)((float)1 / inv[36].Itemstack?.StackSize ?? 1) * 4;
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
        if (CabinetOpen && slot.Empty && blockSel.SelectionBoxIndex <= 8) {
            return TryTake(byPlayer, blockSel);
        }
        else if (DrawerOpen && slot.Empty && blockSel.SelectionBoxIndex == 9) {
            return TryTakeIce(byPlayer);
        }
        else if (DrawerOpen && slot.Itemstack?.Collectible is ILiquidSink) {
            return TryTakeWater(byPlayer, slot, blockSel);
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
        if (!inv[startIndex].Empty && (IsLargeItem(slot.Itemstack) || IsLargeItem(inv[startIndex].Itemstack))) return false;

        for (int i = 0; i < ItemsPerSegment; i++) {
            int currentIndex = startIndex + i;
            if (inv[currentIndex].Empty) {
                int moved = slot.TryPutInto(Api.World, inv[currentIndex]);
                MarkDirty();
                (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                return moved > 0;
            }
        }

        return false;
    }

    private bool TryPutIce(IPlayer byPlayer, ItemSlot slot, BlockSelection selection) {
        if (selection.SelectionBoxIndex != 9) return false;
        if (slot.Empty) return false;
        ItemStack stack = inv[36].Itemstack;

        if (inv[36].Empty || (stack.StackSize < stack.Collectible.MaxStackSize && inv[36].CanStoreInSlot(CoolingOnly))) {
            int quantity = byPlayer.Entity.Controls.CtrlKey ? slot.Itemstack.StackSize : 1;
            int moved = slot.TryPutInto(Api.World, inv[36], quantity);

            if (moved == 0 && slot.Itemstack != null) { // Attempt to merge if it fails
                ItemStackMergeOperation op = new(Api.World, EnumMouseButton.Left, 0, EnumMergePriority.ConfirmedMerge, quantity) {
                    SourceSlot = new DummySlot(slot.Itemstack),
                    SinkSlot = new DummySlot(stack)
                };
                stack.Collectible.TryMergeStacks(op);
            }

            if (inv[36].Itemstack?.StackSize < 20) SetIceHeight(1);
            else if (inv[36].Itemstack?.StackSize < 40) SetIceHeight(2);
            else if (inv[36].Itemstack?.StackSize >= 40) SetIceHeight(3);

            MarkDirty(true);
            (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);

            return moved > 0;
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

    private bool TryTakeIce(IPlayer byPlayer) {
        if (!inv[36].Empty) {
            if (!inv[36].CanStoreInSlot(CoolingOnly)) return false;

            ItemStack stack = inv[36].TakeOutWhole();
            if (byPlayer.InventoryManager.TryGiveItemstack(stack)) {
                AssetLocation sound = stack.Block?.Sounds?.Place;
                Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
            }

            if (stack.StackSize > 0) {
                Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            }

            (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
            SetIceHeight(0);

            MarkDirty(true);
            return true;
        }

        return false;
    }

    private bool TryTakeWater(IPlayer byPlayer, ItemSlot hotbarSlot, BlockSelection selection) {
        if (selection.SelectionBoxIndex != 9) return false;
        ILiquidSink objLsi = hotbarSlot.Itemstack.Collectible as ILiquidSink;

        if (!objLsi.AllowHeldLiquidTransfer) return false;
        if (inv[36].Itemstack == null) return false;

        ItemStack ownContentStack = inv[36].Itemstack;
        if (ownContentStack == null) return false;

        ItemStack contentStack = ownContentStack.Clone();
        int num = SplitStackAndPerformAction(byPlayer.Entity, hotbarSlot, (ItemStack stack) => objLsi.TryPutLiquid(stack, ownContentStack, objLsi.CapacityLitres));
        if (num > 0) {
            TryTakeContent(num);
            DoLiquidMovedEffects(byPlayer, contentStack, num, EnumLiquidDirection.Fill);
            if (inv[36].Empty) SetWaterHeight(false);

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

            if (!inv[36].Empty) {
                if (inv[36].CanStoreInSlot(CoolingOnly)) {
                    if (inv[36].Itemstack?.StackSize < 20) SetIceHeight(1);
                    else if (inv[36].Itemstack?.StackSize < 40) SetIceHeight(2);
                    else if (inv[36].Itemstack?.StackSize >= 40) SetIceHeight(3);
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
        if (!inv[36].Empty && !inv[36].CanStoreInSlot(CoolingOnly)) {
            SetWaterHeight(true); // Unfortunately inside Inventory_OnAcquireTransitionSpeed this updates only when you look at it. Forcing it here too.
        }

        if (byPlayer != null) Api.World.PlaySoundAt(block.soundCabinetClose, byPlayer.Entity, byPlayer, true, 16);

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
            PerishMultiplier = 1f;
        }
        else {
            if (animUtil.activeAnimationsByAnimCode.ContainsKey("cabinetopen") == true)
                animUtil.StopAnimation("cabinetopen");

            PerishMultiplier = 0.75f;
            
            if (!DrawerOpen && !inv[36].Empty && inv[36].CanStoreInSlot(CoolingOnly))
                PerishMultiplier = 0.4f;
        }

        CabinetOpen = open;
    }

    private void ToggleCabinetDrawer(bool open, IPlayer byPlayer = null) {
        if (!inv[36].Empty && !inv[36].CanStoreInSlot(CoolingOnly)) {
            SetWaterHeight(true); // Unfortunately inside Inventory_OnAcquireTransitionSpeed this updates only when you look at it. Forcing it here too.
        }

        if (byPlayer != null) Api.World.PlaySoundAt(block.soundDrawerOpen, byPlayer.Entity, byPlayer, true, 16);

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
            if (!CabinetOpen) PerishMultiplier = 0.75f;
        }
        else {
            if (animUtil?.activeAnimationsByAnimCode.ContainsKey("draweropen") == true) {
                animUtil?.StopAnimation("draweropen");
            }
            if (!CabinetOpen && !inv[36].Empty && inv[36].CanStoreInSlot(CoolingOnly)) {
                PerishMultiplier = 0.4f;
            }
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

            string sKey = "coolingCabinetShape" + '-' + Block.Code.ToShortString();
            if (!shapes.TryGetValue(sKey, out shape)) {
                AssetLocation shapeLocation = new(ShapeReferences.CoolingCabinet);
                shape = Shape.TryGet(capi, shapeLocation);
                shapes[sKey] = shape;
            }
        }

        string[] parts = VariantAttributes.Values.Select(attr => attr.ToString()).ToArray();
        string meshKey = "coolingCabinetAnim" + '-' + string.Join('-', parts) + '-' + block.Code.ToShortString();

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

    protected override float[][] genTransformationMatrices() {
        float[][] tfMatrices = new float[SlotCount][];

        for (int shelf = 0; shelf < ShelfCount; shelf++) {
            for (int segment = 0; segment < SegmentsPerShelf; segment++) {
                for (int item = 0; item < ItemsPerSegment; item++) {
                    int index = shelf * (SegmentsPerShelf * ItemsPerSegment) + segment * ItemsPerSegment + item;

                    float x, y = shelf * 0.4921875f, z;

                    if ((index < ItemsPerSegment && IsLargeItem(inv[index].Itemstack)) || (index >= ItemsPerSegment && IsLargeItem(inv[index].Itemstack))) {
                        x = segment * 0.65f;
                        z = item * 0.65f;
                    }
                    else {
                        x = segment * 0.65f + (index % (ItemsPerSegment / 2) == 0 ? -0.16f : 0.16f);
                        z = (index / (ItemsPerSegment / 2)) % 2 == 0 ? -0.18f : 0.18f;
                    }

                    tfMatrices[index] =
                        new Matrixf()
                        .Translate(0.5f, 0, 0.5f)
                        .RotateYDeg(block.Shape.rotateY)
                        .Scale(0.95f, 0.95f, 0.95f)
                        .Translate(x - 0.625f, y + 0.66f, z - 0.5325f)
                        .Values;
                }
            }
        }

        tfMatrices[36] = new Matrixf().Scale(0.01f, 0.01f, 0.01f).Values; // Hide original cut ice shape, can't bother to custom mesh it out

        return tfMatrices;
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
        if (forPlayer.CurrentBlockSelection.SelectionBoxIndex == 9 && !inv[36].Empty) {
            if (inv[36].CanStoreInSlot(CoolingOnly)) {
                sb.AppendLine(GetNameAndStackSize(inv[36].Itemstack) + " - " + GetUntilMelted(inv[36]));
            }
            else {
                sb.AppendLine(GetAmountOfLiters(inv[36].Itemstack));
            }
        }
    }

    #region Liquid Handlers

    public int SplitStackAndPerformAction(Entity byEntity, ItemSlot slot, System.Func<ItemStack, int> action) {
        if (slot.Itemstack == null) return 0;

        if (slot.Itemstack.StackSize == 1) {
            int num = action(slot.Itemstack);
            if (num > 0) {
                if (byEntity is not EntityPlayer obj) return num;

                obj.WalkInventory(delegate (ItemSlot pslot) {
                    if (pslot.Empty || pslot is ItemSlotCreative || pslot.StackSize == pslot.Itemstack.Collectible.MaxStackSize) {
                        return true;
                    }

                    int mergableQuantity = slot.Itemstack.Collectible.GetMergableQuantity(slot.Itemstack, pslot.Itemstack, EnumMergePriority.DirectMerge);
                    if (mergableQuantity == 0) return true;

                    BlockLiquidContainerBase obj3 = slot.Itemstack.Collectible as BlockLiquidContainerBase;
                    BlockLiquidContainerBase blockLiquidContainerBase = pslot.Itemstack.Collectible as BlockLiquidContainerBase;
                    if ((obj3?.GetContent(slot.Itemstack)?.StackSize).GetValueOrDefault() != (blockLiquidContainerBase?.GetContent(pslot.Itemstack)?.StackSize).GetValueOrDefault()) {
                        return true;
                    }

                    slot.Itemstack.StackSize += mergableQuantity;
                    pslot.TakeOut(mergableQuantity);
                    slot.MarkDirty();
                    pslot.MarkDirty();

                    return true;
                });
            }

            return num;
        }

        ItemStack itemStack = slot.Itemstack.Clone();
        itemStack.StackSize = 1;
        int num2 = action(itemStack);
        if (num2 > 0) {
            slot.TakeOut(1);
            if (byEntity is not EntityPlayer obj2 || !obj2.Player.InventoryManager.TryGiveItemstack(itemStack, slotNotifyEffect: true)) {
                Api.World.SpawnItemEntity(itemStack, byEntity.SidedPos.XYZ);
            }

            slot.MarkDirty();
        }

        return num2;
    }

    public void DoLiquidMovedEffects(IPlayer player, ItemStack contentStack, int moved, EnumLiquidDirection dir) {
        if (player != null) {
            WaterTightContainableProps containableProps = GetContainableProps(contentStack);
            float num = moved / containableProps.ItemsPerLitre;
            (player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
            Api.World.PlaySoundAt((dir == EnumLiquidDirection.Fill) ? containableProps.FillSound : containableProps.PourSound, player.Entity, player, randomizePitch: true, 16f, GameMath.Clamp(num / 5f, 0.35f, 1f));
            Api.World.SpawnCubeParticles(player.Entity.Pos.AheadCopy(0.25).XYZ.Add(0.0, player.Entity.SelectionBox.Y2 / 2f, 0.0), contentStack, 0.75f, (int)num * 2, 0.45f);
        }
    }

    public ItemStack TryTakeContent(int quantityItem) {
        ItemStack itemstack = inv[36].Itemstack;
        if (itemstack == null) return null;

        ItemStack itemStack = inv[36].Itemstack.Clone();
        itemStack.StackSize = quantityItem;
        itemstack.StackSize -= quantityItem;

        if (itemstack.StackSize <= 0) {
            inv[36].Itemstack = null;
        }
        else {
            inv[36].Itemstack = itemstack;
        }

        inv[36].MarkDirty();
        MarkDirty(true);
        return itemStack;
    }

    #endregion
}
