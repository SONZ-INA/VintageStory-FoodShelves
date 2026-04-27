namespace FoodShelves;

/// <summary>
/// A registry that maps <see cref="ItemStack"/> wildcards to <see cref="ICollectibleLayout"/> logic.
/// </summary>
public static class LayoutRegistry {
    private static readonly ICollectibleLayout Default = new DefaultLayout();
    private static readonly ICollectibleLayout Large = new LargeLayout();
    private static readonly ICollectibleLayout Small = new SmallLayout();

    private static readonly List<LayoutRegistration> Registrations = [];

    private class LayoutRegistration(ICollectibleLayout layout, string[] wildcards) {
        public ICollectibleLayout Layout { get; } = layout;
        public string[] Wildcards { get; } = wildcards;
    }

    static LayoutRegistry() {
        Register(new PieLayout(), "game:pie-*");
        Register(new CheeseLayout(), "game:cheese-*");
        Register(new PackLayout(), "pemmican:*-pack");
    }

    private static void Register(ICollectibleLayout layout, params string[] wildcards) {
        Registrations.Add(new LayoutRegistration(layout, wildcards));
    }

    public static ICollectibleLayout GetLayout(ItemStack? stack) {
        if (stack == null) return Default;

        if (stack.IsLargeItem()) return Large;
        if (stack.IsSmallItem()) return Small;

        AssetLocation code = stack.Collectible.Code;
        foreach (var reg in Registrations) {
            foreach (var wildcard in reg.Wildcards) {
                if (WildcardUtil.Match(wildcard, code.ToString())) {
                    return reg.Layout;
                }
            }
        }

        return Default;
    }
}
