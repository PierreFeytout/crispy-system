namespace WebApplication1
{
    using System.Globalization;
    using WebApplication1.Grpc;

    /// <summary>
    /// Score table entry.
    /// </summary>
    public class ScoreTableEntry
    {
        /// <summary>
        /// Gets or sets the partition key.
        /// </summary>
        /// <value>
        /// The partition key.
        /// </value>
        public string PartitionKey { get; set; }

        /// <summary>
        /// Gets or sets the row key.
        /// </summary>
        /// <value>
        /// The row key.
        /// </value>
        public string RowKey { get; set; }

        /// <summary>
        /// Gets or sets the score.
        /// </summary>
        /// <value>
        /// The score.
        /// </value>
        public string Score { get; set; }

        public ScoreTableEntry()
        {

        }

        public ScoreTableEntry(UserEntity userEntity)
        {
            PartitionKey = userEntity.UserName;
            RowKey = userEntity.UserName;
            Score = userEntity.Score.ToString(CultureInfo.InvariantCulture);
        }


        public UserEntity ToEntity()
        {
            return new UserEntity
            {
                UserName = PartitionKey,
                Score = long.Parse(Score)
            };
        }
    }
}