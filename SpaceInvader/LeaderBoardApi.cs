
namespace SpaceInvader;
using System.Net.Http.Json;

public class LeaderBoardApi(HttpClient httpClient)
{
    public void UpdateScore(string userName, long score)
    {
        var scoreEnty = new ScoreEntry
        {
            UserName = userName,
            Score = score
        };
        var result = httpClient.PostAsJsonAsync("/scores", scoreEnty, ScoreEntryJsonContext.Default.ScoreEntry);
        result.Wait();
    }

    public IEnumerable<ScoreEntry> GetScores()
    {
        try
        {
            var result = httpClient.GetFromJsonAsync("/scores", ScoreEntryJsonContext.Default.ListScoreEntry);

            result.Wait();
            return result.Result!;
        }
        catch
        {
            return [];
        }

    }
}
