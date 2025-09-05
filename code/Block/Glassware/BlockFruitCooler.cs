using System.Linq;

namespace FoodShelves;

public class BlockFruitCooler : BaseFSContainer {
    private WorldInteraction[] freezerInteractions;
    private WorldInteraction[] drawerInteractions;
    private WorldInteraction[] drawerOpenClose;

    public readonly AssetLocation soundCoolerOpen = new(SoundReferences.FruitCoolerOpen);
    public readonly AssetLocation soundCoolerClose = new(SoundReferences.FruitCoolerClose);
    public readonly AssetLocation soundDrawerOpen = new(SoundReferences.FruitDrawerOpen);
    public readonly AssetLocation soundDrawerClose = new(SoundReferences.FruitDrawerClose);

    public override void OnLoaded(ICoreAPI api) {
        base.OnLoaded(api);

        freezerInteractions = ObjectCacheUtil.GetOrCreate(api, "fruitCoolerDoorInteractions", () => {
            return new WorldInteraction[] {
                new() {
                    ActionLangCode = "blockhelp-door-openclose",
                    MouseButton = EnumMouseButton.Right
                }
            };
        });

        drawerOpenClose = ObjectCacheUtil.GetOrCreate(api, "fruitCoolerDrawerOpenClose", () => {
            return new WorldInteraction[] {
                new() {
                    ActionLangCode = "blockhelp-door-openclose",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "shift"
                }
            };
        });

        drawerInteractions = ObjectCacheUtil.GetOrCreate(api, "fruitCoolerDrawerInteractions", () => {
            List<ItemStack> coolingOnlyStackList = [];

            foreach (var obj in api.World.Collectibles) {
                if (obj.CanStoreInSlot("fsCoolingOnly")) {
                    coolingOnlyStackList.Add(new ItemStack(obj));
                }
            }

            return new WorldInteraction[] {
                new() {
                    ActionLangCode = "blockhelp-groundstorage-addone",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = [.. coolingOnlyStackList]
                },
                new() {
                    ActionLangCode = "blockhelp-groundstorage-addbulk",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = [.. coolingOnlyStackList],
                    HotKeyCode = "ctrl",
                }
            };
        });
    }

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer) {
        switch (selection.SelectionBoxIndex) {
            case 0: case 1: case 2: case 3:
                return itemSlottableInteractions.Append(BaseGetPlacedBlockInteractionHelp(world, selection, forPlayer));
            case 4:
                return freezerInteractions.Append(BaseGetPlacedBlockInteractionHelp(world, selection, forPlayer));
            case 5:
                if (world.BlockAccessor.GetBlockEntity(selection.Position) is BEFruitCooler bemf && bemf.DrawerOpen) {
                    if (bemf.Inventory?[bemf.cutIceSlot].Empty == true || bemf.Inventory?[bemf.cutIceSlot].CanStoreInSlot("fsCoolingOnly") == true) {
                        return drawerOpenClose.Append(drawerInteractions.Append(BaseGetPlacedBlockInteractionHelp(world, selection, forPlayer)));
                    }
                }
                else {
                    return drawerOpenClose;
                }
                break;
        }

        return null;
    }

    public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos) {
        // Selection Box indexes:
        // Cooler - 6
        // Drawer - 5
        // Door - 4
        // Sections - 0 - 3
        BEFruitCooler be = blockAccessor.GetBlockEntityExt<BEFruitCooler>(pos);

        List<Cuboidf> sections = [];
        Cuboidf skip = new(); // Skip selectionBox, to keep consistency between selectionBox indexes (0-3-sections 4-door 5-drawer 6-cooler)

        for (int i = 0; i < 7; i++) {
            sections.Add(base.GetSelectionBoxes(blockAccessor, pos).ElementAt(i).Clone());
        }

        if (be != null) {
            if (be.CoolerOpen) {
                int rotAngle = this.GetRotationAngle();

                switch (rotAngle) {
                    case 0:
                        sections[4].Z2 += 0.225f;
                        sections[4].Z1 += 0.835f;
                        break;

                    case 90:
                        sections[4].X2 += 0.225f;
                        sections[4].X1 += 0.835f;
                        break;

                    case 180:
                        sections[4].Z2 -= 0.835f;
                        sections[4].Z1 -= 0.225f;
                        break;

                    case 270:
                        sections[4].X2 -= 0.835f;
                        sections[4].X1 -= 0.225f;
                        break;
                }

                sections[4].Y1 -= 0.75f;
                sections[4].Y2 -= 0.05f;
                sections[6].Y2 -= 0.1875f;

                return [sections[0], sections[1], sections[2], sections[3], sections[4], skip, sections[6]];
            }

            return [skip, skip, skip, skip, sections[4], sections[5], sections[6]];
        }

        return base.GetSelectionBoxes(blockAccessor, pos);
    }
}