
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
        var result = httpClient.PostAsJsonAsync<ScoreEntry>("/scores", scoreEnty);
        result.Wait();
    }
    public IEnumerable<ScoreEntry> GetScores()
    {
        var result = httpClient.GetFromJsonAsync<IEnumerable<ScoreEntry>>("/scores");

        result.Wait();

        return result.Result!;
    }
}
