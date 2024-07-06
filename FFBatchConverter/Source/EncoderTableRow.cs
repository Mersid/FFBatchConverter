namespace FFBatchConverter;

public class EncoderTableRow
{
    [ColumnName("File Name")]
    public string FileName { get; set; }
    public string Duration { get; set; }
    public string Size { get; set; }
    public string Status { get; set; }
}