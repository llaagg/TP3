namespace TP3.Messages;

public static class TP3ReadRequestReader
{
    public static IEnumerable<byte> ReadFrom( Func<IEnumerable<TP3ReadResponse>> responseProvider, ulong maxbytes = 0)
    {
        var offset = 0UL;
        foreach (var response in responseProvider())
        {
            var count = response.Data?.Length ?? 0;
            if (response.Data != null)
            {
                foreach (var b in response.Data)
                {
                    yield return b;
                }
            }

            offset += (ulong)count;
            if (maxbytes > 0 && offset >= maxbytes)
            {
                yield break;
            }
        }
    }
}

