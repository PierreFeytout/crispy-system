namespace WebApplication1
{
    using Microsoft.Extensions.Caching.Memory;
    using WebApplication1.Grpc;

    public class ScoreService
    {
        private readonly TableService _tableService;
        private readonly IMemoryCache _memoryCache;
        private readonly HashSet<string> _cacheKeys = new();
        private readonly bool cacheInitialized = false;

        public ScoreService(TableService tableService, IMemoryCache memoryCache)
        {
            _tableService = tableService;
            _memoryCache = memoryCache;
        }

        public async Task<UserEntity> UpsertScore(UserEntity userEntity)
        {
            if (await _tableService.UpdateScoreAsync(userEntity))
            {
                _memoryCache.Set(userEntity.UserName, userEntity.Score);
                return userEntity;
            }

            await _tableService.AddScoreAsync(userEntity);

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

            var list = await _tableService.GetScoresAsync();
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