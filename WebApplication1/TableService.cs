namespace WebApplication1;
using System.Net.Http.Json;
using System.Text.Json;
using WebApplication1.Grpc;

public class TableService
{
    private readonly HttpClient _http;
    private readonly string _accountName;
    private readonly string _tableName;
    private readonly string _sasToken; // NOTE: This should start with "?"

    public TableService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _accountName = config.GetValue<string>("StorageAccountName");
        _tableName = config.GetValue<string>("TableName");
        _sasToken = config.GetValue<string>("TableSasToken");

        // Set static headers once
        _http.DefaultRequestHeaders.Remove("Accept");
        _http.DefaultRequestHeaders.Add("Accept", "application/json;odata=nometadata");
        _http.DefaultRequestHeaders.Remove("DataServiceVersion");
        _http.DefaultRequestHeaders.Add("DataServiceVersion", "3.0");
        _http.DefaultRequestHeaders.Remove("MaxDataServiceVersion");
        _http.DefaultRequestHeaders.Add("MaxDataServiceVersion", "3.0");
    }

    private void SetDynamicHeaders(bool forUpdate = false)
    {
        var date = DateTime.UtcNow.ToString("R");
        _http.DefaultRequestHeaders.Remove("x-ms-date");
        _http.DefaultRequestHeaders.Add("x-ms-date", date);

        if (forUpdate)
        {
            _http.DefaultRequestHeaders.Remove("If-Match");
            _http.DefaultRequestHeaders.Add("If-Match", "*");
        }
        else
        {
            _http.DefaultRequestHeaders.Remove("If-Match");
        }
    }

    public async Task<bool> AddScoreAsync(UserEntity entry)
    {
        var url = $"https://{_accountName}.table.core.windows.net/{_tableName}?{_sasToken}";
        SetDynamicHeaders();

        var content = JsonContent.Create(new ScoreTableEntry(entry), ScoreEntryJsonContext.Default.ScoreTableEntry);
        var response = await _http.PostAsync(url, content);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateScoreAsync(UserEntity entry)
    {
        // Table Storage expects PK and RK in entity key format for updates/merges
        var entityUrl = $"https://{_accountName}.table.core.windows.net/{_tableName}(PartitionKey='{entry.UserName}',RowKey='{entry.UserName}')?{_sasToken}";
        SetDynamicHeaders(forUpdate: true);

        var content = JsonContent.Create(new ScoreTableEntry(entry), ScoreEntryJsonContext.Default.ScoreTableEntry);
        var request = new HttpRequestMessage(new HttpMethod("MERGE"), entityUrl)
        {
            Content = content
        };
        var response = await _http.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<UserEntity>> GetScoresAsync()
    {
        var url = $"https://{_accountName}.table.core.windows.net/{_tableName}()?{_sasToken}";
        SetDynamicHeaders();

        var response = await _http.GetAsync(url);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);

        var entries = new List<UserEntity>();
        if (doc.RootElement.TryGetProperty("value", out var array))
        {
            foreach (var el in array.EnumerateArray())
            {
                var item = el.Deserialize(ScoreEntryJsonContext.Default.ScoreTableEntry);
                if (item != null)
                    entries.Add(item.ToEntity());
            }
        }
        return entries;
    }
}