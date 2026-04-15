using CloudStructures;
using CloudStructures.Structures;

namespace RedisExampleServer.Services;

/// <summary>
/// Redis 연결을 관리하는 싱글톤 서비스.
/// CloudStructures의 RedisConnection을 감싸서 컨트롤러에서 직접 Redis 자료구조를 사용할 수 있도록 한다.
/// 참고: docs/02_레디스_연결_관리자_.md
/// </summary>
public class RedisService
{
    RedisConnection _connection = null!;

    public RedisConnection Connection => _connection;

    public void Init(string address)
    {
        var config = new RedisConfig("default", address);
        _connection = new RedisConnection(config);
    }

    // 편의 메서드: 키 이름으로 RedisString<T> 생성
    public RedisString<T> GetString<T>(string key)
    {
        return new RedisString<T>(_connection, key, null);
    }

    public RedisString<T> GetString<T>(string key, TimeSpan expiry)
    {
        return new RedisString<T>(_connection, key, expiry);
    }

    public RedisList<T> GetList<T>(string key)
    {
        return new RedisList<T>(_connection, key, null);
    }

    public RedisSet<T> GetSet<T>(string key)
    {
        return new RedisSet<T>(_connection, key, null);
    }

    public RedisSortedSet<T> GetSortedSet<T>(string key)
    {
        return new RedisSortedSet<T>(_connection, key, null);
    }
}
