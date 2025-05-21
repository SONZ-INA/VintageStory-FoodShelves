using System.Linq;
using Vintagestory.ServerMods;

namespace FoodShelves;

public static class RecipePatcher {
    public static void SupportModdedIngredients(ICoreAPI api) {
        api.Logger.Debug("[FoodShelves] Started patching recipes...");
        
        long elapsedMilliseconds = api.World.ElapsedMilliseconds;
        int recipeCountBefore = api.World.GridRecipes.Count;

        string debugCode = null; // Used to "filter" only 1 (or multiple) block codes that will be patched

        if (debugCode != null && debugCode != "") api.Logger.Warning($"[FoodShelves] Debug code is \"{debugCode}\". Will only patch those recipes");

        // Data needed to process
        VariantData variantData = api.LoadAsset<VariantData>("foodshelves:config/variantdata/variantdata.json");
        List<IAsset> allCollectibleRecipes = api.Assets.GetManyInCategory("recipes", "grid", "foodshelves");
        GridRecipeLoader gridRecipeLoader = api.ModLoader.GetModSystem<GridRecipeLoader>();

        // Switcher - Switch "game:" with custom mod domains.
        SwitchModdedIngredients(variantData, allCollectibleRecipes, gridRecipeLoader, debugCode);
        int recipeCountAfterSwitch = api.World.GridRecipes.Count;
        api.Logger.Debug($"\tPatched in {recipeCountAfterSwitch - recipeCountBefore} recipes using the switcher method.");

        // Fallback recipes - those that will accept "*:ingredient-*" and fallback to default textures.
        DefaultFallbackAddition(variantData, allCollectibleRecipes, gridRecipeLoader, debugCode);
        api.Logger.Debug($"\tPatched in {api.World.GridRecipes.Count - recipeCountAfterSwitch} recipes using the default fallback method.");

        api.Logger.Debug($"[FoodShelves] Patched in {api.World.GridRecipes.Count - recipeCountBefore} total recipes in {Math.Round((api.World.ElapsedMilliseconds - elapsedMilliseconds) / 1000.0, 2)}s");
    }

    #region Switcher

    public static void SwitchModdedIngredients(VariantData variantData, List<IAsset> allCollectibleRecipes, GridRecipeLoader gridRecipeLoader, string debugCode = "") {
        // Factorial Complexity
        var entriesList = variantData.RecipeVariantData.Keys.ToList();
        int combinationCount = (1 << entriesList.Count) - 1;

        for (int i = 1; i <= combinationCount; i++) {
            List<string> keysToReplace = new();

            for (int j = 0; j < entriesList.Count; j++) {
                if ((i & (1 << j)) > 0) {
                    keysToReplace.Add(entriesList[j]);
                }
            }

            // Generate all possible combinations of values for the selected keys
            GenerateAndApplyValueCombinations(keysToReplace, 0, new Dictionary<string, string>(), variantData, allCollectibleRecipes, gridRecipeLoader, debugCode);
        }
    }

    // Helper method to generate all combinations of values for the selected keys
    private static void GenerateAndApplyValueCombinations(
        List<string> keysToReplace,
        int currentKeyIndex,
        Dictionary<string, string> currentReplacements,
        VariantData variantData,
        List<IAsset> allCollectibleRecipes,
        GridRecipeLoader gridRecipeLoader,
        string debugCode
    ) {
        // End of tree data structure, apply apply all replacements.
        if (currentKeyIndex >= keysToReplace.Count) {
            ApplyReplacements(currentReplacements, allCollectibleRecipes, gridRecipeLoader, debugCode);
            return;
        }

        string currentKey = keysToReplace[currentKeyIndex];
        string[] possibleValues = variantData.RecipeVariantData[currentKey];

        // For each possible value for the current key
        foreach (string value in possibleValues) {
            // Create a copy of the current replacements
            Dictionary<string, string> newReplacements = new(currentReplacements) {
                [currentKey] = value // Add the current key-value pair
            };

            // Recursively generate combinations for the next key
            GenerateAndApplyValueCombinations(keysToReplace, currentKeyIndex + 1, newReplacements, variantData, allCollectibleRecipes, gridRecipeLoader, debugCode);
        }
    }

    private static void ApplyReplacements(Dictionary<string, string> replacements, List<IAsset> allCollectibleRecipes, GridRecipeLoader gridRecipeLoader, string debugCode) {
        foreach (var collectibleRecipes in allCollectibleRecipes) {
            foreach (var recipe in collectibleRecipes.ToObject<GridRecipe[]>()) {
                if (!recipe.Enabled) continue;
                if (!string.IsNullOrEmpty(debugCode) && !recipe.Output.Code.ToString().Contains(debugCode)) continue;

                GridRecipe newRecipe = recipe.Clone();
                bool recipeChanged = false;

                // Apply all replacements for this combination
                foreach (var ingredient in recipe.Ingredients) {
                    string ingredientCode = ingredient.Value.Code;
                    if (replacements.TryGetValue(ingredientCode, out string value)) {
                        newRecipe.Ingredients[ingredient.Key].Code = value;
                        recipeChanged = true;
                    }
                }

                if (recipeChanged) {
                    gridRecipeLoader.LoadRecipe(collectibleRecipes.Location, newRecipe);
                }
            }
        }
    }

