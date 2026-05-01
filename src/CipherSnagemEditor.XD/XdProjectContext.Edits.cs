using CipherSnagemEditor.Core.Archives;
using CipherSnagemEditor.Core.Binary;
using CipherSnagemEditor.Core.GameCube;
using CipherSnagemEditor.Core.Relocation;
using CipherSnagemEditor.Core.Text;

namespace CipherSnagemEditor.XD;

public sealed partial class XdProjectContext
{
    public string SavePokemonStats(XdPokemonStatsUpdate update)
    {
        var (data, table, _) = ReadCommonRelOrThrow();
        var offset = CommonRelRowOffset(data, table, XdPokemonStatsIndex, XdNumberOfPokemonIndex, XdPokemonStatsSize, update.Index, 1000, "Pokemon stats");

        data.WriteByte(offset + XdPokemonExpRateOffset, ClampByte(update.ExpRate));
        data.WriteByte(offset + XdPokemonCatchRateOffset, ClampByte(update.CatchRate));
        data.WriteByte(offset + XdPokemonGenderRatioOffset, ClampByte(update.GenderRatio));
        data.WriteByte(offset + XdPokemonBaseExpOffset, ClampByte(update.BaseExp));
        data.WriteByte(offset + XdPokemonBaseHappinessOffset, ClampByte(update.BaseHappiness));
        data.WriteUInt16(offset + XdPokemonHeightOffset, ClampUInt16ToU16((int)Math.Round(update.Height * 10.0)));
        data.WriteUInt16(offset + XdPokemonWeightOffset, ClampUInt16ToU16((int)Math.Round(update.Weight * 10.0)));
        data.WriteUInt32(offset + XdPokemonStatsNameIdOffset, ClampUInt32(update.NameId));
        data.WriteByte(offset + XdPokemonType1Offset, ClampByte(update.Type1));
        data.WriteByte(offset + XdPokemonType2Offset, ClampByte(update.Type2));
        data.WriteByte(offset + XdPokemonAbility1Offset, ClampByte(update.Ability1));
        data.WriteByte(offset + XdPokemonAbility2Offset, ClampByte(update.Ability2));

        for (var tm = 0; tm < XdPokemonTmCount; tm++)
        {
            data.WriteByte(offset + XdPokemonFirstTmOffset + tm, tm < update.LearnableTms.Count && update.LearnableTms[tm] ? (byte)1 : (byte)0);
        }

        data.WriteUInt16(offset + XdPokemonHeldItem1Offset, ClampUInt16ToU16(update.HeldItem1));
        data.WriteUInt16(offset + XdPokemonHeldItem2Offset, ClampUInt16ToU16(update.HeldItem2));
        data.WriteByte(offset + XdPokemonHpOffset, ClampByte(update.Hp));
        data.WriteByte(offset + XdPokemonAttackOffset, ClampByte(update.Attack));
        data.WriteByte(offset + XdPokemonDefenseOffset, ClampByte(update.Defense));
        data.WriteByte(offset + XdPokemonSpecialAttackOffset, ClampByte(update.SpecialAttack));
        data.WriteByte(offset + XdPokemonSpecialDefenseOffset, ClampByte(update.SpecialDefense));
        data.WriteByte(offset + XdPokemonSpeedOffset, ClampByte(update.Speed));
        data.WriteUInt16(offset + XdPokemonEvYieldOffset, ClampUInt16ToU16(update.HpYield));
        data.WriteUInt16(offset + XdPokemonEvYieldOffset + 2, ClampUInt16ToU16(update.AttackYield));
        data.WriteUInt16(offset + XdPokemonEvYieldOffset + 4, ClampUInt16ToU16(update.DefenseYield));
        data.WriteUInt16(offset + XdPokemonEvYieldOffset + 6, ClampUInt16ToU16(update.SpecialAttackYield));
        data.WriteUInt16(offset + XdPokemonEvYieldOffset + 8, ClampUInt16ToU16(update.SpecialDefenseYield));
        data.WriteUInt16(offset + XdPokemonEvYieldOffset + 10, ClampUInt16ToU16(update.SpeedYield));

        for (var row = 0; row < XdPokemonEvolutionCount; row++)
        {
            var evolutionOffset = offset + XdPokemonFirstEvolutionOffset + (row * 6);
            var evolution = row < update.Evolutions.Count ? update.Evolutions[row] : null;
            data.WriteByte(evolutionOffset, ClampByte(evolution?.Method ?? 0));
            data.WriteUInt16(evolutionOffset + 2, ClampUInt16ToU16(evolution?.Condition ?? 0));
            data.WriteUInt16(evolutionOffset + 4, ClampUInt16ToU16(evolution?.EvolvedSpeciesId ?? 0));
        }

        for (var row = 0; row < XdPokemonLevelUpMoveCount; row++)
        {
            var moveOffset = offset + XdPokemonFirstLevelUpMoveOffset + (row * 4);
            var move = row < update.LevelUpMoves.Count ? update.LevelUpMoves[row] : null;
            data.WriteByte(moveOffset, ClampByte(move?.Level ?? 0));
            data.WriteUInt16(moveOffset + 2, ClampUInt16ToU16(move?.MoveId ?? 0));
        }

        return WriteCommonRel(data);
    }

