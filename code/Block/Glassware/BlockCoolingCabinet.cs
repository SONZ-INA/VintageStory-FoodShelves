namespace FoodShelves;

public class BlockCoolingCabinet : BaseFSContainer, IMultiBlockColSelBoxes {
    private WorldInteraction[]? cabinetInteractions;
    private WorldInteraction[]? drawerInteractions;

    private static readonly Cuboidf Skip = new(); // Skip selectionBox, to keep consistency between selectionBox indexes (1-8-shelves 9-drawer 10-cabinet)

    public override void OnLoaded(ICoreAPI api) {
        base.OnLoaded(api);

        itemSlottableInteractions = ObjectCacheUtil.GetOrCreate(api, "coolingCabinetItemInteractions", () => {
            List<ItemStack> holderUniversalStackList = [];

            foreach (var obj in api.World.Collectibles) {
                if (obj.CanStoreInSlot("fsHolderUniversal")) {
                    holderUniversalStackList.Add(new ItemStack(obj));
                }
            }

            return new WorldInteraction[] {
                new() {
                    ActionLangCode = "blockhelp-groundstorage-add",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = [.. holderUniversalStackList]
                },
                new() {
                    ActionLangCode = "blockhelp-groundstorage-remove",
                    MouseButton = EnumMouseButton.Right,
                }
            };
        });

        cabinetInteractions = ObjectCacheUtil.GetOrCreate(api, "coolingCabinetCabinetInteractions", () => {
            return new WorldInteraction[] {
                new() {
                    ActionLangCode = "blockhelp-door-openclose",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "shift",
                }
            };
        });

        drawerInteractions = ObjectCacheUtil.GetOrCreate(api, "coolingCabinetDrawerInteractions", () => {
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
        if (world.BlockAccessor.GetBlockEntity(selection.Position) is BECoolingCabinet becc) {
            if (selection.SelectionBoxIndex < 9) {
                if (becc.DoorOpen) {
                    return cabinetInteractions.Append(itemSlottableInteractions);
                }
            }

            if (selection.SelectionBoxIndex == 9) {
                if (becc.DrawerOpen) {
                    return cabinetInteractions.Append(drawerInteractions);
                }
                else {
                    return cabinetInteractions;
                }
            }
        }

        return cabinetInteractions;
    }

    #region MBColSelBoxes

    // Selection box for master block
    public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos) {
        BECoolingCabinet? be = blockAccessor.GetBlockEntityExt<BECoolingCabinet>(pos);
        var boxes = base.GetSelectionBoxes(blockAccessor, pos);

        if (be == null) return boxes;

        Cuboidf drawerSelBox = boxes[9].Clone();
        Cuboidf cabinetSelBox = boxes[10].Clone();

        if (be.DrawerOpen) {
            int rotAngle = this.GetRotationAngle();

            switch (rotAngle) {
                case 0: drawerSelBox.Z2 += .3125f; break;
                case 90: drawerSelBox.X2 += .3125f; break;
                case 180: drawerSelBox.Z1 -= .3125f; break;
                case 270: drawerSelBox.X1 -= .3125f; break;
            }
        }

        if (be.DoorOpen) {
            Cuboidf bottomShelfL = boxes[0].Clone();
            Cuboidf bottomShelfM = boxes[1].Clone();

            return [bottomShelfL, bottomShelfM, Skip, Skip, Skip, Skip, Skip, Skip, Skip, drawerSelBox];
        }

        return [Skip, Skip, Skip, Skip, Skip, Skip, Skip, Skip, Skip, drawerSelBox, cabinetSelBox];
    }

    // Selection boxes for multiblock parts
    public Cuboidf[] MBGetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset) {
        // Selection Box indexes:
        // Drawer - 9 / Cabinet - 10
        // Top    - 6 7 8
        // Middle - 3 4 5
        // Bottom - 0 1 2

        BECoolingCabinet? be = blockAccessor.GetBlockEntityExt<BECoolingCabinet>(pos);
        var boxes = base.GetSelectionBoxes(blockAccessor, pos);

        if (be == null) return boxes;

        Cuboidf drawerSelBox = boxes[9].Clone();

        drawerSelBox.MBNormalizeSelectionBox(offset);

        if (be.DrawerOpen) {
            int rotAngle = this.GetRotationAngle();

            switch (rotAngle) {
                case 0: drawerSelBox.Z2 += .3125f; break;
                case 90: drawerSelBox.X2 += .3125f; break;
                case 180: drawerSelBox.Z1 -= .3125f; break;
                case 270: drawerSelBox.X1 -= .3125f; break;
            }
        }

        if (!be.DoorOpen) {
            Cuboidf cabinetSelBox = boxes[10].Clone();
            cabinetSelBox.MBNormalizeSelectionBox(offset);

            return [Skip, Skip, Skip, Skip, Skip, Skip, Skip, Skip, Skip, drawerSelBox, cabinetSelBox];
        }
        else {
            List<Cuboidf> sBs = [];

            for (int i = 0; i < 9; i++) {
                sBs.Add(boxes[i].Clone());
                sBs[i].MBNormalizeSelectionBox(offset);
            }
            sBs.Add(drawerSelBox);

            if (offset.Y != 0) {
                return [Skip, Skip, Skip, sBs[3], sBs[4], sBs[5], sBs[6], sBs[7], sBs[8]];
            }

            return [Skip, sBs[1], sBs[2], Skip, Skip, Skip, Skip, Skip, Skip, sBs[9]];
        }
    }

    public Cuboidf[] MBGetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset) {
        return base.GetCollisionBoxes(blockAccessor, pos);
    }

    #endregion
}