namespace FoodShelves;

public class BEWallCabinet : BEBaseFSAnimatable {
    protected new BlockWallCabinet block;

    public override string InventoryClassName => "shelf";
    public override string AttributeTransformCode => "onshelfTransform";

    public override string AttributeCheck => "shelvable";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByBlock;

    public override int SlotCount => 4;

    [TreeSerializable(false)] public bool CabinetOpen { get; set; }
    
    private float perishMultiplierUnBuffed = 0.75f;

    public BEWallCabinet() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck)); }

    public override void Initialize(ICoreAPI api) {
        block = api.World.BlockAccessor.GetBlock(Pos) as BlockWallCabinet;
        base.Initialize(api);

        perishMultiplierUnBuffed = globalBlockBuffs ? 0.75f : 1f;
    }

    public override bool OnInteract(IPlayer byPlayer, BlockSelection blockSel) {
        // Open/Close cabinet
        if (byPlayer.Entity.Controls.ShiftKey) {
            if (!CabinetOpen) ToggleCabinetDoor(true, byPlayer);
            else ToggleCabinetDoor(false, byPlayer);
            
            MarkDirty(true);
            return true;
        }

        return base.OnInteract(byPlayer, blockSel);
    }

    #region Animations

    protected override void HandleAnimations() {
        if (animUtil != null) {
            if (CabinetOpen) ToggleCabinetDoor(true);
            else ToggleCabinetDoor(false);
        }
    }

    private void ToggleCabinetDoor(bool open, IPlayer byPlayer = null) {
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

            if (byPlayer != null) Api.World.PlaySoundAt(block.soundCabinetOpen, byPlayer.Entity, byPlayer, true, 16);
            PerishMultiplier = 1f;
        }
        else {
            if (animUtil.activeAnimationsByAnimCode.ContainsKey("cabinetopen") == true)
                animUtil.StopAnimation("cabinetopen");

            PerishMultiplier = perishMultiplierUnBuffed;

            if (byPlayer != null) Api.World.PlaySoundAt(block.soundCabinetClose, byPlayer.Entity, byPlayer, true, 16, 0.3f);
        }

        CabinetOpen = open;
    }

    #endregion

    protected override float[][] genTransformationMatrices() {
        float[][] tfMatrices = new float[SlotCount][];

        for (int index = 0; index < SlotCount; index++) {
            float x = index % 2 == 0 ? 0.275f : 0.725f;
            float y = index >= 2 ? 0.5625f : 0.0625f;
            float z = 0.25f;

            tfMatrices[index] =
                new Matrixf()
                .Translate(0.5f, 0, 0.5f)
                .RotateYDeg(block.Shape.rotateY)
                .Translate(x - 0.5f, y, z - 0.5f)
                .Translate(-0.5f, 0, -0.5f)
                .Values;
        }

        return tfMatrices;
    }
}
