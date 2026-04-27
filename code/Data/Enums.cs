namespace FoodShelves;

public static class Enums {
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

    public enum TransitionDisplayMode {
        Percentage,
        TimeLeft
    }
}
