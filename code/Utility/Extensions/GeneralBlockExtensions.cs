using System.Text.Json;
using Vintagestory.ServerMods;

namespace FoodShelves;

public static class GeneralBlockExtensions {
    /// <summary>
    /// Retrieves the block entity of type <typeparamref name="T"/> at the given position.<br/>
    /// If the block at the position is a multiblock, it attempts to return the block entity at the master (origin) block instead.
    /// </summary>
    public static T GetBlockEntityExt<T>(this IBlockAccessor blockAccessor, BlockPos pos) where T : BlockEntity {
        if (blockAccessor.GetBlockEntity<T>(pos) is T blockEntity) {
            return blockEntity;
        }

        if (blockAccessor.GetBlock(pos) is BlockMultiblock multiblock) {
            BlockPos multiblockPos = new(pos.X + multiblock.OffsetInv.X, pos.Y + multiblock.OffsetInv.Y, pos.Z + multiblock.OffsetInv.Z, pos.dimension);
            return blockAccessor.GetBlockEntity<T>(multiblockPos);
        }

        return null;
    }

    /// <summary>
    /// Adjusts (normalizes) the selection box position by applying an offset, ensuring correct alignment for multiblock structures that occupy multiple block spaces.<br/>
    /// Requires that the selection boxes in the blocktype JSON are defined with standardized rotations:<br/>
    /// -- { "*-north": 0, "*-east": 270, "*-west": 90, "*-south": 180 }.<br/>
    /// This prevents incorrect selection box clipping when targeting multiblock parts other than the master block.
    /// </summary>
    public static void MBNormalizeSelectionBox(this Cuboidf selectionBox, Vec3i offset) {
        selectionBox.X1 += offset.X;
        selectionBox.X2 += offset.X;
        selectionBox.Y1 += offset.Y;
        selectionBox.Y2 += offset.Y;
        selectionBox.Z1 += offset.Z;
        selectionBox.Z2 += offset.Z;
    }

    /// <summary>
    /// Retrieves the localized material name from the item's FSAttributes.<br/>
    /// Returns an empty string if no relevant attribute is found.
    /// </summary>
    public static string GetMaterialNameLocalized(this ItemStack itemStack, bool includeParenthesis = true) {
        if (itemStack.Attributes["FSAttributes"] is not ITreeAttribute tree)
            return "";

        foreach (var pair in tree) {
            if (pair.Key == "wood") {
                string toReturn = Lang.Get("game:material-" + pair.Value);
                return (includeParenthesis ? "(" : "") + toReturn + (includeParenthesis ? ")" : "");
            }
        }

        return "";
    }

    /// <summary>
    /// Populates the block's creative inventory with variant item stacks based on its material properties and orientation.<br/>
    /// - Checks the block's horizontal orientation and only proceeds if the block's current variant side matches the drop side.<br/>
    /// - Loads material variants from world properties JSON files if available.<br/>
    /// - Adds a default block stack unless explicitly skipped via attributes.<br/>
    /// - For each material variant, creates a corresponding item stack with variant-specific attributes for display in the creative inventory.<br/>
    /// - Assigns the generated stacks to the block’s creative inventory tabs "general", "decorative", and "purposefulstorage".<br/>
    /// </summary>
    public static void LoadVariantsCreative(ICoreAPI api, Block block) {
        string blockSide = block.Variant["side"];
        string dropSide = "east";

        string properties = block.GetBehavior<BlockBehaviorHorizontalOrientable>()?.propertiesAtString;
        if (properties != null) {
            JsonDocument jsonDoc = JsonDocument.Parse(properties);
            dropSide = jsonDoc.RootElement.GetProperty("dropBlockFace").GetString() ?? "east";
        }

        if (blockSide != null && blockSide != dropSide) 
            return;

        var materials = block.Attributes["materials"].AsObject<RegistryObjectVariantGroup>();
        string material = "";
        StandardWorldProperty props = null;

        if (materials?.LoadFromProperties != null) {
            material = materials.LoadFromProperties.ToString().Split('/')[1];
            props = api.Assets.TryGet(materials.LoadFromProperties.WithPathPrefixOnce("worldproperties/").WithPathAppendixOnce(".json"))?.ToObject<StandardWorldProperty>();
        }

        var stacks = new List<JsonItemStack>();

        if (block.Attributes?["skipDefault"]?.AsBool() != true) {
            var defaultBlock = new JsonItemStack() {
                Code = block.Code,
                Type = EnumItemClass.Block,
                Attributes = new JsonObject(JToken.Parse("{}"))
            };

            defaultBlock.Resolve(api.World, block.Code);
            stacks.Add(defaultBlock);
        }

        if (props != null && material != "") {
            foreach (var prop in props.Variants) {
                var fsAttributes = new JObject {
                    [material] = prop.Code.Path
                };

                var attributes = new JObject {
                    ["FSAttributes"] = fsAttributes
                };

                var jstack = new JsonItemStack() {
                    Code = block.Code,
                    Type = EnumItemClass.Block,
                    Attributes = new JsonObject(attributes)
                };

                jstack.Resolve(api.World, block.Code);
                stacks.Add(jstack);
            }
        }

        block.CreativeInventoryStacks = [
            new() { Stacks = [.. stacks], Tabs = ["general", "decorative", "foodshelves"]}
        ];
    }
}

