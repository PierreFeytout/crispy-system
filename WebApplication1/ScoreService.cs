namespace WebApplication1
{
    using Azure;
    using Azure.Data.Tables;

    public class ScoreService
    {
        private const string TableName = "scoreTable";
        private readonly TableServiceClient _tableServiceClient;

        public ScoreService(TableServiceClient tableServiceClient)
        {
            _tableServiceClient = tableServiceClient;
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
            return userEntity;
        }

        public async Task<IEnumerable<UserEntity>> GetScores()
        {
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

            return list.OrderByDescending(x => x.Score);
        }
    }
}
