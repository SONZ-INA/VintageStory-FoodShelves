namespace FoodShelves;

public static class Patches {
    #region Generic

    public static void PatchFoodUniversal(CollectibleObject obj, RestrictionData data) {
        if (obj.CheckTypedRestriction(data) || WildcardUtil.Match(data.CollectibleCodes, obj.Code.ToString())) {
            obj.EnsureAttributesNotNull();
            obj.Attributes.Token[FoodUniversal] = JToken.FromObject(true);
        }

        ModelTransform transformation = obj.GetTransformation(FoodUniversalTransformations);
        if (transformation != null) {
            obj.Attributes.Token[onGlassFoodBlockTransform] = JToken.FromObject(transformation);
            obj.Attributes.Token[onGlassFoodCaseTransform] = JToken.FromObject(transformation);
        }
    }

    public static void PatchHolderUniversal(CollectibleObject obj, RestrictionData data) {
        if (obj.CheckTypedRestriction(data) || WildcardUtil.Match(data.CollectibleCodes, obj.Code.ToString())) {
            obj.EnsureAttributesNotNull();
            obj.Attributes.Token[HolderUniversal] = JToken.FromObject(true);
        }

        ModelTransform transformation = obj.GetTransformation(HolderUniversalTransformations);
        if (transformation != null) {
            obj.Attributes.Token[onCoolingCabinetTransform] = JToken.FromObject(transformation);
        }
    }

    public static void PatchLiquidyStuff(CollectibleObject obj, RestrictionData data) {
        if (obj.CheckTypedRestriction(data) || WildcardUtil.Match(data.CollectibleCodes, obj.Code.ToString())) {
            obj.EnsureAttributesNotNull();
            obj.Attributes.Token[LiquidyStuff] = JToken.FromObject(true);
        }
    }

    public static void PatchCoolingOnly(CollectibleObject obj, RestrictionData data) {
        if (obj.CheckTypedRestriction(data) || WildcardUtil.Match(data.CollectibleCodes, obj.Code.ToString())) {
            obj.EnsureAttributesNotNull();
            obj.Attributes.Token[CoolingOnly] = JToken.FromObject(true);
        }
    }

    #endregion

    #region Shelves

    public static void PatchPieShelf(CollectibleObject obj, RestrictionData data) {
        if (obj.CheckTypedRestriction(data) || WildcardUtil.Match(data.CollectibleCodes, obj.Code.ToString())) {
            obj.EnsureAttributesNotNull();
            obj.Attributes.Token["cPieShelf"] = JToken.FromObject(true);

            ModelTransform transformation = obj.GetTransformation(PieShelfTransformations);
            if (transformation != null) {
                obj.Attributes.Token[onPieShelfTransform] = JToken.FromObject(transformation);
            }
        }
    }

    public static void PatchBreadShelf(CollectibleObject obj, RestrictionData data) {
        if (obj.CheckTypedRestriction(data) || WildcardUtil.Match(data.CollectibleCodes, obj.Code.ToString())) {
            obj.EnsureAttributesNotNull();
            obj.Attributes.Token[BreadShelf] = JToken.FromObject(true);

            ModelTransform transformation = obj.GetTransformation(BreadShelfTransformations);
            if (transformation != null) {
                obj.Attributes.Token[onBreadShelfTransform] = JToken.FromObject(transformation);
            }
        }
    }

    public static void PatchBarShelf(CollectibleObject obj, RestrictionData data) {
        if (obj.CheckTypedRestriction(data) || WildcardUtil.Match(data.CollectibleCodes, obj.Code.ToString())) {
            obj.EnsureAttributesNotNull();
            obj.Attributes.Token[BarShelf] = JToken.FromObject(true);

            ModelTransform transformation = obj.GetTransformation(BarShelfTransformations);
            if (transformation != null) {
                obj.Attributes.Token[onBarShelfTransform] = JToken.FromObject(transformation);
            }
        }
    }

    public static void PatchSushiShelf(CollectibleObject obj, RestrictionData data) {
        if (obj.CheckTypedRestriction(data) || WildcardUtil.Match(data.CollectibleCodes, obj.Code.ToString())) {
            obj.EnsureAttributesNotNull();
            obj.Attributes.Token[SushiShelf] = JToken.FromObject(true);
        }
    }

