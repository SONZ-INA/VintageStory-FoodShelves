namespace FoodShelves;

public class VariantData {
    // Contains a key that will act as a "keyword to replace" with all possible values.
    public Dictionary<string, string[]> RecipeVariantData { get; set; }

    // Contains a "key" that will act as a "keyword to replace" with all possible matches.
    // Values has the key-value pair, key being the keyword to replace with, and values are "skip these files".
    public Dictionary<string, Dictionary<string, string[]>> DefaultFallback { get; set; }
}
