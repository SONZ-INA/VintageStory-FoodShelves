using System.Linq;

namespace FoodShelves;

public class BlockCoolingCabinet : BaseFSContainer, IMultiBlockColSelBoxes {
    private WorldInteraction[] itemSlottableInteractions;
    private WorldInteraction[] cabinetInteractions;
    private WorldInteraction[] drawerInteractions;

    public readonly AssetLocation soundCabinetOpen = new(SoundReferences.CoolingCabinetOpen);
    public readonly AssetLocation soundCabinetClose = new(SoundReferences.CoolingCabinetClose);
    public readonly AssetLocation soundDrawerOpen = new(SoundReferences.IceDrawerOpen);
    public readonly AssetLocation soundDrawerClose = new(SoundReferences.IceDrawerClose);

    public override void OnLoaded(ICoreAPI api) {
        base.OnLoaded(api);

        itemSlottableInteractions = ObjectCacheUtil.GetOrCreate(api, "coolingCabinetItemInteractions", () => {
            List<ItemStack> holderUniversalStackList = new();

            foreach (var obj in api.World.Collectibles) {
                if (obj.CanStoreInSlot("fsHolderUniversal")) {
                    holderUniversalStackList.Add(new ItemStack(obj));
                }
            }

            return new WorldInteraction[] {
                new() {
                    ActionLangCode = "blockhelp-groundstorage-add",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = holderUniversalStackList.ToArray()
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
            List<ItemStack> coolingOnlyStackList = new();

            foreach (var obj in api.World.Collectibles) {
                if (obj.CanStoreInSlot("fsCoolingOnly")) {
                    coolingOnlyStackList.Add(new ItemStack(obj));
                }
            }

            return new WorldInteraction[] {
                new() {
                    ActionLangCode = "blockhelp-groundstorage-addone",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = coolingOnlyStackList.ToArray()
                },
                new() {
                    ActionLangCode = "blockhelp-groundstorage-addbulk",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = coolingOnlyStackList.ToArray(),
                    HotKeyCode = "ctrl",
                }
            };
        });
    }

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer) {
        if (selection.SelectionBoxIndex == 9 && world.BlockAccessor.GetBlockEntity(selection.Position) is BECoolingCabinet becc) {
            if (becc.DrawerOpen) {
                if (becc.Inventory?[becc.cutIceSlot].Empty == true || becc.Inventory?[becc.cutIceSlot].CanStoreInSlot("fsCoolingOnly") == true) {
                    return cabinetInteractions.Append(drawerInteractions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer)));
                }
            }
        }

        if (selection.SelectionBoxIndex > 8) return cabinetInteractions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        else return cabinetInteractions.Append(itemSlottableInteractions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer)));
    }

    // Selection box for master block
    public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos) {
        BECoolingCabinet be = blockAccessor.GetBlockEntityExt<BECoolingCabinet>(pos);

        Cuboidf drawerSelBox = base.GetSelectionBoxes(blockAccessor, pos).ElementAt(9).Clone();
        Cuboidf cabinetSelBox = base.GetSelectionBoxes(blockAccessor, pos).ElementAt(10).Clone();
        Cuboidf skip = new(); // Skip selectionBox, to keep consistency between selectionBox indexes (1-8-shelves 9-drawer 10-cabinet)

        if (be != null) {
            if (be.DrawerOpen) {
                int rotAngle = GetRotationAngle(this);

                switch (rotAngle) {
                    case 0: drawerSelBox.Z2 += .3125f; break;
                    case 90: drawerSelBox.X2 += .3125f; break;
                    case 180: drawerSelBox.Z1 -= .3125f; break;
                    case 270: drawerSelBox.X1 -= .3125f; break;
                }
            }

            if (be.CabinetOpen) {
                Cuboidf bottomShelfL = base.GetSelectionBoxes(blockAccessor, pos).ElementAt(0).Clone();
                Cuboidf bottomShelfM = base.GetSelectionBoxes(blockAccessor, pos).ElementAt(1).Clone();

                return new Cuboidf[] { bottomShelfL, bottomShelfM, skip, skip, skip, skip, skip, skip, skip, drawerSelBox };
            }

            return new Cuboidf[] { skip, skip, skip, skip, skip, skip, skip, skip, skip, drawerSelBox, cabinetSelBox };
        }

        return base.GetSelectionBoxes(blockAccessor, pos);
    }

    // Selection boxes for multiblock parts
    public Cuboidf[] MBGetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset) {
        // Selection Box indexes:
        // Drawer - 9 / Cabinet - 10
        // Top    - 6 7 8
        // Middle - 3 4 5
        // Bottom - 0 1 2

        BECoolingCabinet be = blockAccessor.GetBlockEntityExt<BECoolingCabinet>(pos);
        if (be != null) {
            Cuboidf drawerSelBox = base.GetSelectionBoxes(blockAccessor, pos).ElementAt(9).Clone();
            Cuboidf skip = new(); // Skip selectionBox, to keep consistency between selectionBox indexes (1-8-shelves 9-drawer 10-cabinet)

            drawerSelBox.MBNormalizeSelectionBox(offset);

            if (be.DrawerOpen) {
                int rotAngle = GetRotationAngle(this);

                switch (rotAngle) {
                    case 0: drawerSelBox.Z2 += .3125f; break;
                    case 90: drawerSelBox.X2 += .3125f; break;
                    case 180: drawerSelBox.Z1 -= .3125f; break;
                    case 270: drawerSelBox.X1 -= .3125f; break;
                }
            }

            if (!be.CabinetOpen) {
                Cuboidf cabinetSelBox = base.GetSelectionBoxes(blockAccessor, pos).ElementAt(10).Clone();
                cabinetSelBox.MBNormalizeSelectionBox(offset);

                return new Cuboidf[] { skip, skip, skip, skip, skip, skip, skip, skip, skip, drawerSelBox, cabinetSelBox };
            }
            else {
                List<Cuboidf> sBs = new();

                for (int i = 0; i < 9; i++) {
                    sBs.Add(base.GetSelectionBoxes(blockAccessor, pos).ElementAt(i).Clone());
                    sBs[i].MBNormalizeSelectionBox(offset);
                }
                sBs.Add(drawerSelBox);

                if (offset.Y != 0) {
                    return new Cuboidf[] { skip, skip, skip, sBs[3], sBs[4], sBs[5], sBs[6], sBs[7], sBs[8] };
                }

                return new Cuboidf[] { skip, sBs[1], sBs[2], skip, skip, skip, skip, skip, skip, sBs[9] };
            }
        }

        return base.GetSelectionBoxes(blockAccessor, pos);
    }

    public Cuboidf[] MBGetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset) {
        return base.GetCollisionBoxes(blockAccessor, pos);
    }
}