    #endregion

    #region Fallback

    public static void DefaultFallbackAddition(VariantData variantData, List<IAsset> allCollectibleRecipes, GridRecipeLoader gridRecipeLoader, string debugCode = "") {
        // Variantless recipe fallback (for all possible variations - factorial complexity)
        var entriesList = variantData.DefaultFallback.Keys.ToList();
        int combinationCount = (1 << entriesList.Count) - 1;

        for (int i = 1; i <= combinationCount; i++) { // Start from 1 to avoid handling the empty-set
            HashSet<string> entriesToReplace = new();

            // Generate all possible sets
            for (int j = 0; j < entriesList.Count; j++) {
                if ((i & (1 << j)) > 0) {
                    entriesToReplace.Add(entriesList[j]);
                }
            }

            ProcessCombination(entriesToReplace, variantData, allCollectibleRecipes, gridRecipeLoader, debugCode);
        }
    }

    private static void ProcessCombination(HashSet<string> entriesToReplace, VariantData variantData, List<IAsset> allCollectibleRecipes, GridRecipeLoader gridRecipeLoader, string debugCode) {
        // Find entries from RecipeVariantData that are NOT in the current combination
        // These will be used for modded variant fallbacks
        Dictionary<string, string[]> moddedFallback = new();
        foreach (var entry in variantData.RecipeVariantData) {
            if (!entriesToReplace.Contains(entry.Key)) {
                moddedFallback.Add(entry.Key, entry.Value);
            }
        }

        foreach (var collectibleRecipes in allCollectibleRecipes) {
            if (ShouldSkipFile(collectibleRecipes.Name, entriesToReplace, variantData))
                continue;

            var contextData = new RecipeProcessingContext(collectibleRecipes, entriesToReplace, variantData, gridRecipeLoader);
            ProcessRecipeCollection(contextData, moddedFallback, debugCode);
        }
    }

    private static bool ShouldSkipFile(string fileName, HashSet<string> entriesToReplace, VariantData variantData) {
        foreach (var entry in entriesToReplace) {
            string[] skipFiles = variantData.DefaultFallback[entry].FirstOrDefault().Value;

            if (skipFiles.Length == 0) continue;
            if (skipFiles.Contains(fileName + ".json")) return true;
        }

        return false;
    }

    private static void ProcessRecipeCollection(RecipeProcessingContext contextData, Dictionary<string, string[]> moddedFallback, string debugCode) {
        foreach (var recipe in contextData.CollectibleRecipes.ToObject<GridRecipe[]>()) {
            if (!recipe.Enabled) continue;
            if (!string.IsNullOrEmpty(debugCode) && !recipe.Output.Code.ToString().Contains(debugCode)) continue;

            Dictionary<string, bool> ingredientsChanged = GetChangedIngredientsDictionary(contextData.EntriesToReplace, recipe);
            if (ingredientsChanged == null) continue;

            ProcessStandardFallback(contextData, recipe, ingredientsChanged);

            if (moddedFallback.Count > 0) {
                ProcessModdedVariants(contextData, recipe, moddedFallback);
            }
        }
    }

    private static void ProcessStandardFallback(RecipeProcessingContext contextData, GridRecipe recipe, Dictionary<string, bool> ingredientsChanged) {
        ApplyRecipeVariant(contextData, recipe, new(), ingredientsChanged); // Empty Dictionary for standard fallback
    }

    private static void ProcessModdedVariants(RecipeProcessingContext contextData, GridRecipe recipe, Dictionary<string, string[]> moddedFallback) {
        // Generate all combinations of modded variants
        GenerateAndApplyModdedVariants(contextData, moddedFallback.Keys.ToList(), 0, new Dictionary<string, string>(), recipe);
    }

    private static void GenerateAndApplyModdedVariants(
        RecipeProcessingContext contextData,
        List<string> moddedKeys,
        int currentKeyIndex,
        Dictionary<string, string> currentReplacements,
        GridRecipe recipe
    ) {
        // Base case: if we've processed all keys, apply the replacements
        if (currentKeyIndex >= moddedKeys.Count) {
            Dictionary<string, bool> ingredientsChanged = GetChangedIngredientsDictionary(contextData.EntriesToReplace, recipe);
            if (ingredientsChanged == null) return;

            ApplyRecipeVariant(contextData, recipe, currentReplacements, ingredientsChanged);
            return;
        }

        string currentKey = moddedKeys[currentKeyIndex];
        string[] possibleValues = contextData.VariantData.RecipeVariantData[currentKey];

        foreach (string value in possibleValues) {
            // Create a copy of the current replacements
            Dictionary<string, string> newReplacements = new(currentReplacements) {
                [currentKey] = value // Add the current key-value pair
            };

            // Recursively generate combinations for the next key
            GenerateAndApplyModdedVariants(contextData, moddedKeys, currentKeyIndex + 1, newReplacements, recipe);
        }
    }

