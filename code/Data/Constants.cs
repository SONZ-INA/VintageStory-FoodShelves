namespace FoodShelves;

public static class Constants {
    public const string FSAttributes = "FSAttributes";
    public const string FSCoolingOnly = "fsCoolingOnly";
    public const string FSGroup = "fsGroup";
    public const string FSSkipDefault = "fsSkipDefault";

    public enum BlockDirection {
        North = 0,
        West = 90,
        South = 180,
        East = 270,
    }

    public enum BlockHintType {
        None,
        SingleSlot,
        Bulk
    }
}
