namespace FoodShelves;

public class BEJarStand : BEBaseFSContainer {
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.BySegment;

    protected override float PerishMultiplier => 0.74f;
    protected override float DryingMultiplier => 4.5f; // Vanilla transition calculations are so fucked

    public override int SegmentsPerShelf => 2;

    protected enum SlotType {
        LeftSegment = 0,
        RightSegment = 1,
        Stand = 2
    }

    public BEJarStand() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck)); }

    public override bool OnInteract(IPlayer byPlayer, BlockSelection blockSel, string? overrideAttrCheck = null) {
        if (blockSel.SelectionBoxIndex == (int)SlotType.Stand) return false;

        ItemSlot jarSlot = inv[blockSel.SelectionBoxIndex];

        if (jarSlot.Empty) {
            return base.OnInteract(byPlayer, blockSel, overrideAttrCheck);
        }

        ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
        bool shift = byPlayer.Entity.Controls.ShiftKey;

        if (TryUse(byPlayer, blockSel)) {
            return true;
        }

        if (shift && hotbarSlot.Empty) {
            return base.OnInteract(byPlayer, blockSel, overrideAttrCheck);
        }

        return false;
    }

    protected bool TryUse(IPlayer player, BlockSelection blockSel) {
        int segmentIndex = blockSel.SelectionBoxIndex;
        if (segmentIndex >= inv.Count || inv[segmentIndex].Empty) return false;

        ItemSlot jarSlot = inv[segmentIndex];

        if (jarSlot.Itemstack?.Collectible is IContainedInteractable ici) {
            MarkDirty();
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

            ItemSlot jarSlot = inv[segment];
            if (jarSlot.Empty) return;

            var contents = GetContents(Api.World, jarSlot.Itemstack);

            if (contents != null && contents.Length > 0) {
                DummySlot dummySlot = new(contents[0], inv);

                string perishInfo = PerishableInfoCompact(Api.World, dummySlot, 0f, false, false).Trim();
                if (perishInfo.StartsWith(",")) {
                    perishInfo = perishInfo.Substring(1).Trim();
                }

                if (!string.IsNullOrEmpty(perishInfo)) {
                    sb.Replace(")</font>", ", " + perishInfo + ")</font>");
                }

                string dryInfo = TransitionInfoCompact(Api.World, dummySlot, EnumTransitionType.Dry, TransitionDisplayMode.Percentage);
                if (!string.IsNullOrEmpty(dryInfo)) {
                    sb.AppendLine(dryInfo);
                }
            }
        }
    }
}