    public string SaveMove(XdMoveUpdate update)
    {
        var (data, table, _) = ReadCommonRelOrThrow();
        var offset = CommonRelRowOffset(data, table, XdMovesIndex, XdNumberOfMovesIndex, XdMoveSize, update.Index, 1000, "Move");

        data.WriteByte(offset + XdMovePriorityOffset, ClampSignedByte(update.Priority));
        data.WriteByte(offset + XdMovePpOffset, ClampByte(update.Pp));
        data.WriteByte(offset + XdMoveTypeOffset, ClampByte(update.TypeId));
        data.WriteByte(offset + XdMoveTargetsOffset, ClampByte(update.TargetId));
        data.WriteByte(offset + XdMoveAccuracyOffset, ClampByte(update.Accuracy));
        data.WriteByte(offset + XdMoveEffectAccuracyOffset, ClampByte(update.EffectAccuracy));
        data.WriteByte(offset + XdMoveContactFlagOffset, Flag(update.ContactFlag));
        data.WriteByte(offset + XdMoveProtectFlagOffset, Flag(update.ProtectFlag));
        data.WriteByte(offset + XdMoveMagicCoatFlagOffset, Flag(update.MagicCoatFlag));
        data.WriteByte(offset + XdMoveSnatchFlagOffset, Flag(update.SnatchFlag));
        data.WriteByte(offset + XdMoveMirrorMoveFlagOffset, Flag(update.MirrorMoveFlag));
        data.WriteByte(offset + XdMoveKingsRockFlagOffset, Flag(update.KingsRockFlag));
        data.WriteByte(offset + XdMoveSoundBasedFlagOffset, Flag(update.SoundBasedFlag));
        data.WriteByte(offset + XdMoveHmFlagOffset, Flag(update.HmFlag));
        data.WriteByte(offset + XdMoveCategoryOffset, ClampByte(update.CategoryId));
        data.WriteByte(offset + XdMoveBasePowerOffset, ClampByte(update.Power));
        data.WriteUInt16(offset + XdMoveEffectOffset, ClampUInt16ToU16(update.EffectId));
        data.WriteUInt16(offset + XdMoveAnimationOffset, ClampUInt16ToU16(update.AnimationId));
        data.WriteUInt32(offset + XdMoveNameIdOffset, ClampUInt32(update.NameId));
        data.WriteUInt32(offset + XdMoveDescriptionIdOffset, ClampUInt32(update.DescriptionId));
        data.WriteUInt16(offset + XdMoveAnimation2Offset, ClampUInt16ToU16(update.Animation2Id));
        data.WriteByte(offset + XdMoveEffectTypeOffset, ClampByte(update.EffectTypeId));

        return WriteCommonRel(data);
    }

