namespace FoodShelves;

public class BEWallCabinet : BEBaseFSAnimatable {
    protected new BlockWallCabinet block = null!;

    public override string InventoryClassName => "shelf";
    public override string AttributeTransformCode => "onshelfTransform";

    public override string AttributeCheck => "shelvable";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByBlock;

    public override int ShelfCount => 2;
    public override int SegmentsPerShelf => 2;

    [TreeSerializable(false)] public bool DoorOpen { get; set; }
    
    private float perishMultiplierUnBuffed = 0.74f;

    private enum SlotType {
        Door = 4,
        Cabinet = 5
    }

    public BEWallCabinet() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck)); }

    public override void Initialize(ICoreAPI api) {
        block = (api.World.BlockAccessor.GetBlock(Pos) as BlockWallCabinet)!;
        base.Initialize(api);

        perishMultiplierUnBuffed = globalBlockBuffs ? 0.75f : 1f;
    }

    public override bool OnInteract(IPlayer byPlayer, BlockSelection blockSel, string? overrideAttrCheck = null) {
        switch ((SlotType)blockSel.SelectionBoxIndex) {
            case SlotType.Cabinet:
                ToggleDoor(true, byPlayer);
                MarkDirty(true);
                return true;
            
            case SlotType.Door:
                ToggleDoor(false, byPlayer);
                MarkDirty(true);
                return true;

            default:
                bool ctrl = byPlayer.Entity.Controls.CtrlKey;
                ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;

                if ((!slot.Empty || ctrl) && TryUse(byPlayer, blockSel))
                    return true;

                return base.OnInteract(byPlayer, blockSel);
        }
    }

    protected bool TryUse(IPlayer player, BlockSelection blockSel) {
        int index = blockSel.SelectionBoxIndex;

        if (inv[index].Itemstack?.Collectible is IContainedInteractable ic) {
            MarkDirty();
            return ic.OnContainedInteractStart(this, inv[index], player, blockSel);
        }

        return false;
    }

    #region Animations

    protected override void HandleAnimations() {
        if (AnimUtil == null)
            return;

        if (DoorOpen) ToggleDoor(true);
        else ToggleDoor(false);
    }

    private void ToggleDoor(bool open, IPlayer? byPlayer = null) {
        if (open) {
            AnimUtil.TryStartAnimation("dooropen", 3f);
            PerishMultiplier = 1f;

            if (byPlayer != null) {
                Api.World.PlaySoundAt(SoundReferences.WallCabinetOpen, byPlayer, byPlayer, true, 16);
            }
        }
        else {
            AnimUtil.TryStopAnimation("dooropen");
            PerishMultiplier = perishMultiplierUnBuffed;

            if (byPlayer != null) {
                Api.World.PlaySoundAt(SoundReferences.WallCabinetClose, byPlayer, byPlayer, true, 16, 0.3f);
            }
        }

        DoorOpen = open;
    }

    #endregion

    protected override float[][] genTransformationMatrices() {
        return TransformationGenerator.Generate(this, td => {
            td.x = td.segment * 0.43f - 0.215f;
            td.y = td.shelf * 0.5f + 0.065f;
            td.z = -0.2f;
        });
    }
}
