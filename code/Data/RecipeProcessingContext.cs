using Vintagestory.ServerMods;

namespace FoodShelves;

// Used to shorten the parameter count for RecipePatcher
public class RecipeProcessingContext(
    IAsset collectibleRecipes,
    HashSet<string> entriesToReplace,
    VariantData variantData,
    GridRecipeLoader gridRecipeLoader
    ) {
    public HashSet<string> EntriesToReplace { get; set; } = entriesToReplace;
    public VariantData VariantData { get; set; } = variantData;
    public IAsset CollectibleRecipes { get; set; } = collectibleRecipes;
    public GridRecipeLoader GridRecipeLoader { get; set; } = gridRecipeLoader;
}