    public string SaveItem(XdItemUpdate update)
    {
        var (data, table, _) = ReadCommonRelOrThrow();
        var offset = CommonRelRowOffset(data, table, XdItemsIndex, XdNumberOfItemsIndex, XdItemSize, update.Index, 2000, "Item");

        data.WriteByte(offset + XdItemBagSlotOffset, ClampByte(update.BagSlotId));
        data.WriteByte(offset + XdItemCantBeHeldOffset, update.CanBeHeld ? (byte)0 : (byte)1);
        data.WriteByte(offset + XdItemInBattleUseOffset, ClampByte(update.InBattleUseId));
        data.WriteUInt16(offset + XdItemPriceOffset, ClampUInt16ToU16(update.Price));
        data.WriteUInt16(offset + XdItemCouponOffset, ClampUInt16ToU16(update.CouponPrice));
        data.WriteByte(offset + XdItemBattleHoldOffset, ClampByte(update.HoldItemId));
        data.WriteUInt32(offset + XdItemNameIdOffset, ClampUInt32(update.NameId));
        data.WriteUInt32(offset + XdItemDescriptionIdOffset, ClampUInt32(update.DescriptionId));
        data.WriteByte(offset + XdItemParameterOffset, ClampByte(update.Parameter));
        for (var index = 0; index < 4; index++)
        {
            data.WriteByte(offset + XdItemFriendshipOffset + index, ClampSignedByte(ValueAt(update.FriendshipEffects, index)));
        }

        return WriteCommonRel(data);
    }

    public string SaveType(XdTypeUpdate update)
    {
        var (data, table, _) = ReadCommonRelOrThrow();
        var offset = CommonRelRowOffset(data, table, XdTypeIndex, XdNumberOfTypesIndex, XdTypeSize, update.Index, 64, "Type");

        data.WriteByte(offset + XdTypeCategoryOffset, ClampByte(update.CategoryId));
        data.WriteUInt32(offset + XdTypeNameIdOffset, ClampUInt32(update.NameId));
        for (var index = 0; index < update.Effectiveness.Count; index++)
        {
            data.WriteByte(offset + XdTypeFirstEffectivenessOffset + index, ClampByte(update.Effectiveness[index]));
        }

        return WriteCommonRel(data);
    }

    public string SaveTreasure(XdTreasureUpdate update)
    {
        var (data, table, _) = ReadCommonRelOrThrow();
        var offset = CommonRelRowOffset(data, table, XdTreasureIndex, XdNumberOfTreasuresIndex, XdTreasureSize, update.Index, 1000, "Treasure");

        data.WriteByte(offset + XdTreasureModelOffset, ClampByte(update.ModelId));
        data.WriteByte(offset + XdTreasureQuantityOffset, ClampByte(update.Quantity));
        data.WriteUInt16(offset + XdTreasureAngleOffset, ClampUInt16ToU16(update.Angle));
        data.WriteUInt16(offset + XdTreasureRoomOffset, ClampUInt16ToU16(update.RoomId));
        data.WriteUInt16(offset + XdTreasureItemOffset, ClampUInt16ToU16(update.ItemId));
        WriteSingle(data, offset + XdTreasureXOffset, update.X);
        WriteSingle(data, offset + XdTreasureYOffset, update.Y);
        WriteSingle(data, offset + XdTreasureZOffset, update.Z);

        return WriteCommonRel(data);
    }

