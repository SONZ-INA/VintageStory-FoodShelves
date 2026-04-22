namespace FoodShelves;

public class BlockMeatFreezer : BaseFSContainer, IMultiBlockColSelBoxes {
    private WorldInteraction[]? freezerInteractions;
    private WorldInteraction[]? drawerInteractions;
    private WorldInteraction[]? drawerOpenClose;

    private static readonly Cuboidf Skip = new(); // Skip selectionBox, to keep consistency between selectionBox indexes (0-3-sections 4-door 5-drawer 6-freezer)

    public override void OnLoaded(ICoreAPI api) {
        base.OnLoaded(api);

        freezerInteractions = ObjectCacheUtil.GetOrCreate(api, "meatFreezerDoorInteractions", () => {
            return new WorldInteraction[] {
                new() {
                    ActionLangCode = "blockhelp-door-openclose",
                    MouseButton = EnumMouseButton.Right
                }
            };
        });

        drawerOpenClose = ObjectCacheUtil.GetOrCreate(api, "meatFreezerDrawerOpenClose", () => {
            return new WorldInteraction[] {
                new() {
                    ActionLangCode = "blockhelp-door-openclose",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "shift"
                }
            };
        });

        drawerInteractions = ObjectCacheUtil.GetOrCreate(api, "meatFreezerDrawerInteractions", () => {
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
        switch (selection.SelectionBoxIndex) {
            case 0: case 1: case 2: case 3:
                return itemSlottableInteractions.Append(BaseGetPlacedBlockInteractionHelp(world, selection, forPlayer));
            
            case 4:
                return freezerInteractions.Append(BaseGetPlacedBlockInteractionHelp(world, selection, forPlayer));
            
            case 5:
                if (world.BlockAccessor.GetBlockEntity(selection.Position) is BEMeatFreezer bemf) {
                    if (bemf.DrawerOpen) {
                        return drawerOpenClose.Append(drawerInteractions.Append(BaseGetPlacedBlockInteractionHelp(world, selection, forPlayer)));
                    }
                    else {
                        return drawerOpenClose;
                    }
                }
                break;
        }

        return null;
    }

    #region MBColSelBoxes

    // Selection box for master block
    public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos) {
        BEMeatFreezer? be = blockAccessor.GetBlockEntityExt<BEMeatFreezer>(pos);
        var boxes = base.GetSelectionBoxes(blockAccessor, pos);

        if (be == null) return boxes;

        Cuboidf freezerBase = boxes[6].Clone();

        if (be.DoorOpen) {
            Cuboidf section1 = boxes[0].Clone();
            Cuboidf section2 = boxes[1].Clone();

            return [section1, section2, Skip, Skip, Skip, Skip, freezerBase];
        }

        return [Skip, Skip, Skip, Skip, Skip, Skip, freezerBase];
    }

    // Selection boxes for multiblock parts
    public Cuboidf[] MBGetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset) {
        // Selection Box indexes:
        // Freezer - 6
        // Drawer - 5
        // Door - 4
        // Sections - 0 - 3

        BEMeatFreezer? be = blockAccessor.GetBlockEntityExt<BEMeatFreezer>(pos);
        var boxes = base.GetSelectionBoxes(blockAccessor, pos);

        if (be == null) return boxes;

        Cuboidf freezerDoor = boxes[4].Clone();
        Cuboidf drawerSelBox = boxes[5].Clone();
        Cuboidf freezerBase = boxes[6].Clone();

        freezerBase.MBNormalizeSelectionBox(offset);
        freezerDoor.MBNormalizeSelectionBox(offset);
        drawerSelBox.MBNormalizeSelectionBox(offset);

        if (be.DoorOpen) {
            freezerDoor.Y1 += 0.1325f;
            freezerDoor.Y2 += 0.7f;
                
            BlockDirection rotAngle = (BlockDirection)this.GetRotationAngle();

            switch (rotAngle) {
                case BlockDirection.North: freezerDoor.Z2 -= 0.7f; break;
                case BlockDirection.West: freezerDoor.X2 -= 0.7f; break;
                case BlockDirection.South: freezerDoor.Z1 += 0.7f; break;
                case BlockDirection.East: freezerDoor.X1 += 0.7f; break;
            }
        }

        if (be.DrawerOpen) {
            BlockDirection rotAngle = (BlockDirection)this.GetRotationAngle();

            switch (rotAngle) {
                case BlockDirection.North: drawerSelBox.Z2 += .5575f; break;
                case BlockDirection.West: drawerSelBox.X2 += .5575f; break;
                case BlockDirection.South: drawerSelBox.Z1 -= .5575f; break;
                case BlockDirection.East: drawerSelBox.X1 -= .5575f; break;
            }
        }

        if (!be.DoorOpen) {
            return [Skip, Skip, Skip, Skip, freezerDoor, drawerSelBox, freezerBase];
        }
        else {                
            List<Cuboidf> sBs = [];

            for (int i = 0; i < 4; i++) {
                sBs.Add(boxes[i].Clone());
                sBs[i].MBNormalizeSelectionBox(offset);
            }
            sBs.Add(drawerSelBox);

            if (offset.Y != 0) {
                return [sBs[0], sBs[1], sBs[2], sBs[3], freezerDoor, Skip, Skip];
            }

            return [Skip, Skip, Skip, Skip, Skip, drawerSelBox, freezerBase];
        }
    }

    public Cuboidf[] MBGetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset) {
        return base.GetCollisionBoxes(blockAccessor, pos);
    }

    #endregion
}