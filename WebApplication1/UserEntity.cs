namespace WebApplication1
{
    using Azure.Data.Tables;

    /// <summary>
    /// User entity
    /// </summary>
    /// <seealso cref="System.IEquatable&lt;WebApplication1.UserEntity&gt;" />
    public class UserEntity
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public required string UserName { get; set; }

        /// <summary>
        /// Gets or sets the score.
        /// </summary>
        /// <value>
        /// The score.
        /// </value>
        public long Score { get; set; }

        public TableEntity CreateTableEntity()
        {
            return new TableEntity(UserName, UserName)
            {
                {nameof(Score), Score }
            };
        }

        public static UserEntity CreateUserEntity(TableEntity entity)
        {
            return new UserEntity
            {
                UserName = entity.PartitionKey,
                Score = entity.GetInt64(nameof(Score)) ?? 0,
            };
        }
    }
}