    public static void PatchEggShelf(CollectibleObject obj, RestrictionData data) {
        if (obj.CheckTypedRestriction(data) || WildcardUtil.Match(data.CollectibleCodes, obj.Code.ToString())) {
            obj.EnsureAttributesNotNull();
            obj.Attributes.Token[EggShelf] = JToken.FromObject(true);
        }
    }

    public static void PatchSeedShelf(CollectibleObject obj, RestrictionData data) {
        if (obj.CheckTypedRestriction(data) || WildcardUtil.Match(data.CollectibleCodes, obj.Code.ToString())) {
            obj.EnsureAttributesNotNull();
            obj.Attributes.Token[SeedShelf] = JToken.FromObject(true);
        }
    }

    public static void PatchGlassJarShelf(CollectibleObject obj, RestrictionData data) {
        if (obj.CheckTypedRestriction(data) || WildcardUtil.Match(data.CollectibleCodes, obj.Code.ToString())) {
            obj.EnsureAttributesNotNull();
            obj.Attributes.Token[GlassJarShelf] = JToken.FromObject(true);
        }
    }

    #endregion

    #region Baskets

    public static void PatchFruitBasket(CollectibleObject obj, RestrictionData data) {
        if (obj.CheckTypedRestriction(data) || WildcardUtil.Match(data.CollectibleCodes, obj.Code.ToString())) {
            obj.EnsureAttributesNotNull();
            obj.Attributes.Token[FruitBasket] = JToken.FromObject(true);

            ModelTransform transformation = obj.GetTransformation(FruitBasketTransformations);
            if (transformation != null) {
                obj.Attributes.Token[onFruitBasketTransform] = JToken.FromObject(transformation);
            }
        }
    }

    public static void PatchVegetableBasket(CollectibleObject obj, RestrictionData data) {
        bool passedByGrouping = false;

        foreach (var group in data.GroupingCodes.Values) {
            foreach (var code in group) {
                if (WildcardUtil.Match(code, obj.Code.ToString())) {
                    passedByGrouping = true;
                    break;
                }
            }

            if (passedByGrouping) break;
        }

        if (passedByGrouping) {
            obj.EnsureAttributesNotNull();
            obj.Attributes.Token[VegetableBasket] = JToken.FromObject(true);

            ModelTransform transformation = obj.GetTransformation(VegetableBasketTransformations);
            if (transformation != null) {
                obj.Attributes.Token[onVegetableBasketTransform] = JToken.FromObject(transformation);
            }
        }
    }

    public static void PatchEggBasket(CollectibleObject obj, RestrictionData data) {
        if (obj.CheckTypedRestriction(data) || WildcardUtil.Match(data.CollectibleCodes, obj.Code.ToString())) {
            obj.EnsureAttributesNotNull();
            obj.Attributes.Token[EggBasket] = JToken.FromObject(true);
        }
    }

    #endregion

    #region Barrels

    public static void PatchBarrelRack(CollectibleObject obj, RestrictionData data) {
        if (obj.CheckTypedRestriction(data) || WildcardUtil.Match(data.CollectibleCodes, obj.Code.ToString())) {
            obj.EnsureAttributesNotNull();
            obj.Attributes.Token[BarrelRack] = JToken.FromObject(true);
        }
    }

    public static void PatchBarrelRackBig(CollectibleObject obj, RestrictionData data) {
        if (obj.CheckTypedRestriction(data) || WildcardUtil.Match(data.CollectibleCodes, obj.Code.ToString())) {
            obj.EnsureAttributesNotNull();
            obj.Attributes.Token[BarrelRackBig] = JToken.FromObject(true);
        }
    }

    //public static void PatchFirkinRack(CollectibleObject obj, RestrictionData data) {
    //    if (obj.CheckTypedRestriction(data) || WildcardUtil.Match(data.CollectibleCodes, obj.Code.Path.ToString())) {
    //        obj.EnsureAttributesNotNull();
    //        obj.Attributes.Token[FirkinRack] = JToken.FromObject(true);
    //    }
    //}

    #endregion

    public static void PatchPumpkinCase(CollectibleObject obj, RestrictionData data) {
        if (obj.CheckTypedRestriction(data) || WildcardUtil.Match(data.CollectibleCodes, obj.Code.ToString())) {
            obj.EnsureAttributesNotNull();
            obj.Attributes.Token[PumpkinCase] = JToken.FromObject(true);
        }
    }
}