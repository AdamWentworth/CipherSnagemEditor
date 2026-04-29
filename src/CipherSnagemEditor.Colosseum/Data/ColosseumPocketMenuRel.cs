using CipherSnagemEditor.Core.Binary;
using CipherSnagemEditor.Core.Relocation;

namespace CipherSnagemEditor.Colosseum.Data;

public sealed class ColosseumPocketMenuRel
{
    private const int MartItemsIndex = 2;
    private const int NumberOfMartItemsIndex = 3;

    private readonly BinaryData _data;
    private readonly RelocationTable _relocationTable;

    private ColosseumPocketMenuRel(byte[] bytes, RelocationTable relocationTable)
    {
        _data = new BinaryData(bytes.ToArray());
        _relocationTable = relocationTable;
    }

    public static ColosseumPocketMenuRel Parse(byte[] bytes)
        => new(bytes, RelocationTable.Parse(bytes));

    public byte[] ToArray() => _data.ToArray();

    public int RandomizeShops(IReadOnlyList<ColosseumItem> items)
    {
        var itemPool = EligibleShopItems(items).ToArray();
        var pokeballPool = itemPool.Where(item => item.Index <= 12).ToArray();
        if (itemPool.Length == 0 || pokeballPool.Length == 0)
        {
            return 0;
        }

        var itemsByIndex = items.ToDictionary(item => item.Index);
        var itemCount = _relocationTable.GetValueAtPointer(NumberOfMartItemsIndex);
        var itemOffset = _relocationTable.GetPointer(MartItemsIndex);
        if (itemCount <= 0 || itemOffset < 0 || itemOffset + (itemCount * 2) > _data.Length)
        {
            return 0;
        }

        var changed = 0;
        var isFirstItemOfShop = true;
        for (var index = 0; index < itemCount; index++)
        {
            var offset = itemOffset + (index * 2);
            var scriptItemId = _data.ReadUInt16(offset);
            if (scriptItemId == 0)
            {
                isFirstItemOfShop = true;
                continue;
            }

            var itemId = NormalizeScriptItemId(scriptItemId, items.Count);
            if (!itemsByIndex.TryGetValue(itemId, out var currentItem) || !IsEligibleShopItem(currentItem))
            {
                continue;
            }

            var pool = isFirstItemOfShop ? pokeballPool : itemPool;
            var replacement = pool[Random.Shared.Next(pool.Length)];
            _data.WriteUInt16(offset, checked((ushort)ScriptItemId(replacement.Index)));
            isFirstItemOfShop = false;
            changed++;
        }

        return changed;
    }

    private static IEnumerable<ColosseumItem> EligibleShopItems(IEnumerable<ColosseumItem> items)
        => items
            .Where(item => item.Index > 0)
            .Where(IsEligibleShopItem);

    private static bool IsEligibleShopItem(ColosseumItem item)
        => item.BagSlotId < 5 && item.Price > 0;

    internal static int NormalizeScriptItemId(int itemId, int itemCount)
        => itemId > itemCount && itemId < 0x250 ? itemId - 151 : itemId;

    internal static int ScriptItemId(int itemId)
        => itemId >= 0x15e ? itemId + 151 : itemId;
}
