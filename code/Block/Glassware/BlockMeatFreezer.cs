using System.Linq;

namespace FoodShelves;

public class BlockMeatFreezer : BaseFSContainer, IMultiBlockColSelBoxes {
    private WorldInteraction[] freezerInteractions;
    private WorldInteraction[] drawerInteractions;
    private WorldInteraction[] drawerOpenClose;

    public readonly AssetLocation soundFreezerOpen = new(SoundReferences.CoolingCabinetOpen);
    public readonly AssetLocation soundFreezerClose = new(SoundReferences.CoolingCabinetClose);
    public readonly AssetLocation soundDrawerOpen = new(SoundReferences.IceDrawerOpen);
    public readonly AssetLocation soundDrawerClose = new(SoundReferences.IceDrawerClose);

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
                if (world.BlockAccessor.GetBlockEntity(selection.Position) is BEMeatFreezer bemf && bemf.DrawerOpen) {
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

    #region MBColSelBoxes

    // Selection box for master block
    public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos) {
        BEMeatFreezer be = blockAccessor.GetBlockEntityExt<BEMeatFreezer>(pos);

        Cuboidf freezerBase = base.GetSelectionBoxes(blockAccessor, pos).ElementAt(6).Clone();
        Cuboidf skip = new(); // Skip selectionBox, to keep consistency between selectionBox indexes (0-3-sections 4-door 5-drawer 6-freezer)

        if (be != null) {
            if (be.FreezerOpen) {
                Cuboidf section1 = base.GetSelectionBoxes(blockAccessor, pos).ElementAt(0).Clone();
                Cuboidf section2 = base.GetSelectionBoxes(blockAccessor, pos).ElementAt(1).Clone();

                return [section1, section2, skip, skip, skip, skip, freezerBase];
            }

            return [skip, skip, skip, skip, skip, skip, freezerBase,];
        }

        return base.GetSelectionBoxes(blockAccessor, pos);
    }

    // Selection boxes for multiblock parts
    public Cuboidf[] MBGetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset) {
        // Selection Box indexes:
        // Freezer - 6
        // Drawer - 5
        // Door - 4
        // Sections - 0 - 3

        BEMeatFreezer be = blockAccessor.GetBlockEntityExt<BEMeatFreezer>(pos);
        if (be != null) {
            Cuboidf freezerDoor = base.GetSelectionBoxes(blockAccessor, pos).ElementAt(4).Clone();
            Cuboidf drawerSelBox = base.GetSelectionBoxes(blockAccessor, pos).ElementAt(5).Clone();
            Cuboidf freezerBase = base.GetSelectionBoxes(blockAccessor, pos).ElementAt(6).Clone();
            Cuboidf skip = new(); // Skip selectionBox, to keep consistency between selectionBox indexes (0-3-sections 4-door 5-drawer 6-freezer)

            freezerBase.MBNormalizeSelectionBox(offset);
            freezerDoor.MBNormalizeSelectionBox(offset);
            drawerSelBox.MBNormalizeSelectionBox(offset);

            if (be.FreezerOpen) {
                freezerDoor.Y1 += 0.1325f;
                freezerDoor.Y2 += 0.7f;
                
                int rotAngle = this.GetRotationAngle();

                switch (rotAngle) {
                    case 0: freezerDoor.Z2 -= 0.7f; break;
                    case 90: freezerDoor.X2 -= 0.7f; break;
                    case 180: freezerDoor.Z1 += 0.7f; break;
                    case 270: freezerDoor.X1 += 0.7f; break;
                }
            }

            if (be.DrawerOpen) {
                int rotAngle = this.GetRotationAngle();

                switch (rotAngle) {
                    case 0: drawerSelBox.Z2 += .5575f; break;
                    case 90: drawerSelBox.X2 += .5575f; break;
                    case 180: drawerSelBox.Z1 -= .5575f; break;
                    case 270: drawerSelBox.X1 -= .5575f; break;
                }
            }

            if (!be.FreezerOpen) {
                return [skip, skip, skip, skip, freezerDoor, drawerSelBox, freezerBase];
            }
            else {                
                List<Cuboidf> sBs = [];

                for (int i = 0; i < 4; i++) {
                    sBs.Add(base.GetSelectionBoxes(blockAccessor, pos).ElementAt(i).Clone());
                    sBs[i].MBNormalizeSelectionBox(offset);
                }
                sBs.Add(drawerSelBox);

                if (offset.Y != 0) {
                    return [sBs[0], sBs[1], sBs[2], sBs[3], freezerDoor, skip, skip];
                }

                return [skip, skip, skip, skip, skip, drawerSelBox, freezerBase];
            }
        }

        return base.GetSelectionBoxes(blockAccessor, pos);
    }

    public Cuboidf[] MBGetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset) {
        return base.GetCollisionBoxes(blockAccessor, pos);
    }

    #endregion
}