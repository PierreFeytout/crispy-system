namespace WebApplication1
{
    using WebApplication1.Grpc;
    using static WebApplication1.Grpc.LeaderboardService;

    public class LeaderBoardService : LeaderboardServiceBase
    {
        private readonly ScoreService _scoreService;

        public LeaderBoardService(ScoreService scoreService)
        {
            _scoreService = scoreService;
        }

        public override async Task<UpsertScoreResponse> UpsertScore(UpsertScoreRequest request, global::Grpc.Core.ServerCallContext context)
        {
            var score = await _scoreService.UpsertScore(request.User);

            return new UpsertScoreResponse { User = score };
        }


        public override async Task<GetScoresResponse> GetScores(GetScoresRequest request, global::Grpc.Core.ServerCallContext context)
        {
            var scores = await _scoreService.GetScores();

            var response = new GetScoresResponse();

            response.Users.AddRange(scores);

            Console.WriteLine(context.Peer);

            return response;
        }
    }
}
