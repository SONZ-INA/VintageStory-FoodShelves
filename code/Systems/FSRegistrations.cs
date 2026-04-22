namespace FoodShelves;

/// <summary>
/// Handles registering all Food Shelves Block and BlockEntity classes.
/// </summary>
public static class FSRegistrations {
    /// <summary>
    /// Registers all Block classes for Food Shelves.
    /// </summary>
    public static void RegisterBlockClasses(ICoreAPI api) {
        // BaseVariants
        api.RegisterBlockClass("FoodShelves.BlockFSContainer", typeof(BaseFSContainer));

        // Barrels
        api.RegisterBlockClass("FoodShelves.BlockBarrelRack", typeof(BlockBarrelRack));
        api.RegisterBlockClass("FoodShelves.BlockTunRack", typeof(BlockTunRack));

        // Baskets
        api.RegisterBlockClass("FoodShelves.BlockFruitBasket", typeof(BlockFruitBasket));
        api.RegisterBlockClass("FoodShelves.BlockVegetableBasket", typeof(BlockVegetableBasket));
        api.RegisterBlockClass("FoodShelves.BlockEggBasket", typeof(BlockEggBasket));
        api.RegisterBlockClass("FoodShelves.BlockMushroomBasket", typeof(BlockMushroomBasket));

        // Coolers
        api.RegisterBlockClass("FoodShelves.BlockCoolingCabinet", typeof(BlockCoolingCabinet));
        api.RegisterBlockClass("FoodShelves.BlockMeatFreezer", typeof(BlockMeatFreezer));
        api.RegisterBlockClass("FoodShelves.BlockFruitCooler", typeof(BlockFruitCooler));

        // Glassware
        api.RegisterBlockClass("FoodShelves.BlockWallCabinet", typeof(BlockWallCabinet));
        //api.RegisterBlockClass("FoodShelves.BlockGlassJar", typeof(BlockGlassJar));

        // Other
        api.RegisterBlockClass("FoodShelves.BlockCeilingRack", typeof(BlockCeilingRack));

        // Shelves
        api.RegisterBlockClass("FoodShelves.BlockDoubleShelf", typeof(BlockDoubleShelf));
    }

    /// <summary>
    /// Registers all BlockEntity classes for Food Shelves.
    /// </summary>
    public static void RegisterBlockEntityClasses(ICoreAPI api) {
        // Barrels
        api.RegisterBlockEntityClass("FoodShelves.BEBarrelRack", typeof(BEBarrelRack));
        api.RegisterBlockEntityClass("FoodShelves.BETunRack", typeof(BETunRack));

        // Baskets
        api.RegisterBlockEntityClass("FoodShelves.BEEggBasket", typeof(BEEggBasket));
        api.RegisterBlockEntityClass("FoodShelves.BEFruitBasket", typeof(BEFruitBasket));
        api.RegisterBlockEntityClass("FoodShelves.BEVegetableBasket", typeof(BEVegetableBasket));
        api.RegisterBlockEntityClass("FoodShelves.BEMushroomBasket", typeof(BEMushroomBasket));

        // Coolers
        api.RegisterBlockEntityClass("FoodShelves.BECoolingCabinet", typeof(BECoolingCabinet));
        api.RegisterBlockEntityClass("FoodShelves.BEMeatFreezer", typeof(BEMeatFreezer));
        api.RegisterBlockEntityClass("FoodShelves.BEFruitCooler", typeof(BEFruitCooler));

        // Glassware
        api.RegisterBlockEntityClass("FoodShelves.BELargeJar", typeof(BELargeJar));
        api.RegisterBlockEntityClass("FoodShelves.BEFoodDisplayBlock", typeof(BEFoodDisplayBlock));
        api.RegisterBlockEntityClass("FoodShelves.BEFoodDisplayCase", typeof(BEFoodDisplayCase));
        api.RegisterBlockEntityClass("FoodShelves.BEWallCabinet", typeof(BEWallCabinet));
        api.RegisterBlockEntityClass("FoodShelves.BESeedBins", typeof(BESeedBins));
        //api.RegisterBlockEntityClass("FoodShelves.BEGlassJar", typeof(BEGlassJar));

        // Other
        api.RegisterBlockEntityClass("FoodShelves.BEPumpkinCase", typeof(BEPumpkinCase));
        api.RegisterBlockEntityClass("FoodShelves.BETableWShelf", typeof(BETableWShelf));
        api.RegisterBlockEntityClass("FoodShelves.BEFlourSack", typeof(BEFlourSack));
        api.RegisterBlockEntityClass("FoodShelves.BEBucketHook", typeof(BEBucketHook));
        api.RegisterBlockEntityClass("FoodShelves.BECeilingRack", typeof(BECeilingRack));

        // Shelves
        api.RegisterBlockEntityClass("FoodShelves.BEBarShelf", typeof(BEBarShelf));
        api.RegisterBlockEntityClass("FoodShelves.BEBreadShelf", typeof(BEBreadShelf));
        api.RegisterBlockEntityClass("FoodShelves.BEDoubleShelf", typeof(BEDoubleShelf));
        api.RegisterBlockEntityClass("FoodShelves.BEEggShelf", typeof(BEEggShelf));
        api.RegisterBlockEntityClass("FoodShelves.BEPieShelf", typeof(BEPieShelf));
        api.RegisterBlockEntityClass("FoodShelves.BESeedShelf", typeof(BESeedShelf));
        api.RegisterBlockEntityClass("FoodShelves.BEShortShelf", typeof(BEShortShelf));
        api.RegisterBlockEntityClass("FoodShelves.BESushiShelf", typeof(BESushiShelf));
    }
}
