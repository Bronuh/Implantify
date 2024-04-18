using Verse;

namespace Implantify;

internal static class LocalLog
{
    public static void Error(object message = null)
    {
#if DEBUG
        Log.Error(message is not null ? message.ToString() : "");
#endif
    }
    
    public static void Message(object message = null)
    {
#if DEBUG
        Log.Message(message is not null ? message.ToString() : "");
#endif
    }
}