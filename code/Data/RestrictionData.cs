namespace FoodShelves;

public sealed class RestrictionData {
    public string[] CollectibleTypes { get; set; }
    public string[] CollectibleCodes { get; set; }
    public string[] BlacklistedCodes { get; set; }
    public Dictionary<string, string[]> GroupingCodes { get; set; }
}
