namespace FoodShelves;

public class RestrictionData {
    public string[] CollectibleTypes { get; set; }
    public string[] CollectibleCodes { get; set; }
    public Dictionary<string, string[]> GroupingCodes { get; set; }
}

public static class Restrictions
{
    #region Shelveable

    public const string Shelvable = "shelvable";
    public static bool ShelvableCheck(this CollectibleObject obj) => obj?.Attributes?[Shelvable].AsBool() == true;
    public static bool ShelvableCheck(this ItemSlot slot) => slot?.Itemstack?.Collectible?.Attributes?[Shelvable].AsBool() == true;

    #endregion

    #region FirkinRack

    public const string FirkinRack = "firkinrackcheck";
    public static bool FirkinRackCheck(this CollectibleObject obj) => obj?.Attributes?[FirkinRack].AsBool() == true;
    public static bool FirkinRackCheck(this ItemSlot slot) => slot?.Itemstack?.Collectible?.Attributes?[FirkinRack].AsBool() == true;

    #endregion
}