    private static void ApplyRecipeVariant(RecipeProcessingContext contextData, GridRecipe recipe, Dictionary<string, string> moddedReplacements, Dictionary<string, bool> ingredientsChanged) {
        GridRecipe newRecipe = recipe.Clone();

        // Apply modded replacements if any
        foreach (var moddedPair in moddedReplacements) {
            string originalKey = moddedPair.Key;
            string replacementValue = moddedPair.Value;

            foreach (var ingredient in recipe.Ingredients) {
                if (ingredient.Value.Code == originalKey) {
                    newRecipe.Ingredients[ingredient.Key].Code = replacementValue;
                }
            }
        }

        // Apply standard fallback replacements
        foreach (var ingredient in recipe.Ingredients) {
            if (contextData.EntriesToReplace.Contains(ingredient.Value.Code)) {
                newRecipe.Ingredients[ingredient.Key].Code = contextData.VariantData.DefaultFallback[ingredient.Value.Code].FirstOrDefault().Key;
                string ingredientName = newRecipe.Ingredients[ingredient.Key].Name;
                newRecipe.Ingredients[ingredient.Key].Name = null;

                ingredientsChanged[ingredient.Value.Code] = true;

                RemoveAttributesForIngredient(newRecipe, ingredientName);
            }
        }

        // Only load the recipe if all targeted ingredients are changed
        bool allChanged = ingredientsChanged.All(pair => pair.Value);
        if (allChanged) {
            contextData.GridRecipeLoader.LoadRecipe(contextData.CollectibleRecipes.Location, newRecipe);
        }
    }

    private static void RemoveAttributesForIngredient(GridRecipe recipe, string ingredientName) {
        if (recipe.Output.Attributes == null) return;

        JToken jTokenAttributes = recipe.Output.Attributes.AsObject<JToken>();
        var attributes = jTokenAttributes.First.First.Children().ToList();
        List<JToken> attributesToRemove = new();

        // Find attributes related to this ingredient
        foreach (var item in attributes) {
            if (item.ToString().Contains(ingredientName)) {
                attributesToRemove.Add(item);
            }
        }

        // Remove the attributes
        if (attributesToRemove.Count > 0) {
            foreach (JToken attribute in attributesToRemove) {
                attributes.Remove(attribute);
            }
        }

        // Update or remove the attributes based on what remains
        if (attributes.Count == 0) {
            recipe.Output.Attributes = null;
        }
        else {
            string propsJson = string.Join(", ", attributes.Select(attr => attr.ToString()));
            string attributesJson = "{ \"FSAttributes\": {" + propsJson + "} }";
            recipe.Output.Attributes = new(JToken.Parse(attributesJson));
        }
    }

    private static Dictionary<string, bool> GetChangedIngredientsDictionary(HashSet<string> entriesToReplace, GridRecipe recipe) {
        // Only load recipe if all ingredients are changed, to avoid duplicates.
        Dictionary<string, bool> ingredientsChanged = new();
        foreach (string entry in entriesToReplace) {
            ingredientsChanged.Add(entry, false);
        }

        // First pass - determine if "*:ingredient-*" is a plausible switch.
        // eg. if "game:cloth-*" is only found in 1 grid slot in a recipe, even when loading the fallback it will always have attributes (and never fallback).
        // Thus, creating a recipe with "*:cloth-*" without any attributes won't ever be needed in-game, since "something:cloth-*" will always be present
        // Also, only proceed if both ingredients are used in multiple grid slots.
        foreach (string entry in entriesToReplace) {
            string ingredientLetter = null;
            foreach (var ingredient in recipe.Ingredients) {
                if (entry == ingredient.Value.Code) {
                    ingredientLetter = ingredient.Key;
                    break;
                }
            }

            // If ingredient not found in recipe, skip
            if (ingredientLetter == null) return null;

            // Count occurrences in grid
            int ingredientGridCount = 0;
            foreach (char c in recipe.IngredientPattern) {
                if ($"{c}" == ingredientLetter) {
                    if (ingredientGridCount == 0) ingredientGridCount++;
                    else {
                        ingredientsChanged[entry] = true;
                        break;
                    }
                }
            }
        }

        // If either of the ingredients is only used once, return null (skip it).
        foreach (var pair in ingredientsChanged) {
            if (pair.Value == false) return null;
            else ingredientsChanged[pair.Key] = false; // Reset for actual replacement tracking
        }

        return ingredientsChanged;
    }

    #endregion
}