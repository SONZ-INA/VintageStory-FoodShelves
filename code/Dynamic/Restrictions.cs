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

    #region Baskets

    #region VegetableBasket

    public const string VegetableBasket = "vegetablebasketcheck";
    public static bool VegetableBasketCheck(this CollectibleObject obj) => obj?.Attributes?[VegetableBasket].AsBool() == true;
    public static bool VegetableBasketCheck(this ItemSlot slot) {
        if (slot?.Itemstack?.Collectible?.Attributes?[VegetableBasket].AsBool() == false) return false;
        if (slot?.Inventory?.ClassName == "hopper") return false;
        return true;
    }

    #endregion

    #region EggBasket

    public const string EggBasket = "eggbasketcheck";
    public static bool EggBasketCheck(this CollectibleObject obj) => obj?.Attributes?[EggBasket].AsBool() == true;
    public static bool EggBasketCheck(this ItemSlot slot) => slot?.Itemstack?.Collectible?.Attributes?[EggBasket].AsBool() == true;

    #endregion

    #endregion

    #region Barrels

    #region BarrelRack

    public const string BarrelRack = "barrelrackcheck";
    public static bool BarrelRackCheck(this CollectibleObject obj) => obj?.Attributes?[BarrelRack].AsBool() == true;
    public static bool BarrelRackCheck(this ItemSlot slot) => slot?.Itemstack?.Collectible?.Attributes?[BarrelRack].AsBool() == true;

    #endregion

    #region BarrelRackBig

    public const string BarrelRackBig = "barrelrackbigcheck";
    public static bool BarrelRackBigCheck(this CollectibleObject obj) => obj?.Attributes?[BarrelRackBig].AsBool() == true;
    public static bool BarrelRackBigCheck(this ItemSlot slot) => slot?.Itemstack?.Collectible?.Attributes?[BarrelRackBig].AsBool() == true;

    #endregion

    #region FirkinRack

    public const string FirkinRack = "firkinrackcheck";
    public static bool FirkinRackCheck(this CollectibleObject obj) => obj?.Attributes?[FirkinRack].AsBool() == true;
    public static bool FirkinRackCheck(this ItemSlot slot) => slot?.Itemstack?.Collectible?.Attributes?[FirkinRack].AsBool() == true;

    #endregion

    #endregion
}
