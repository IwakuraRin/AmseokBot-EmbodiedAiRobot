using System.Text.Json.Serialization;

namespace NasForWindows.PluginSdk;

public sealed record PluginManifest(
    string SchemaVersion,
    string Id,
    string Name,
    string Version,
    string MinHostApiVersion,
    IReadOnlyList<PluginNavigationItem> Navigation,
    IReadOnlyList<PluginPageDefinition> Pages);

public sealed record PluginNavigationItem(string Title, string PageId, string? Permission);

public sealed record PluginPageDefinition(string Id, string Title, PluginPageType Type);

[JsonConverter(typeof(JsonStringEnumConverter<PluginPageType>))]
public enum PluginPageType
{
    [JsonStringEnumMemberName("data-table")]
    DataTable,

    [JsonStringEnumMemberName("detail")]
    Detail,

    [JsonStringEnumMemberName("form")]
    Form,

    [JsonStringEnumMemberName("dashboard")]
    Dashboard,
}
