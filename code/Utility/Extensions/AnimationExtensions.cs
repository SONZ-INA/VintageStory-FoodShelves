namespace FoodShelves;

public static class AnimationExtensions {
    /// <summary>
    /// Start the block animation if it's not active.
    /// </summary>
    public static void TryStartAnimation(this BlockEntityAnimationUtil? animUtil, string code, float speed = 1f) {
        if (animUtil?.activeAnimationsByAnimCode.ContainsKey(code) == false) {
            animUtil.StartAnimation(new AnimationMetaData {
                Animation = code,
                Code = code,
                AnimationSpeed = speed,
                EaseOutSpeed = 1,
                EaseInSpeed = 2
            });
        }
    }

    /// <summary>
    /// Stop the block animation if it's active.
    /// </summary>
    public static void TryStopAnimation(this BlockEntityAnimationUtil? animUtil, string code) {
        if (animUtil?.activeAnimationsByAnimCode.ContainsKey(code) == true) {
            animUtil.StopAnimation(code);
        }
    }
}
