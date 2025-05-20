using System.Linq;
using Vintagestory.ServerMods;

namespace FoodShelves;

public static class RecipePatcher {
    public static void SupportModdedIngredients(ICoreAPI api) {
        api.Logger.Debug("[FoodShelves] Started patching recipes...");
        
        long elapsedMilliseconds = api.World.ElapsedMilliseconds;
        int recipeCountBefore = api.World.GridRecipes.Count;

        string debugCode = "ceilingjar";

        // Data needed to process
        VariantData variantData = api.LoadAsset<VariantData>("foodshelves:config/variantdata/variantdata.json");
        List<IAsset> allCollectibleRecipes = api.Assets.GetManyInCategory("recipes", "grid", "foodshelves");
        GridRecipeLoader gridRecipeLoader = api.ModLoader.GetModSystem<GridRecipeLoader>();

        // Modded variant support
        SwitchModdedIngredients(variantData, allCollectibleRecipes, gridRecipeLoader, debugCode);

        // Variantless recipe fallback (for all possible variations)
        var entriesList = variantData.DefaultFallback.Keys.ToList();
        int combinationCount = (1 << entriesList.Count) - 1;

        for (int i = 1; i <= combinationCount; i++) { // Start from 1 to avoid handling the empty-set
            HashSet<string> entriesToReplace = new();

            // Generate all possible sets (factorial complexity)
            for (int j = 0; j < entriesList.Count; j++) {
                if ((i & (1 << j)) > 0) {
                    entriesToReplace.Add(entriesList[j]);
                }
            }

            // Find all "game:ingredient-*" to replace to "custommod:ingredient-*" to support modded variants as well
            Dictionary<string, string[]> moddedFallback = new();
            foreach (var entry in variantData.RecipeVariantData) {
                if (!entriesToReplace.Contains(entry.Key)) {
                    moddedFallback.Add(entry.Key, entry.Value);
                }
            }

            // Start patching recipes
            foreach (var collectibleRecipes in allCollectibleRecipes) {
                bool skip = false;
                foreach (var entry in entriesToReplace) {
                    string[] skipFiles = variantData.DefaultFallback[entry].FirstOrDefault().Value;

                    if (skipFiles.Length == 0) break;
                    if (skipFiles.Contains(collectibleRecipes.Name + ".json") == true) {
                        skip = true;
                        break;
                    }
                }
                if (skip) continue;

                // Actually start patching recipes for each block
                foreach (var recipe in collectibleRecipes.ToObject<GridRecipe[]>()) {
                    if (!recipe.Enabled) continue;

                    // Default recipes count: 749
                    // Wool & More: 2066 (TOO much)
                    if (!recipe.Output.Code.ToString().Contains(debugCode)) continue;

                    // Floursack only:
                    // game: 12
                    // wool: 252

                    if (moddedFallback.Count > 0) {
                        bool containsModdedVariant = false;

                        foreach (var moddedEntry in moddedFallback) {
                            string originalKey = moddedEntry.Key;
                            string[] variantValues = moddedEntry.Value;

                            foreach (string variantValue in variantValues) {
                                // TODO: Ovo za changed i ostalo izdvoji u methodu.

                                Dictionary<string, bool> ingredientsChangedModded = new();
                                foreach (string entry in entriesToReplace) {
                                    ingredientsChangedModded.Add(entry, false);
                                }
                                bool skipDefaultModded = false;

                                foreach (string entry in entriesToReplace) {
                                    string ingredientLetter = null;
                                    foreach (var ingredient in recipe.Ingredients) {
                                        if (entry == ingredient.Value.Code) {
                                            ingredientLetter = ingredient.Key;
                                            break;
                                        }
                                    }

                                    if (ingredientLetter == null) {
                                        skipDefaultModded = true;
                                        break;
                                    }

                                    int ingredientGridCount = 0;
                                    foreach (char c in recipe.IngredientPattern) {
                                        if ($"{c}" == ingredientLetter) {
                                            if (ingredientGridCount == 0) ingredientGridCount++;
                                            else {
                                                ingredientsChangedModded[entry] = true;
                                                break;
                                            }
                                        }
                                    }
                                }

                                // If either of the ingredients is only used once, skip it.
                                foreach (var pair in ingredientsChangedModded) {
                                    if (pair.Value == false) {
                                        skipDefaultModded = true;
                                        break;
                                    }
                                    else {
                                        ingredientsChangedModded[pair.Key] = false;
                                    }
                                }
                                if (skipDefaultModded) continue;

                                // Handle all recipe changes with "game" domain.
                                GridRecipe newRecipeModded = recipe.Clone();

                                foreach (var ingredient in recipe.Ingredients) {
                                    if (ingredient.Value.Code == originalKey)
                                        newRecipeModded.Ingredients[ingredient.Key].Code = variantValue;

                                    if (entriesToReplace.Contains(ingredient.Value.Code)) {
                                        newRecipeModded.Ingredients[ingredient.Key].Code = variantData.DefaultFallback[ingredient.Value.Code].FirstOrDefault().Key;
                                        string toChange = newRecipeModded.Ingredients[ingredient.Key].Name;
                                        newRecipeModded.Ingredients[ingredient.Key].Name = null;

                                        ingredientsChangedModded[ingredient.Value.Code] = true;

                                        // Attribute handling
                                        if (newRecipeModded.Output.Attributes != null) {
                                            JToken jTokenAttributes = newRecipeModded.Output.Attributes.AsObject<JToken>();
                                            var attributes = jTokenAttributes.First.First.Children().ToList();
                                            List<JToken> newJTokenAttributes = new();

                                            foreach (var item in attributes) {
                                                if (item.ToString().Contains(toChange)) {
                                                    newJTokenAttributes.Add(item);
                                                }
                                            }

                                            if (newJTokenAttributes.Count > 0) {
                                                foreach (JToken attribute in newJTokenAttributes) {
                                                    attributes.Remove(attribute);
                                                }
                                            }

                                            if (attributes.Count == 0) {
                                                newRecipeModded.Output.Attributes = null;
                                            }
                                            else {
                                                string propsJson = string.Join(", ", attributes.Select(attr => attr.ToString()));
                                                string attributesJson = "{ \"FSAttributes\": {" + propsJson + "} }";
                                                newRecipeModded.Output.Attributes = new(JToken.Parse(attributesJson));
                                            }
                                        }

                                        containsModdedVariant = true;
                                    }
                                }

                                bool recipeChangedModded = true;
                                foreach (var pair in ingredientsChangedModded) {
                                    if (pair.Value == false) {
                                        recipeChangedModded = false;
                                        break;
                                    }
                                }

                                if (recipeChangedModded) gridRecipeLoader.LoadRecipe(collectibleRecipes.Location, newRecipeModded);
                                if (!containsModdedVariant) break;
                            }

                            if (!containsModdedVariant) break;
                        }
                    }


                    // Only load recipe if all ingredients are changed, to avoid duplicates.
                    Dictionary<string, bool> ingredientsChanged = new();
                    foreach (string entry in entriesToReplace) {
                        ingredientsChanged.Add(entry, false);
                    }
                    bool skipDefault = false;

                    // First pass - determine if "*:ingredient-*" is a plausible switch.
                    // eg. if "game:cloth-*" is only found in 1 grid slot in a recipe, even when loading the fallback it will always have attributes (never fallback).
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

                        if (ingredientLetter == null) {
                            skipDefault = true;
                            break;
                        }

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

                    // If either of the ingredients is only used once, skip it.
                    foreach (var pair in ingredientsChanged) {
                        if (pair.Value == false) {
                            skipDefault = true;
                            break;
                        }
                        else {
                            ingredientsChanged[pair.Key] = false;
                        }
                    }
                    if (skipDefault) continue;

                    // Handle all recipe changes with "game" domain.
                    GridRecipe newRecipe = recipe.Clone();

                    foreach (var ingredient in recipe.Ingredients) {
                        if (entriesToReplace.Contains(ingredient.Value.Code)) {
                            newRecipe.Ingredients[ingredient.Key].Code = variantData.DefaultFallback[ingredient.Value.Code].FirstOrDefault().Key;
                            string toChange = newRecipe.Ingredients[ingredient.Key].Name;
                            newRecipe.Ingredients[ingredient.Key].Name = null;

                            ingredientsChanged[ingredient.Value.Code] = true;

                            // Attribute handling
                            if (newRecipe.Output.Attributes != null) {
                                JToken jTokenAttributes = newRecipe.Output.Attributes.AsObject<JToken>();
                                var attributes = jTokenAttributes.First.First.Children().ToList();

                                List<JToken> newJTokenAttributes = new();

                                foreach (var item in attributes) {
                                    if (item.ToString().Contains(toChange)) {
                                        newJTokenAttributes.Add(item);
                                    }
                                }

                                if (newJTokenAttributes.Count > 0) {
                                    foreach (JToken attribute in newJTokenAttributes) {
                                        attributes.Remove(attribute);
                                    }
                                }

                                if (attributes.Count == 0) {
                                    newRecipe.Output.Attributes = null;
                                }
                                else {
                                    string propsJson = string.Join(", ", attributes.Select(attr => attr.ToString()));
                                    string attributesJson = "{ \"FSAttributes\": {" + propsJson + "} }";
                                    newRecipe.Output.Attributes = new(JToken.Parse(attributesJson));
                                }
                            }
                        }
                    }

                    bool recipeChanged = true;
                    foreach (var pair in ingredientsChanged) {
                        if (pair.Value == false) {
                            recipeChanged = false;
                            break;
                        }
                    }

                    if (recipeChanged) gridRecipeLoader.LoadRecipe(collectibleRecipes.Location, newRecipe);
                }
            }
        }

        api.Logger.Debug($"[FoodShelves] Patched in {api.World.GridRecipes.Count - recipeCountBefore} recipes in {Math.Round((api.World.ElapsedMilliseconds - elapsedMilliseconds) / 1000.0, 2)}s");
    }

    public static void SwitchModdedIngredients(VariantData variantData, List<IAsset> allCollectibleRecipes, GridRecipeLoader gridRecipeLoader, string debugCode = "") {
        foreach (var entry in variantData.RecipeVariantData) {
            foreach (string variantModItem in entry.Value) {
                foreach (var collectibleRecipes in allCollectibleRecipes) {
                    foreach (var recipe in collectibleRecipes.ToObject<GridRecipe[]>()) {
                        if (!recipe.Enabled) continue;

                        if (!recipe.Output.Code.ToString().Contains(debugCode)) continue;

                        GridRecipe newRecipe = recipe.Clone();
                        bool recipeChanged = false;

                        // Switch ingredient from "game:ingredient-*" to "custommod:ingredient-*".
                        foreach (var ingredient in recipe.Ingredients) {
                            if (ingredient.Value.Code == entry.Key) {
                                newRecipe.Ingredients[ingredient.Key].Code = variantModItem;
                                newRecipe.Name = collectibleRecipes.Location;
                                recipeChanged = true;
                            }
                        }

                        if (recipeChanged) gridRecipeLoader.LoadRecipe(collectibleRecipes.Location, newRecipe);
                    }
                }
            }
        }
    }
}