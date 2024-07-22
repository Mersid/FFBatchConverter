using System.Text.Json.Serialization;

namespace FFBatchConverter.Avalonia;

// https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation?pivots=dotnet-8-0
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Settings))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
    
}