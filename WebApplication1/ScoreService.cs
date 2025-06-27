namespace WebApplication1
{
    using Azure;
    using Azure.Data.Tables;
    using Microsoft.Extensions.Caching.Memory;

    public class ScoreService
    {
        private const string TableName = "scoreTable";
        private readonly TableServiceClient _tableServiceClient;
        private readonly IMemoryCache _memoryCache;
        private readonly HashSet<string> _cacheKeys = new();
        private readonly bool cacheInitialized = false;

        public ScoreService(TableServiceClient tableServiceClient, IMemoryCache memoryCache)
        {
            _tableServiceClient = tableServiceClient;
            _memoryCache = memoryCache;
        }

        public async Task<UserEntity> UpsertScore(UserEntity userEntity)
        {
            await _tableServiceClient.CreateTableIfNotExistsAsync(TableName);
            var tableClient = _tableServiceClient.GetTableClient(TableName);

            var response = await tableClient.GetEntityIfExistsAsync<TableEntity>(userEntity.UserName, userEntity.UserName);

            if (response.HasValue)
            {
                response.Value["Score"] = userEntity.Score;
                await tableClient.UpdateEntityAsync(response.Value, ETag.All);
                return userEntity;
            }

            var entity = userEntity.CreateTableEntity();

            await tableClient.AddEntityAsync(entity);

            _memoryCache.Set(userEntity.UserName, userEntity.Score);
            _cacheKeys.Add(userEntity.UserName);
            return userEntity;
        }

        public async Task<IEnumerable<UserEntity>> GetScores()
        {
            if (cacheInitialized)
            {
                return _cacheKeys.Select(x => new UserEntity { UserName = x, Score = _memoryCache.Get<long>(x) }).OrderByDescending(x => x.Score);
            }

            await _tableServiceClient.CreateTableIfNotExistsAsync(TableName);

            var tableClient = _tableServiceClient.GetTableClient(TableName);

            var page = tableClient.QueryAsync<TableEntity>().GetAsyncEnumerator();

            var list = new List<UserEntity>();

            do
            {
                if (page.Current is not null)
                {
                    list.Add(UserEntity.CreateUserEntity(page.Current));
                }
            } while (await page.MoveNextAsync());

            var orderedScores = list.OrderByDescending(x => x.Score);

            foreach (var item in orderedScores)
            {
                _memoryCache.Set(item.UserName, item.Score);
                _cacheKeys.Add(item.UserName);
            }

            return orderedScores;
        }
    }
}
