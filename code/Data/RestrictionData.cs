namespace FoodShelves;

public class RestrictionData {
    public string[] CollectibleTypes { get; set; }
    public string[] CollectibleCodes { get; set; }
    public Dictionary<string, string[]> GroupingCodes { get; set; }
}