    public string SaveInteractionPoint(XdInteractionPointUpdate update)
    {
        var (data, table, _) = ReadCommonRelOrThrow();
        var offset = CommonRelRowOffset(data, table, XdInteractionPointsIndex, XdNumberOfInteractionPointsIndex, XdInteractionPointSize, update.Index, 5000, "Interaction point");

        for (var index = 0; index < XdInteractionPointSize; index++)
        {
            data.WriteByte(offset + index, 0);
        }

        data.WriteByte(offset + XdInteractionMethodOffset, ClampByte(update.InteractionMethodId));
        data.WriteUInt16(offset + XdInteractionRoomIdOffset, ClampUInt16ToU16(update.RoomId));
        data.WriteByte(offset + XdInteractionRegionIdOffset, ClampByte(update.RegionId));

        var scriptIdentifier = update.InfoKind == XdInteractionInfoKind.None
            ? 0
            : update.InfoKind == XdInteractionInfoKind.CurrentScript
                ? XdInteractionCurrentScriptIdentifier
                : XdInteractionCommonScriptIdentifier;
        data.WriteUInt16(offset + XdInteractionScriptValueOffset, ClampUInt16ToU16(scriptIdentifier));
        data.WriteUInt16(offset + XdInteractionScriptIndexOffset, ClampUInt16ToU16(XdInteractionScriptIndexFor(update)));

        switch (update.InfoKind)
        {
            case XdInteractionInfoKind.None:
                break;
            case XdInteractionInfoKind.Warp:
                data.WriteUInt16(offset + XdInteractionWarpTargetRoomIdOffset, ClampUInt16ToU16(update.TargetRoomId));
                data.WriteByte(offset + XdInteractionWarpTargetEntryIdOffset, ClampByte(update.TargetEntryId));
                data.WriteByte(offset + XdInteractionWarpSoundOffset, update.Sound ? (byte)1 : (byte)0);
                break;
            case XdInteractionInfoKind.Door:
                data.WriteUInt16(offset + XdInteractionDoorIdOffset, ClampUInt16ToU16(update.DoorId));
                break;
            case XdInteractionInfoKind.Text:
                data.WriteUInt16(offset + XdInteractionStringIdOffset, ClampUInt16ToU16(update.StringId));
                break;
            case XdInteractionInfoKind.Elevator:
                data.WriteUInt16(offset + XdInteractionElevatorIdOffset, ClampUInt16ToU16(update.ElevatorId));
                data.WriteUInt16(offset + XdInteractionElevatorTargetRoomIdOffset, ClampUInt16ToU16(update.TargetRoomId));
                data.WriteUInt16(offset + XdInteractionTargetElevatorIdOffset, ClampUInt16ToU16(update.TargetElevatorId));
                data.WriteByte(offset + XdInteractionElevatorDirectionOffset, ClampByte(update.DirectionId));
                break;
            case XdInteractionInfoKind.CutsceneWarp:
                data.WriteUInt16(offset + XdInteractionWarpTargetRoomIdOffset, ClampUInt16ToU16(update.TargetRoomId));
                data.WriteByte(offset + XdInteractionWarpTargetEntryIdOffset, ClampByte(update.TargetEntryId));
                data.WriteUInt16(offset + XdInteractionCutsceneIdOffset, ClampUInt16ToU16(update.CutsceneId));
                data.WriteUInt16(offset + XdInteractionCameraIdOffset, ClampUInt16ToU16(update.CameraFsysId));
                data.WriteByte(offset + XdInteractionCameraIdOffset + 1, 0x18);
                break;
            case XdInteractionInfoKind.Pc:
                data.WriteUInt16(offset + XdInteractionPcRoomIdOffset, ClampUInt16ToU16(update.TargetRoomId));
                data.WriteByte(offset + XdInteractionPcUnknownOffset, ClampByte(update.PcUnknown));
                break;
            case XdInteractionInfoKind.CurrentScript:
            case XdInteractionInfoKind.CommonScript:
                data.WriteUInt32(offset + XdInteractionParameter1Offset, update.Parameter1);
                data.WriteUInt32(offset + XdInteractionParameter2Offset, update.Parameter2);
                data.WriteUInt32(offset + XdInteractionParameter3Offset, update.Parameter3);
                data.WriteUInt32(offset + XdInteractionParameter4Offset, update.Parameter4);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(update), $"Unsupported XD interaction info kind {update.InfoKind}.");
        }

