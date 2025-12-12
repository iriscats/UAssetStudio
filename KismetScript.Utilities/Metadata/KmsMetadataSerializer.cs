using System.Text.Json;
using System.Text.Json.Serialization;

namespace KismetScript.Utilities.Metadata;

/// <summary>
/// JSON serialization utilities for KmsMetadata.
/// </summary>
public static class KmsMetadataSerializer
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Serialize metadata to JSON string.
    /// </summary>
    public static string Serialize(KmsMetadata metadata)
    {
        return JsonSerializer.Serialize(metadata, DefaultOptions);
    }

    /// <summary>
    /// Deserialize metadata from JSON string.
    /// </summary>
    public static KmsMetadata? Deserialize(string json)
    {
        return JsonSerializer.Deserialize<KmsMetadata>(json, ReadOptions);
    }

    /// <summary>
    /// Write metadata to file.
    /// </summary>
    public static void WriteToFile(KmsMetadata metadata, string path)
    {
        var json = Serialize(metadata);
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// Read metadata from file.
    /// </summary>
    public static KmsMetadata? ReadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        return Deserialize(json);
    }
}
