namespace SpaceInvader;
using System.Collections.Generic;
using System.Text.Json.Serialization;

[JsonSerializable(typeof(List<ScoreEntry>))]
[JsonSerializable(typeof(ScoreEntry))]
public partial class ScoreEntryJsonContext : JsonSerializerContext
{
}