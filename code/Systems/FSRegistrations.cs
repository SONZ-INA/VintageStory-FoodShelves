namespace FoodShelves;

/// <summary>
/// Handles registering all Food Shelves Block, BlockEntity and BlockBehavior classes.
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
        api.RegisterBlockClass("FoodShelves.BlockEggBasket", typeof(BlockEggBasket));
        api.RegisterBlockClass("FoodShelves.BlockFruitBasket", typeof(BlockFruitBasket));
        api.RegisterBlockClass("FoodShelves.BlockMushroomBasket", typeof(BlockMushroomBasket));
        api.RegisterBlockClass("FoodShelves.BlockVegetableBasket", typeof(BlockVegetableBasket));

        // Coolers
        api.RegisterBlockClass("FoodShelves.BlockCoolingCabinet", typeof(BlockCoolingCabinet));
        api.RegisterBlockClass("FoodShelves.BlockFruitCooler", typeof(BlockFruitCooler));
        api.RegisterBlockClass("FoodShelves.BlockMeatFreezer", typeof(BlockMeatFreezer));

        // Glassware
        api.RegisterBlockClass("FoodShelves.BlockJar", typeof(BlockJar));
        api.RegisterBlockClass("FoodShelves.BlockWallCabinet", typeof(BlockWallCabinet));

        // Other
        api.RegisterBlockClass("FoodShelves.BlockCeilingRack", typeof(BlockCeilingRack));
        api.RegisterBlockClass("FoodShelves.BlockJarStand", typeof(BlockJarStand));

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
        api.RegisterBlockEntityClass("FoodShelves.BEMushroomBasket", typeof(BEMushroomBasket));
        api.RegisterBlockEntityClass("FoodShelves.BEVegetableBasket", typeof(BEVegetableBasket));

        // Coolers
        api.RegisterBlockEntityClass("FoodShelves.BECoolingCabinet", typeof(BECoolingCabinet));
        api.RegisterBlockEntityClass("FoodShelves.BEFruitCooler", typeof(BEFruitCooler));
        api.RegisterBlockEntityClass("FoodShelves.BEMeatFreezer", typeof(BEMeatFreezer));

        // Glassware
        api.RegisterBlockEntityClass("FoodShelves.BEFoodDisplayBlock", typeof(BEFoodDisplayBlock));
        api.RegisterBlockEntityClass("FoodShelves.BEFoodDisplayCase", typeof(BEFoodDisplayCase));
        api.RegisterBlockEntityClass("FoodShelves.BEJar", typeof(BEJar));
        api.RegisterBlockEntityClass("FoodShelves.BEJarLarge", typeof(BEJarLarge));
        api.RegisterBlockEntityClass("FoodShelves.BESeedBins", typeof(BESeedBins));
        api.RegisterBlockEntityClass("FoodShelves.BEWallCabinet", typeof(BEWallCabinet));

        // Other
        api.RegisterBlockEntityClass("FoodShelves.BEBucketHook", typeof(BEBucketHook));
        api.RegisterBlockEntityClass("FoodShelves.BECeilingRack", typeof(BECeilingRack));
        api.RegisterBlockEntityClass("FoodShelves.BEFlourSack", typeof(BEFlourSack));
        api.RegisterBlockEntityClass("FoodShelves.BEJarStand", typeof(BEJarStand));
        api.RegisterBlockEntityClass("FoodShelves.BEPumpkinCase", typeof(BEPumpkinCase));
        api.RegisterBlockEntityClass("FoodShelves.BETableWShelf", typeof(BETableWShelf));

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

    /// <summary>
    /// Registers all BlockBehavior classes for Food Shelves.
    /// </summary>
    public static void RegisterBlockBehaviors(ICoreAPI api) {
        api.RegisterBlockBehaviorClass("FoodShelves.ShiftRightClickPickup", typeof(BlockBehaviorShiftRightClickPickup));
    }
}
