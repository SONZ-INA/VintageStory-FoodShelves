namespace FoodShelves;

public class BEJarStand : BEBaseFSContainer {
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.BySegment;

    protected override float PerishMultiplier => 0.74f;
    protected override float DryingMultiplier => 4.5f; // Vanilla transition calculations are so fucked

    public override int SegmentsPerShelf => 2;

    private enum SlotType {
        LeftSegment = 0,
        RightSegment = 1,
        Stand = 2
    }

    public BEJarStand() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck)); }

    public override bool OnInteract(IPlayer byPlayer, BlockSelection blockSel, string? overrideAttrCheck = null) {
        if (blockSel.SelectionBoxIndex == (int)SlotType.Stand) return false;

        ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
        bool ctrl = byPlayer.Entity.Controls.CtrlKey;

        ItemSlot jarSlotInStand = inv[blockSel.SelectionBoxIndex];

        if (!jarSlotInStand.Empty) {
            if (!hotbarSlot.Empty || ctrl) {
                if (TryUse(byPlayer, hotbarSlot, blockSel)) {
                    return true;
                }

                return false;
            }
        }

        return base.OnInteract(byPlayer, blockSel, overrideAttrCheck);
    }

    protected bool TryUse(IPlayer player, ItemSlot hotbarSlot, BlockSelection blockSel) {
        int segmentIndex = blockSel.SelectionBoxIndex;
        if (segmentIndex >= inv.Count || inv[segmentIndex].Empty) return false;

        ItemSlot jarSlot = inv[segmentIndex];

        if (jarSlot.Itemstack?.Collectible is IContainedInteractable ici) {
            return ici.OnContainedInteractStart(this, jarSlot, player, blockSel);
        }

        return false;
    }

    protected override float[][]? genTransformationMatrices() {
        return TransformationGenerator.GenerateLayout(this, (t) => {
            t.offsetX = -0.2325f;
            t.offsetY = 0.25f;
            t.offsetZ = -0.2325f;
            
            t.x = t.segment * 0.465f;
        });
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb) {
        base.GetBlockInfo(forPlayer, sb);

        int segment = forPlayer.CurrentBlockSelection.SelectionBoxIndex;
        if (segment is (int)SlotType.LeftSegment or (int)SlotType.RightSegment) {
            var contents = GetContents(Api.World, inv[segment].Itemstack);

            if (contents != null && contents.Length > 0) {
                DummySlot dummySlot = new(contents[0], inv);
                sb.AppendLine(TransitionInfoCompact(Api.World, dummySlot, EnumTransitionType.Dry, TransitionDisplayMode.Percentage));
            }
        }
    }
}
