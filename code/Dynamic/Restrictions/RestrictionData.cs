﻿namespace FoodShelves; 

public class RestrictionData {
    #region Shelves

    public class PieShelfData {
        public string[] PieShelfTypes { get; set; }
        public string[] PieShelfCodes { get; set; }
    }

    public class BreadShelfData {
        public string[] BreadShelfTypes { get; set; }
        public string[] BreadShelfCodes { get; set; }
    }

    public class BarShelfData {
        public string[] BarShelfTypes { get; set; }
        public string[] BarShelfCodes { get; set; }
    }

    public class SushiShelfData {
        public string[] SushiShelfTypes { get; set; }
        public string[] SushiShelfCodes { get; set; }
    }

    public class SeedShelfData {
        public string[] SeedShelfTypes { get; set; }
        public string[] SeedShelfCodes { get; set; }
    }

    public class EggShelfData {
        public string[] EggShelfTypes { get; set; }
        public string[] EggShelfCodes { get; set; }
    }

    #endregion

    #region Baskets

    public class FruitBasketData {
        public string[] FruitBasketTypes { get; set; }
        public string[] FruitBasketCodes { get; set; }
    }

    public class VegetableBasketData {
        public string[] VegetableBasketTypes { get; set; }
        public string[] VegetableBasketCodes { get; set; }
    }

    #endregion

    #region Barrels

    public class BarrelRackData {
        public string[] BarrelRackTypes { get; set; }
        public string[] BarrelRackCodes { get; set; }
    }

    public class BarrelRackBigData {
        public string[] BarrelRackBigTypes { get; set; }
        public string[] BarrelRackBigCodes { get; set; }
    }

    #endregion
}
