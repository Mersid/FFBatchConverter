namespace FFBatchConverter.Terminal;

[AttributeUsage(AttributeTargets.Property)]
public class ColumnNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}