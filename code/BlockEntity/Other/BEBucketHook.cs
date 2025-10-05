
using System.Linq;

namespace FoodShelves;

public class BEBucketHook : BEBaseFSContainer {
    protected override string CantPlaceMessage => "foodshelves:Only buckets can be placed on this hook.";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByBlock;

    public BEBucketHook() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck)); }

    protected override float[][] genTransformationMatrices() {
        float[][] tfMatrices = new float[SlotCount][];

        tfMatrices[0] = new Matrixf()
            .Translate(0.5f, 0, 0.5f)
            .RotateYDeg(block.Shape.rotateY)
            .Translate(-.5f, .2f, -.54f)
            .Values;

        return tfMatrices;
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb) {
        base.GetBlockInfo(forPlayer, sb);

        if (!inv[0].Empty) {
            var contents = GetContents(Api.World, inv[0].Itemstack);
            sb.Append(" <font color=\"#ababab\">");
            if (contents.Length > 0) {
                if (contents[0] != null) {
                    sb.Append(GetAmountOfLiters(contents[0]));

                    // Create dummy inventory and slot for transition calculations
                    DummyInventory dummyInv = new(Api);
                    ItemSlot contentSlot = new ItemSlotSurvival(dummyInv) {
                        Itemstack = contents[0]
                    };

                    // Get transition states
                    TransitionState[] transitionStates = contentSlot.Itemstack?.Collectible.UpdateAndGetTransitionStates(Api.World, contentSlot);

                    if (transitionStates != null) {
                        // Find perish transition state
                        TransitionState perishState = transitionStates.FirstOrDefault(state =>
                            state.Props.Type == EnumTransitionType.Perish &&
                            contentSlot.Itemstack.Collectible.GetTransitionRateMul(Api.World, contentSlot, state.Props.Type) > 0);

                        if (perishState != null) {
                            float perishRate = contentSlot.Itemstack.Collectible.GetTransitionRateMul(Api.World, inv[0], perishState.Props.Type);
                            float freshHoursLeft = perishState.FreshHoursLeft / perishRate;
                            sb.Append(" " + GetTimeRemainingText(Api.World, freshHoursLeft, EnumTransitionType.Perish, perishState.TransitionLevel));
                        }
                    }
                }
            }
            else {
                sb.Append(Lang.Get("foodshelves:Empty."));
            }

            sb.Append("</font>");
        }
    }
}
