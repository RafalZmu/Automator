namespace Automator.Core.Launcher;

/// <summary>
/// Pure rules for the tab bar: tabs are shown in rows of nine, selected with the 1 through 9 keys of the focused row.
/// A slot is a tab's stable module identity; a position is where the slot currently sits in the persisted order.
/// </summary>
public static class TabLayout
{
    public const int RowSize = 9;
    public const int MaximumSlots = 45;

    public static int RowCount(int tabCount) => Math.Max(1, (tabCount + RowSize - 1) / RowSize);

    /// <summary>Zero-based row that holds a zero-based position.</summary>
    public static int RowOfPosition(int position) => position / RowSize;

    /// <summary>
    /// Returns a permutation of slots 1 through <paramref name="slotCount"/>. The launcher (slot 1) stays first; requested slots
    /// that are out of range or repeated are dropped, and slots missing from the request keep their natural order at the end.
    /// </summary>
    public static IReadOnlyList<int> Normalize(IEnumerable<int>? requested, int slotCount)
    {
        var order = new List<int>(slotCount);
        var seen = new HashSet<int>();
        if (slotCount >= 1) { order.Add(1); seen.Add(1); }
        foreach (var slot in requested ?? [])
        {
            if (slot >= 1 && slot <= slotCount && seen.Add(slot)) order.Add(slot);
        }
        for (var slot = 1; slot <= slotCount; slot++)
        {
            if (seen.Add(slot)) order.Add(slot);
        }
        return order;
    }

    /// <summary>The slot at a one-based key (1 through 9) of a zero-based row, or null when that position holds no tab.</summary>
    public static int? SlotAt(IReadOnlyList<int> order, int row, int key)
    {
        if (row < 0 || key is < 1 or > RowSize) return null;
        var position = row * RowSize + key - 1;
        return position < order.Count ? order[position] : null;
    }

    public static int RowOfSlot(IReadOnlyList<int> order, int slot)
    {
        for (var index = 0; index < order.Count; index++)
        {
            if (order[index] == slot) return RowOfPosition(index);
        }
        return 0;
    }
}
