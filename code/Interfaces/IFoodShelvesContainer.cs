namespace FoodShelves;

public interface IFoodShelvesContainer {
    public ITreeAttribute VariantAttributes { get; set; }
    public bool OnInteract(IPlayer byPlayer, BlockSelection blockSel);
}
