using CipherSnagemEditor.Colosseum.Data;
using CipherSnagemEditor.Core.Archives;
using CipherSnagemEditor.Core.GameCube;

namespace CipherSnagemEditor.Colosseum;

public sealed record ColosseumShopRandomizerResult(string PocketMenuRelPath, int ChangedItems);

public sealed partial class ColosseumProjectContext
{
    private ColosseumShopRandomizerResult RandomizePocketMenuShops(ColosseumCommonRel commonRel)
    {
        var pocket = ColosseumPocketMenuRel.Parse(LoadPocketMenuRelBytes());
        var changedItems = pocket.RandomizeShops(commonRel.ItemData);
        foreach (var item in commonRel.ItemData.Where(item => item.Price > 0))
        {
            commonRel.WriteItem(new ColosseumItemUpdate(
                item.Index,
                item.NameId,
                item.DescriptionId,
                item.BagSlotId,
                item.CanBeHeld,
                item.Price,
                checked(item.Price * 10),
                item.Parameter,
                item.HoldItemId,
                item.InBattleUseId,
                item.FriendshipEffects,
                item.TmIndex,
                item.TmMoveId));
        }

        var path = WritePocketMenuRel(pocket.ToArray());
        return new ColosseumShopRandomizerResult(path, changedItems);
    }

    private byte[] LoadPocketMenuRelBytes()
    {
        if (Iso is null)
        {
            throw new InvalidOperationException("No Colosseum ISO is loaded.");
        }

        var workspacePath = Path.Combine(GetIsoExportDirectory("pocket_menu.fsys"), "pocket_menu.rel");
        if (File.Exists(workspacePath))
        {
            return File.ReadAllBytes(workspacePath);
        }

        var pocketEntry = Iso.Files.FirstOrDefault(entry =>
            string.Equals(Path.GetFileName(entry.Name), "pocket_menu.fsys", StringComparison.OrdinalIgnoreCase));
        if (pocketEntry is null)
        {
            throw new FileNotFoundException("Could not find pocket_menu.fsys in the ISO.");
        }

        var fsysBytes = GameCubeIsoReader.ReadFile(Iso, pocketEntry);
        var rawFsysPath = ResolveIsoExtractPath(pocketEntry.Name, null);
        Directory.CreateDirectory(Path.GetDirectoryName(rawFsysPath) ?? WorkspaceDirectory!);
        if (!File.Exists(rawFsysPath))
        {
            File.WriteAllBytes(rawFsysPath, fsysBytes);
        }

        var archive = FsysArchive.Parse(pocketEntry.Name, fsysBytes);
        var relEntry = archive.Entries.FirstOrDefault(entry =>
            string.Equals(entry.Name, "pocket_menu.rel", StringComparison.OrdinalIgnoreCase));
        if (relEntry is null)
        {
            throw new FileNotFoundException("Could not find pocket_menu.rel inside pocket_menu.fsys.");
        }

        return archive.Extract(relEntry);
    }

    private string WritePocketMenuRel(byte[] bytes)
    {
        var folder = GetIsoExportDirectory("pocket_menu.fsys");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "pocket_menu.rel");
        File.WriteAllBytes(path, bytes);
        LoadedFiles["pocket_menu.rel"] = bytes;
        return path;
    }
}
