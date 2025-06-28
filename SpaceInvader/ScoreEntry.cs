
namespace SpaceInvader;

using System.Text.Json.Serialization;

public class ScoreEntry
{
    [JsonPropertyName("userName")]
    public string UserName { get; set; }

    [JsonPropertyName("score")]
    public long Score { get; set; }
}