        return WriteCommonRel(data);
    }

    public string SaveShadowPokemon(XdShadowPokemonUpdate update)
    {
        var archive = TryReadFsys("deck_archive.fsys", out var error)
            ?? throw new InvalidDataException(error ?? "deck_archive.fsys was not found.");
        var darkEntry = FindDeckEntry(archive, "DeckData_DarkPokemon")
            ?? throw new InvalidDataException("DeckData_DarkPokemon was not found in deck_archive.fsys.");
        var storyEntry = FindDeckEntry(archive, "DeckData_Story")
            ?? throw new InvalidDataException("DeckData_Story was not found in deck_archive.fsys.");

        var darkBytes = archive.Extract(darkEntry);
        var storyBytes = archive.Extract(storyEntry);
        if (!TryReadDeckLayout(storyBytes, out var storyLayout, out var storyError))
        {
            throw new InvalidDataException(storyError);
        }

        var darkStart = 0x20 + (update.Index * XdShadowPokemonSize);
        if (darkStart < 0 || darkStart + XdShadowPokemonSize > darkBytes.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(update), $"Shadow Pokemon #{update.Index} is outside DeckData_DarkPokemon.");
        }

        var storyStart = storyLayout.PokemonDataOffset + (update.StoryPokemonIndex * XdDeckPokemonSize);
        if (storyStart < 0 || storyStart + XdDeckPokemonSize > storyBytes.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(update), $"Story Pokemon #{update.StoryPokemonIndex} is outside DeckData_Story.");
        }

        darkBytes[darkStart + XdShadowFleeOffset] = ClampByte(update.FleeValue);
        darkBytes[darkStart + XdShadowCatchRateOffset] = ClampByte(update.CatchRate);
        darkBytes[darkStart + XdShadowLevelOffset] = ClampByte(update.Level);
        darkBytes[darkStart + XdShadowInUseOffset] = ClampByte(update.InUseFlag);
        WriteU16(darkBytes, darkStart + XdShadowStoryIndexOffset, update.StoryPokemonIndex);
        WriteU16(darkBytes, darkStart + XdShadowHeartGaugeOffset, update.HeartGauge);
        for (var slot = 0; slot < 4; slot++)
        {
            WriteU16(darkBytes, darkStart + XdShadowFirstMoveOffset + (slot * 2), ValueAt(update.ShadowMoveIds, slot));
        }

        darkBytes[darkStart + XdShadowAggressionOffset] = ClampByte(update.Aggression);
        darkBytes[darkStart + XdShadowAlwaysFleeOffset] = ClampByte(update.AlwaysFlee);

        WriteU16(storyBytes, storyStart + XdDeckPokemonSpeciesOffset, update.SpeciesId);
        storyBytes[storyStart + XdDeckPokemonLevelOffset] = ClampByte(update.ShadowBoostLevel);
        storyBytes[storyStart + 0x03] = ClampByte(update.Happiness);
        WriteU16(storyBytes, storyStart + XdDeckPokemonItemOffset, update.ItemId);
        for (var iv = 0; iv < 6; iv++)
        {
            storyBytes[storyStart + 0x08 + iv] = ClampByte(update.Iv);
        }

        for (var ev = 0; ev < 6; ev++)
        {
            storyBytes[storyStart + 0x0e + ev] = ClampByte(ValueAt(update.Evs, ev));
        }

        for (var slot = 0; slot < 4; slot++)
        {
            WriteU16(storyBytes, storyStart + XdDeckPokemonFirstMoveOffset + (slot * 2), ValueAt(update.RegularMoveIds, slot));
        }

        var originalPid = storyBytes[storyStart + 0x1e];
        var ability = update.Ability is 0 or 1 ? update.Ability : originalPid % 2;
        var gender = update.Gender is >= 0 and <= 3 ? update.Gender : (originalPid / 2) % 4;
        var nature = update.Nature is >= 0 and <= 24 ? update.Nature : originalPid / 8;
        storyBytes[storyStart + 0x1e] = checked((byte)((nature << 3) + (gender << 1) + ability));

        return WriteFsysEntries("deck_archive.fsys", new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            [darkEntry.Name] = darkBytes,
            [storyEntry.Name] = storyBytes
        });
    }

    public string SavePokespot(XdPokespotUpdate update)
    {
        var (data, _, _) = ReadCommonRelOrThrow();
        if (update.StartOffset < 0 || update.StartOffset + XdPokespotEntrySize > data.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(update), $"Pokespot row at 0x{update.StartOffset:x} is outside common.rel.");
        }

        data.WriteByte(update.StartOffset, ClampByte(update.MinLevel));
        data.WriteByte(update.StartOffset + 1, ClampByte(update.MaxLevel));
        data.WriteUInt16(update.StartOffset + 2, checked((ushort)ClampUInt16(update.SpeciesId)));
        data.WriteByte(update.StartOffset + 7, ClampByte(update.EncounterPercentage));
        data.WriteUInt16(update.StartOffset + 0x0a, checked((ushort)ClampUInt16(update.StepsPerSnack)));

        return WriteFsysEntries("common.fsys", new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["common.rel"] = data.ToArray()
        });
    }

    public string SaveGiftPokemon(XdGiftPokemonUpdate update)
    {
        var layout = XdGiftLayouts(Iso.Region).FirstOrDefault(candidate => candidate.RowId == update.RowId)
            ?? throw new ArgumentOutOfRangeException(nameof(update), $"Gift Pokemon row {update.RowId} is unknown.");
        var dolEntry = FindIsoFile("Start.dol")
            ?? throw new FileNotFoundException("Start.dol was not found in the ISO.");
        var dol = new BinaryData(GameCubeIsoReader.ReadFile(Iso, dolEntry));

        dol.WriteUInt16(layout.StartOffset + layout.SpeciesOffset, ClampUInt16ToU16(update.SpeciesId));
        if (layout.LevelOffset >= 0)
        {
            dol.WriteByte(layout.StartOffset + layout.LevelOffset, ClampByte(update.Level));
        }
        else
        {
            dol.WriteByte(layout.SharedLevelOffset, ClampByte(update.Level));
        }

        if (!layout.UsesLevelUpMoves)
        {
            for (var index = 0; index < layout.MoveOffsets.Count; index++)
            {
                dol.WriteUInt16(layout.StartOffset + layout.MoveOffsets[index], ClampUInt16ToU16(ValueAt(update.MoveIds, index)));
            }
        }

        var bytes = dol.ToArray();
        WriteIsoEntry(dolEntry, bytes);
        var workspacePath = Path.Combine(WorkspaceDirectory, "Game Files", "Start.dol");
        Directory.CreateDirectory(Path.GetDirectoryName(workspacePath)!);
        File.WriteAllBytes(workspacePath, bytes);
        return workspacePath;
    }

    public XdMessageTable SaveMessageString(string isoFileName, string entryName, int id, string text)
    {
        var sourceBytes = LoadMessageTableBytes(isoFileName, entryName);
        var updatedTable = GameStringTable.Parse(sourceBytes).WithString(id, text);
        var bytes = updatedTable.ToArray(allowGrowth: Settings.IncreaseFileSizes);
        WriteFsysEntries(isoFileName, new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            [entryName] = bytes
        });

        return new XdMessageTable(
            $"{Path.GetFileNameWithoutExtension(isoFileName)}/{entryName}",
            isoFileName,
            entryName,
            updatedTable.Strings.Select(message => new XdMessageString(
                message.Id,
                $"0x{message.Id:X}",
                message.Text)).ToArray());
    }

    private byte[] LoadMessageTableBytes(string isoFileName, string entryName)
    {
        var workspacePath = WorkspaceFsysEntryPath(isoFileName, entryName);
        if (File.Exists(workspacePath))
        {
            return File.ReadAllBytes(workspacePath);
        }

        var archive = TryReadFsys(isoFileName, out var error)
            ?? throw new InvalidDataException(error ?? $"{isoFileName} could not be read.");
        var messageEntry = archive.Entries.FirstOrDefault(entry =>
            string.Equals(entry.Name, entryName, StringComparison.OrdinalIgnoreCase));
        if (messageEntry is null)
        {
            throw new FileNotFoundException($"{entryName} was not found in {isoFileName}.");
        }

        return archive.Extract(messageEntry);
    }

    private static int XdInteractionScriptIndexFor(XdInteractionPointUpdate update)
        => update.InfoKind switch
        {
            XdInteractionInfoKind.None => 0,
            XdInteractionInfoKind.Warp => XdInteractionWarpScriptIndex,
            XdInteractionInfoKind.Door => XdInteractionDoorScriptIndex,
            XdInteractionInfoKind.Text => XdInteractionTextScriptIndex,
            XdInteractionInfoKind.Elevator => XdInteractionElevatorScriptIndex,
            XdInteractionInfoKind.CutsceneWarp => XdInteractionCutsceneScriptIndex,
            XdInteractionInfoKind.Pc => XdInteractionPcScriptIndex,
            _ => update.ScriptIndex
        };

    private string WriteCommonRel(BinaryData data)
        => WriteFsysEntries("common.fsys", new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["common.rel"] = data.ToArray()
        });

    private static int CommonRelRowOffset(
        BinaryData data,
        RelocationTable table,
        int tablePointerIndex,
        int countPointerIndex,
        int rowSize,
        int rowIndex,
        int maxCount,
        string label)
    {
        var start = table.GetPointer(tablePointerIndex);
        var count = table.GetValueAtPointer(countPointerIndex);
        if (!IsSafeTableRange(data, start, count, rowSize, maxCount))
        {
            throw new InvalidDataException($"{label} table is outside common.rel.");
        }

        if (rowIndex < 0 || rowIndex >= count)
        {
            throw new ArgumentOutOfRangeException(nameof(rowIndex), $"{label} #{rowIndex} is outside the {count}-row table.");
        }

        return checked(start + (rowIndex * rowSize));
    }

    private string WriteFsysEntries(string fsysName, IReadOnlyDictionary<string, byte[]> replacements)
    {
        var isoEntry = FindIsoFile(fsysName)
            ?? throw new FileNotFoundException($"{fsysName} was not found in the ISO.");
        var archive = FsysArchive.Parse(isoEntry.Name, GameCubeIsoReader.ReadFile(Iso, isoEntry));
        var result = archive.ReplaceFiles(replacements);
        if (result.ReplacedFiles.Count != replacements.Count)
        {
            var replaced = result.ReplacedFiles.Select(file => file.EntryName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missing = replacements.Keys.Where(key => !replaced.Contains(key));
            throw new InvalidDataException($"{fsysName} is missing expected entries: {string.Join(", ", missing)}");
        }

        WriteIsoEntry(isoEntry, result.ArchiveBytes);

        string? firstPath = null;
        foreach (var replacement in replacements)
        {
            var path = WriteWorkspaceFile(fsysName, replacement.Key, replacement.Value);
            firstPath ??= path;
        }

        return firstPath ?? isoEntry.Name;
    }

    private string WriteWorkspaceFile(string fsysName, string entryName, byte[] bytes)
    {
        var path = WorkspaceFsysEntryPath(fsysName, entryName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    private string WorkspaceFsysEntryPath(string fsysName, string entryName)
    {
        var archiveFolder = Path.GetFileNameWithoutExtension(SafeFileName(fsysName));
        if (string.IsNullOrWhiteSpace(archiveFolder))
        {
            archiveFolder = SafeFileName(fsysName);
        }

        return Path.Combine(WorkspaceDirectory, "Game Files", archiveFolder, SafeFileName(entryName));
    }

    private IsoWriteResult WriteIsoEntry(GameCubeIsoFileEntry entry, byte[] sourceBytes)
    {
        var maximumBytes = MaximumReplacementSize(entry);
        if (entry.TocEntryOffset is null && sourceBytes.Length > entry.Size)
        {
            throw new InvalidDataException($"{entry.Name} cannot grow because it does not have a normal FST size entry.");
        }

        var insertedBytes = 0;
        using (var stream = File.Open(Iso.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
        {
            insertedBytes = EnsureIsoCapacity(stream, entry, sourceBytes.Length, maximumBytes);
            maximumBytes = checked(maximumBytes + (uint)insertedBytes);

            stream.Position = entry.Offset;
            stream.Write(sourceBytes);

            if (entry.Size > sourceBytes.Length)
            {
                WriteZeros(stream, checked((int)(entry.Size - sourceBytes.Length)));
            }

            if (entry.TocEntryOffset is not null)
            {
                Span<byte> sizeBytes = stackalloc byte[4];
                BigEndian.WriteUInt32(sizeBytes, 0, checked((uint)sourceBytes.Length));
                stream.Position = entry.TocEntryOffset.Value + 8;
                stream.Write(sizeBytes);
            }
        }

        LoadedFiles[entry.Name] = sourceBytes;
        Iso = GameCubeIsoReader.Open(Iso.Path);
        return new IsoWriteResult(maximumBytes, insertedBytes);
    }

    private uint MaximumReplacementSize(GameCubeIsoFileEntry entry)
    {
        if (entry.TocEntryOffset is null)
        {
            return entry.Size;
        }

        var nextFile = Iso.Files
            .Where(file => file.Offset > entry.Offset)
            .OrderBy(file => file.Offset)
            .FirstOrDefault();
        var maxEnd = nextFile?.Offset ?? checked((uint)new FileInfo(Iso.Path).Length);
        return maxEnd <= entry.Offset ? entry.Size : maxEnd - entry.Offset;
    }

    private int EnsureIsoCapacity(FileStream stream, GameCubeIsoFileEntry entry, int sourceLength, uint currentMaximumBytes)
    {
        if (sourceLength <= currentMaximumBytes)
        {
            return 0;
        }

        if (entry.TocEntryOffset is null)
        {
            throw new InvalidDataException($"{entry.Name} cannot grow because it does not have a normal FST size entry.");
        }

        var insertedBytes = Align16(checked(sourceLength - (int)currentMaximumBytes));
        var insertOffset = checked((long)entry.Offset + entry.Size);
        InsertZeros(stream, insertOffset, insertedBytes);

        Span<byte> offsetBytes = stackalloc byte[4];
        foreach (var shiftedEntry in Iso.Files
            .Where(file => file.TocEntryOffset is not null && file.Offset > entry.Offset)
            .OrderBy(file => file.Offset))
        {
            BigEndian.WriteUInt32(offsetBytes, 0, checked(shiftedEntry.Offset + (uint)insertedBytes));
            stream.Position = shiftedEntry.TocEntryOffset!.Value + 4;
            stream.Write(offsetBytes);
        }

        UpdateUserDataSize(stream);
        return insertedBytes;
    }

    private static void InsertZeros(FileStream stream, long offset, int count)
    {
        if (count <= 0)
        {
            return;
        }

        var oldLength = stream.Length;
        if (offset < 0 || offset > oldLength)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), $"Offset 0x{offset:x} is outside the ISO.");
        }

        var buffer = new byte[1024 * 1024];
        stream.SetLength(oldLength + count);
        var readEnd = oldLength;
        while (readEnd > offset)
        {
            var readStart = Math.Max(offset, readEnd - buffer.Length);
            var length = checked((int)(readEnd - readStart));
            stream.Position = readStart;
            ReadExactly(stream, buffer.AsSpan(0, length));
            stream.Position = readStart + count;
            stream.Write(buffer, 0, length);
            readEnd = readStart;
        }

        stream.Position = offset;
        WriteZeros(stream, count);
    }

    private static void UpdateUserDataSize(FileStream stream)
    {
        const int userDataStartOffsetLocation = 0x434;
        const int userDataSizeLocation = 0x438;

        Span<byte> headerBytes = stackalloc byte[4];
        stream.Position = userDataStartOffsetLocation;
        ReadExactly(stream, headerBytes);
        var userDataStart = BigEndian.ReadUInt32(headerBytes, 0);
        if (userDataStart == 0 || userDataStart > stream.Length)
        {
            return;
        }

        BigEndian.WriteUInt32(headerBytes, 0, checked((uint)(stream.Length - userDataStart)));
        stream.Position = userDataSizeLocation;
        stream.Write(headerBytes);
    }

    private static void ReadExactly(Stream stream, Span<byte> buffer)
    {
        var read = 0;
        while (read < buffer.Length)
        {
            var count = stream.Read(buffer[read..]);
            if (count == 0)
            {
                throw new EndOfStreamException("Unexpected end of ISO while shifting file data.");
            }

            read += count;
        }
    }

    private static void WriteU16(byte[] bytes, int offset, int value)
        => BigEndian.WriteUInt16(bytes, offset, checked((ushort)ClampUInt16(value)));

    private static void WriteSingle(BinaryData data, int offset, float value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BigEndian.WriteUInt32(bytes, 0, unchecked((uint)BitConverter.SingleToInt32Bits(value)));
        data.WriteBytes(offset, bytes);
    }

    private static int ValueAt(IReadOnlyList<int> values, int index)
        => index < values.Count ? values[index] : 0;

    private static byte Flag(bool value) => value ? (byte)1 : (byte)0;

    private static byte ClampByte(int value)
        => checked((byte)Math.Clamp(value, 0, byte.MaxValue));

    private static int ClampUInt16(int value)
        => Math.Clamp(value, 0, ushort.MaxValue);

    private static ushort ClampUInt16ToU16(int value)
        => checked((ushort)ClampUInt16(value));

    private static uint ClampUInt32(int value)
        => checked((uint)Math.Max(0, value));

    private static byte ClampSignedByte(int value)
        => unchecked((byte)(sbyte)Math.Clamp(value, sbyte.MinValue, sbyte.MaxValue));

    private static int Align16(int value)
        => (value + 0x0f) & ~0x0f;

    private static void WriteZeros(Stream stream, int count)
    {
        Span<byte> zeros = stackalloc byte[4096];
        while (count > 0)
        {
            var length = Math.Min(count, zeros.Length);
            stream.Write(zeros[..length]);
            count -= length;
        }
    }

    private static string SafeFileName(string name)
    {
        var baseName = Path.GetFileName(name);
        if (!string.IsNullOrWhiteSpace(baseName))
        {
            name = baseName;
        }

        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray();
        return new string(chars);
    }

    private sealed record IsoWriteResult(uint MaximumBytes, int InsertedBytes);
}
