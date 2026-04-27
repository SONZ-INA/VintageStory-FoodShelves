namespace FoodShelves;

public class BEMushroomBasket : BEBaseFSBasket {
    protected override string CeilingAttachedUtil => ShapeReferences.utilMushroomBasket;
    protected override string CantPlaceMessage => "foodshelves:Only mushrooms can be placed in this basket.";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByBlockAverageAndSoonest;

    public override int ItemsPerSegment => 18;

    public BEMushroomBasket() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck)); }
}
