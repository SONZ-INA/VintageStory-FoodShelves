using Vintagestory.ServerMods;

namespace FoodShelves;

// Used to shorten the parameter count for RecipePatcher
public class RecipeProcessingContext {
    public HashSet<string> EntriesToReplace { get; set; }
    public VariantData VariantData { get; set; }
    public IAsset CollectibleRecipes { get; set; }
    public GridRecipeLoader GridRecipeLoader { get; set; }

    public RecipeProcessingContext(
        IAsset collectibleRecipes,
        HashSet<string> entriesToReplace,
        VariantData variantData,
        GridRecipeLoader gridRecipeLoader
    ) {
        CollectibleRecipes = collectibleRecipes;
        EntriesToReplace = entriesToReplace;
        VariantData = variantData;
        GridRecipeLoader = gridRecipeLoader;
    }
}
