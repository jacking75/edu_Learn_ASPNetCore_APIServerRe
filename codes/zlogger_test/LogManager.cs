using System.Collections.Generic;
using Microsoft.Extensions.Logging;

public static class LogManager
{
    public enum EventType
    {
        CreateAccount = 101,
    }

    public static Dictionary<EventType, EventId> EventIdDic = new()
    {
        { EventType.CreateAccount, new EventId((int)EventType.CreateAccount, nameof(EventType.CreateAccount)) },
    };
}
