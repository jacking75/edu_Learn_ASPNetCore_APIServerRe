using SampleServer.Models;

namespace SampleServer.Services;

public interface IGameService
{
    (ErrorCode, (Int64 uid, string token)) LoginUser(Int64 playerId, string token);
    (ErrorCode, string gameGuid) StartGame(Int64 uid);
}
