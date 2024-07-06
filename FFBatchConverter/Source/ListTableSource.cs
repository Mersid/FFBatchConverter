using System.ComponentModel;
using System.Reflection;
using Terminal.Gui;

namespace FFBatchConverter;

public class ListTableSource<TRowType> : ITableSource
{
    private List<TRowType> RowList { get; set; } = [];
    public string[] ColumnNames { get; }
    public int Columns { get; }

    public object? this[int row, int col] => GetCell(row, col);

    public int Rows => RowList.Count;

    public ListTableSource()
    {
        PropertyInfo[] properties = typeof(TRowType).GetProperties();
        ColumnNames = properties.Select(p =>
        {
            // If the property has a ColumnNameAttribute attribute, use that as the column name.
            // Otherwise, use the property name.
            ColumnNameAttribute? displayName = p.GetCustomAttribute<ColumnNameAttribute>();

            return displayName is not null ? displayName.Name : p.Name;
        }).ToArray();

        Columns = ColumnNames.Length;
    }

    public void Add(TRowType row)
    {
        RowList.Add(row);
    }

    public void AddRange(IEnumerable<TRowType> rows)
    {
        RowList.AddRange(rows);
    }

    private object? GetCell(int row, int column)
    {
        TRowType rowData = RowList[row];
        PropertyInfo property = typeof(TRowType).GetProperties()[column];
        return property.GetValue(rowData);
    }
}