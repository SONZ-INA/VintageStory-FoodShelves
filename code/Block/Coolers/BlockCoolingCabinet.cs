namespace FoodShelves;

public class BlockCoolingCabinet : BaseFSContainer, IMultiBlockColSelBoxes {
    private WorldInteraction[]? doorOpenClose;
    private WorldInteraction[]? drawerOpenClose;
    private WorldInteraction[]? drawerInteractions;

    private static readonly Cuboidf Skip = new(); // Skip selectionBox, to keep consistency between selectionBox indexes (1-8-shelves 9-drawer 10-cabinet, 11-12-doors)

    public override void OnLoaded(ICoreAPI api) {
        base.OnLoaded(api);

        doorOpenClose = ObjectCacheUtil.GetOrCreate(api, "coolingCabinetCabinetInteractions", () => {
            return new WorldInteraction[] {
                new() {
                    ActionLangCode = "blockhelp-door-openclose",
                    MouseButton = EnumMouseButton.Right
                }
            };
        });

        drawerOpenClose = ObjectCacheUtil.GetOrCreate(api, "drawerOpenClose", () => {
            return new WorldInteraction[] {
                new() {
                    ActionLangCode = "blockhelp-door-openclose",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "shift"
                }
            };
        });

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
                    return itemSlottableInteractions;
                }
            }

            if (selection.SelectionBoxIndex == 11) {
                if (becc.DrawerOpen) {
                    return drawerOpenClose.Append(drawerInteractions);
                }
                else {
                    return drawerOpenClose;
                }
            }
        }

        return doorOpenClose;
    }

    #region MBColSelBoxes

    // Selection box for master block
    public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos) {
        BECoolingCabinet? be = blockAccessor.GetBlockEntityExt<BECoolingCabinet>(pos);
        var boxes = base.GetSelectionBoxes(blockAccessor, pos);

        if (be == null) return boxes;

        Cuboidf drawerSelBox = boxes[11].Clone();
        Cuboidf cabinetSelBox = boxes[12].Clone();

        if (be.DrawerOpen) {
            BlockDirection rotAngle = (BlockDirection)this.GetRotationAngle();

            switch (rotAngle) {
                case BlockDirection.North: drawerSelBox.Z2 += .3125f; break;
                case BlockDirection.West: drawerSelBox.X2 += .3125f; break;
                case BlockDirection.South: drawerSelBox.Z1 -= .3125f; break;
                case BlockDirection.East: drawerSelBox.X1 -= .3125f; break;
            }
        }

        if (be.DoorOpen) {
            Cuboidf bottomShelfL = boxes[0].Clone();
            Cuboidf bottomShelfM = boxes[1].Clone();

            return [bottomShelfL, bottomShelfM, Skip, Skip, Skip, Skip, Skip, Skip, Skip, Skip, Skip, drawerSelBox];
        }

        return [Skip, Skip, Skip, Skip, Skip, Skip, Skip, Skip, Skip, Skip, Skip, drawerSelBox, cabinetSelBox];
    }

    // Selection boxes for multiblock parts
    public Cuboidf[] MBGetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset) {
        // Selection Box indexes:
        // Drawer - 11 / Cabinet - 12
        // LDoor  - 9 / RDoor - 10
        // Top    - 6 7 8
        // Middle - 3 4 5
        // Bottom - 0 1 2

        BECoolingCabinet? be = blockAccessor.GetBlockEntityExt<BECoolingCabinet>(pos);
        var boxes = base.GetSelectionBoxes(blockAccessor, pos);

        if (be == null) return boxes;

        Cuboidf drawerSelBox = boxes[11].Clone();

        drawerSelBox.MBNormalizeSelectionBox(offset);

        if (be.DrawerOpen) {
            BlockDirection rotAngle = (BlockDirection)this.GetRotationAngle();

            switch (rotAngle) {
                case BlockDirection.North: drawerSelBox.Z2 += .3125f; break;
                case BlockDirection.West: drawerSelBox.X2 += .3125f; break;
                case BlockDirection.South: drawerSelBox.Z1 -= .3125f; break;
                case BlockDirection.East: drawerSelBox.X1 -= .3125f; break;
            }
        }

        if (!be.DoorOpen) {
            Cuboidf cabinetSelBox = boxes[12].Clone();
            cabinetSelBox.MBNormalizeSelectionBox(offset);

            return [Skip, Skip, Skip, Skip, Skip, Skip, Skip, Skip, Skip, Skip, Skip, drawerSelBox, cabinetSelBox];
        }
        else {
            List<Cuboidf> sBs = [];

            for (int i = 0; i < 11; i++) {
                sBs.Add(boxes[i].Clone());
                sBs[i].MBNormalizeSelectionBox(offset);
            }
            sBs.Add(drawerSelBox);

            return [sBs[0], sBs[1], sBs[2], sBs[3], sBs[4], sBs[5], sBs[6], sBs[7], sBs[8], sBs[9], sBs[10], sBs[11]];
        }
    }

    public Cuboidf[] MBGetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset) {
        int rot = this.GetRotationAngle();

        bool collides = (BlockDirection)rot switch {
            BlockDirection.North => offset.Z == 0,
            BlockDirection.West => offset.X == 0,
            BlockDirection.South => offset.Z == 0,
            BlockDirection.East => offset.X == 0,
            _ => offset.Z == 0
        };

        if (!collides) return [];

        return [new Cuboidf(0, 0, 0, 1, 1, 1)];
    }

    #endregion
}