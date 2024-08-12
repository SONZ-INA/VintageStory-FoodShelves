﻿using static FoodShelves.Patches;

[assembly: ModInfo(name: "Food Shelves", modID: "foodshelves")]

namespace FoodShelves;

public class Core : ModSystem {
    public override void Start(ICoreAPI api) {
        base.Start(api);

        api.RegisterBlockClass("FoodShelves.BlockPieShelf", typeof(BlockPieShelf));
        api.RegisterBlockEntityClass("FoodShelves.BlockEntityPieShelf", typeof(BlockEntityPieShelf));

        api.RegisterBlockClass("FoodShelves.BlockBreadShelf", typeof(BlockBreadShelf));
        api.RegisterBlockEntityClass("FoodShelves.BlockEntityBreadShelf", typeof(BlockEntityBreadShelf));

        api.RegisterBlockClass("FoodShelves.BlockBarShelf", typeof(BlockBarShelf));
        api.RegisterBlockEntityClass("FoodShelves.BlockEntityBarShelf", typeof(BlockEntityBarShelf));

        api.RegisterBlockClass("FoodShelves.BlockSushiShelf", typeof(BlockSushiShelf));
        api.RegisterBlockEntityClass("FoodShelves.BlockEntitySushiShelf", typeof(BlockEntitySushiShelf));

        api.RegisterBlockClass("FoodShelves.BlockEggShelf", typeof(BlockEggShelf));
        api.RegisterBlockEntityClass("FoodShelves.BlockEntityEggShelf", typeof(BlockEntityEggShelf));

        api.RegisterBlockClass("FoodShelves.BlockSeedShelf", typeof(BlockSeedShelf));
        api.RegisterBlockEntityClass("FoodShelves.BlockEntitySeedShelf", typeof(BlockEntitySeedShelf));

        api.RegisterBlockClass("FoodShelves.BlockTableWShelf", typeof(BlockTableWShelf));
        api.RegisterBlockEntityClass("FoodShelves.BlockEntityTableWShelf", typeof(BlockEntityTableWShelf));

        api.RegisterBlockClass("FoodShelves.BlockFruitBasket", typeof(BlockFruitBasket));
        api.RegisterBlockEntityClass("FoodShelves.BlockEntityFruitBasket", typeof(BlockEntityFruitBasket));

        api.RegisterBlockClass("FoodShelves.BlockBarrelRack", typeof(BlockBarrelRack));
        api.RegisterBlockEntityClass("FoodShelves.BlockEntityBarrelRack", typeof(BlockEntityBarrelRack));

        api.RegisterBlockClass("FoodShelves.BlockVegetableBasket", typeof(BlockVegetableBasket));
        api.RegisterBlockEntityClass("FoodShelves.BlockEntityVegetableBasket", typeof(BlockEntityVegetableBasket));

        //api.RegisterBlockClass("FoodShelves.BlockBarrelRackBig", typeof(BlockBarrelRackBig));
        //api.RegisterBlockEntityClass("FoodShelves.BlockEntityBarrelRackBig", typeof(BlockEntityBarrelRackBig));

        //api.RegisterBlockClass("FoodShelves.BlockHorizontalBarrelBig", typeof(BlockHorizontalBarrelBig));
    }

    public override void AssetsFinalize(ICoreAPI api) {
        base.AssetsFinalize(api);

        foreach (CollectibleObject obj in api.World.Collectibles) {
            PatchPieShelf(obj);
            PatchBreadShelf(obj);
            PatchBarShelf(obj);
            PatchSushiShelf(obj);
            PatchEggShelf(obj);
            PatchFruitBasket(obj);
            PatchSeedShelf(obj);
            PatchHorizontalBarrelRack(obj);
            //PatchHorizontalBarrelRackBig(obj);
            PatchVegetableBasket(obj);
        }
    }
}
