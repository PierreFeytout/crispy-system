namespace WebApplication1;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using WebApplication1.Grpc;

[JsonSerializable(typeof(List<UserEntity>))]
[JsonSerializable(typeof(UserEntity))]
[JsonSerializable(typeof(ScoreTableEntry))]
public partial class ScoreEntryJsonContext : JsonSerializerContext
{
}