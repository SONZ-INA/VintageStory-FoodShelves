namespace FoodShelves;

public class BlockFruitCooler : BaseFSContainer {
    private WorldInteraction[]? freezerInteractions;
    private WorldInteraction[]? drawerInteractions;
    private WorldInteraction[]? drawerOpenClose;

    private static readonly Cuboidf Skip = new(); // Skip selectionBox, to keep consistency between selectionBox indexes (0-3-sections 4-door 5-drawer 6-cooler)

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
                if (obj.CanStoreInSlot(FSCoolingOnly)) {
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

    public override WorldInteraction[]? GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer) {
        var baseHelp = BaseGetPlacedBlockInteractionHelp(world, selection, forPlayer);

        switch (selection.SelectionBoxIndex) {
            case 0 or 1 or 2 or 3:
                return itemSlottableInteractions.Append(baseHelp);

            case 4:
                return freezerInteractions.Append(baseHelp);

            case 5:
                if (world.BlockAccessor.GetBlockEntity(selection.Position)
                    is not BEFruitCooler be) {
                    return drawerOpenClose;
                }

                return be.DrawerOpen
                    ? drawerOpenClose.Append(drawerInteractions.Append(baseHelp))
                    : drawerOpenClose;
        }

        return null;
    }

    public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos) {
        // Selection Box indexes:
        // Cooler - 6
        // Drawer - 5
        // Door - 4
        // Sections - 0 - 3

        BEFruitCooler? be = blockAccessor.GetBlockEntityExt<BEFruitCooler>(pos);
        var boxes = base.GetSelectionBoxes(blockAccessor, pos);

        if (be == null) return boxes;

        Cuboidf[] sections = new Cuboidf[7];

        for (int i = 0; i < sections.Length; i++) {
            sections[i] = boxes[i].Clone();
        }

        if (be.DoorOpen) {
            OffsetDoorBox(sections[4]);
            sections[6].Y2 -= 0.1875f;

            return [sections[0], sections[1], sections[2], sections[3], sections[4], Skip, sections[6]];
        }

        return [Skip, Skip, Skip, Skip, sections[4], sections[5], sections[6]];
    }

    private void OffsetDoorBox(Cuboidf box) {
        switch ((BlockDirection)this.GetRotationAngle()) {
            case BlockDirection.North:
                box.Z2 += 0.225f;
                box.Z1 += 0.835f;
                break;

            case BlockDirection.West:
                box.X2 += 0.225f;
                box.X1 += 0.835f;
                break;

            case BlockDirection.South:
                box.Z2 -= 0.835f;
                box.Z1 -= 0.225f;
                break;

            case BlockDirection.East:
                box.X2 -= 0.835f;
                box.X1 -= 0.225f;
                break;
        }

        box.Y1 -= 0.75f;
        box.Y2 -= 0.05f;
    }
}