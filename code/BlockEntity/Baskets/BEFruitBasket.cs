namespace FoodShelves;

public class BEFruitBasket : BEBaseFSBasket {
    protected override string CeilingAttachedUtil => ShapeReferences.utilFruitBasket;
    protected override string CantPlaceMessage => "foodshelves:Only fruit can be placed in this basket.";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByBlockAverageAndSoonest;

    public override int ItemsPerSegment => 22;

    public BEFruitBasket() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck)); }
}
