namespace FoodShelves;

public class RestrictionData {
    public string[] CollectibleTypes { get; set; }
    public string[] CollectibleCodes { get; set; }
    public Dictionary<string, string[]> GroupingCodes { get; set; }
}

public static class Restrictions
{
    #region General

    #region Shelveable

    public const string Shelvable = "shelvable";
    public static bool ShelvableCheck(this CollectibleObject obj) => obj?.Attributes?[Shelvable].AsBool() == true;
    public static bool ShelvableCheck(this ItemSlot slot) => slot?.Itemstack?.Collectible?.Attributes?[Shelvable].AsBool() == true;

    #endregion

    #region FoodUniversal

    public const string FoodUniversal = "fooduniversalcheck";
    public static bool FoodUniversalCheck(this CollectibleObject obj) => obj?.Attributes?[FoodUniversal].AsBool() == true;
    public static bool FoodUniversalCheck(this ItemSlot slot) {
        if (slot?.Itemstack?.Collectible?.Attributes?[FoodUniversal].AsBool() == false) return false;
        if (slot?.Inventory?.ClassName == "hopper") return false;
        return true;
    }

    #endregion

    #region HolderUniversal

    public const string HolderUniversal = "holderuniversalcheck";
    public static bool HolderUniversalCheck(this CollectibleObject obj) => obj?.Attributes?[HolderUniversal].AsBool() == true;
    public static bool HolderUniversalCheck(this ItemSlot slot) {
        if (slot?.Itemstack?.Collectible?.Attributes?[HolderUniversal].AsBool() != true) return false;
        if (slot?.Inventory?.ClassName == "hopper") return false;
        return true;
    }

    #endregion

    #region LiquidyStuff

    public const string LiquidyStuff = "liquidystuffcheck";
    public static bool LiquidyStuffCheck(this CollectibleObject obj) => obj?.Attributes?[LiquidyStuff].AsBool() == true;
    public static bool LiquidyStuffCheck(this ItemSlot slot) => slot?.Itemstack?.Collectible?.Attributes?[LiquidyStuff].AsBool() == true;

    #endregion

    #region CoolingOnly

    public const string CoolingOnly = "coolingonlycheck";
    public static bool CoolingOnlyCheck(this CollectibleObject obj) => obj?.Attributes?[CoolingOnly].AsBool() == true;
    public static bool CoolingOnlyCheck(this ItemSlot slot) {
        if (slot?.Itemstack?.Collectible?.Attributes?[CoolingOnly].AsBool() == false) return false;
        if (slot?.Inventory?.ClassName == "hopper") return false;
        return true;
    }

    #endregion

    #endregion

    #region Shelves

    #region PieShelf

    public const string PieShelf = "pieshelfcheck";
    public static bool PieShelfCheck(this CollectibleObject obj) => obj?.Attributes?[PieShelf].AsBool() == true;
    public static bool PieShelfCheck(this ItemSlot slot) => slot?.Itemstack?.Collectible?.Attributes?[PieShelf].AsBool() == true;

    #endregion

    #region BreadShelf

    public const string BreadShelf = "breadshelfcheck";
    public static bool BreadShelfCheck(this CollectibleObject obj) => obj?.Attributes?[BreadShelf].AsBool() == true;
    public static bool BreadShelfCheck(this ItemSlot slot) => slot?.Itemstack?.Collectible?.Attributes?[BreadShelf].AsBool() == true;

    #endregion

    #region BarShelf

    public const string BarShelf = "barshelfcheck";
    public static bool BarShelfCheck(this CollectibleObject obj) => obj?.Attributes[BarShelf].AsBool() == true;
    public static bool BarShelfCheck(this ItemSlot slot) => slot?.Itemstack?.Collectible?.Attributes?[BarShelf].AsBool() == true;

    #endregion

    #region SushiShelf

    public const string SushiShelf = "sushishelfcheck";
    public static bool SushiShelfCheck(this CollectibleObject obj) => obj?.Attributes?[SushiShelf].AsBool() == true;
    public static bool SushiShelfCheck(this ItemSlot slot) => slot?.Itemstack?.Collectible?.Attributes?[SushiShelf].AsBool() == true;

    #endregion

    #region SeedShelf

    public const string SeedShelf = "seedshelfcheck";
    public static bool SeedShelfCheck(this CollectibleObject obj) => obj?.Attributes?[SeedShelf].AsBool() == true;
    public static bool SeedShelfCheck(this ItemSlot slot) => slot?.Itemstack?.Collectible?.Attributes?[SeedShelf].AsBool() == true;

    #endregion

    #region EggShelf

    public const string EggShelf = "eggshelfcheck";
    public static bool EggShelfCheck(this CollectibleObject obj) => obj?.Attributes?[EggShelf].AsBool() == true;
    public static bool EggShelfCheck(this ItemSlot slot) => slot?.Itemstack?.Collectible?.Attributes?[EggShelf].AsBool() == true;

    #endregion

    #region GlassJarShelf

    public const string GlassJarShelf = "glassjarshelfcheck";
    public static bool GlassJarShelfCheck(this CollectibleObject obj) => obj?.Attributes?[GlassJarShelf].AsBool() == true;
    public static bool GlassJarShelfCheck(this ItemSlot slot) => slot?.Itemstack?.Collectible?.Attributes?[GlassJarShelf].AsBool() == true;

    #endregion

    #region TableWShelf

    public const string TableWShelf = "tablewshelfcheck";
    public static bool TableWShelfCheck(this CollectibleObject obj) => obj?.Attributes?[TableWShelf].AsBool() == true;
    public static bool TableWShelfCheck(this ItemSlot slot) => slot?.Itemstack?.Collectible?.Attributes?[TableWShelf].AsBool() == true;

    #endregion

    #endregion

    #region Baskets

    #region FruitBasket

    public const string FruitBasket = "fruitbasketcheck";
    public static bool FruitBasketCheck(this CollectibleObject obj) => obj?.Attributes?[FruitBasket].AsBool() == true;
    public static bool FruitBasketCheck(this ItemSlot slot) => slot?.Itemstack?.Collectible?.Attributes?[FruitBasket].AsBool() == true;

    #endregion

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


    #region PumpkinCase

    public const string PumpkinCase = "pumpkincasecheck";
    public static bool PumpkinCaseCheck(this CollectibleObject obj) => obj?.Attributes?[PumpkinCase].AsBool() == true;
    public static bool PumpkinCaseCheck(this ItemSlot slot) => slot?.Itemstack?.Collectible?.Attributes?[PumpkinCase].AsBool() == true;

    #endregion
}
