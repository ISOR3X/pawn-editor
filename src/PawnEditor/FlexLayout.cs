using System.Collections.Generic;
using System.Linq;

public interface IFlexItem
{
    float Width { get; }
    int Priority { get; }
    float Grow { get; } // 0 = no grow, >0 = proportional grow like CSS flex-grow
}

public static class FlexLayout
{
    public const float ColumnGap = 8f;
    public const float RowGap = 8f;
    
    /// <summary>
    /// Computes the layout of items by grouping them into rows based on their widths
    /// and ensuring the total width of items in each row does not exceed the available space.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the items to be arranged. Must implement the <see cref="IFlexItem"/> interface.
    /// </typeparam>
    /// <param name="items">
    /// A list of items to be arranged into rows.
    /// </param>
    /// <returns>
    /// A list of rows, where each row is a list of items that fit within the available width.
    /// </returns>
    public static List<List<T>> ComputeLayout<T>(List<T> items) where T : IFlexItem
    {
        var rows = new List<List<T>>();
        var currentRow = new List<T>();
        var currentRowWidth = 0f;

        foreach (var item in items.OrderBy(i => i.Priority))
        {
            if (currentRowWidth + item.Width > 1f + float.Epsilon)
            {
                rows.Add(currentRow);
                currentRow = [];
                currentRowWidth = 0f;
            }

            currentRow.Add(item);
            currentRowWidth += item.Width;
        }

        if (currentRow.Count > 0)
            rows.Add(currentRow);

        return rows;
    }


    /// <summary>
    /// Resolves the widths of items in a row based on their initial widths, growth factors,
    /// and the available remaining space. Distributes extra space proportionally, according
    /// to the growth values of the items.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the items in the row. Must implement the <see cref="IFlexItem"/> interface.
    /// </typeparam>
    /// <param name="row">
    /// A list of items in a row for which the widths need to be resolved.
    /// </param>
    /// <param name="totalWidth">The total width to divide the items over</param>
    /// <param name="columnGap">
    /// The gap between each item in the row.
    /// </param>
    /// <returns>
    /// An array of floats representing the resolved widths of each item in the row.
    /// </returns>
    public static float[] ResolveWidths<T>(List<T> row, float totalWidth, float columnGap = 0f) where T : IFlexItem
    {
        var widths = new float[row.Count];
        var totalGapWidth = columnGap * (row.Count - 1);
        var availableWidth = totalWidth - totalGapWidth;
        var remainingSpace = availableWidth - row.Sum(i => i.Width * availableWidth);
        var totalGrow = row.Sum(i => i.Grow);

        for (var i = 0; i < row.Count; i++)
        {
            var growExtra = totalGrow > 0f
                ? (row[i].Grow / totalGrow) * remainingSpace
                : 0f;
            widths[i] = row[i].Width * availableWidth + growExtra;
        }

        return widths;
    }
}