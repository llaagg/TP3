using TP3.Messages;

public class MessageHelper
{
    
    public static TP3Message ParseMessage(string message)
    {
        var parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            throw new ArgumentException("Message cannot be empty.", nameof(message));
        }
        
        if (!Enum.TryParse(parts[0], ignoreCase: true, out TP3Command command))
        {
            throw new ArgumentException($"Invalid command: {parts[0]}", nameof(message));
        }

        var tag = Guid.NewGuid().ToString("N");

        if (command == TP3Command.READ)
        {
            if (parts.Length < 2)
            {
                throw new ArgumentException("READ requires qid. Usage: read <qid> [offset] [maxBytes]", nameof(message));
            }

            var qid = parts[1];
            var offset = 0L;
            var maxBytes = 16 * 1024;

            if (parts.Length > 2 && !long.TryParse(parts[2], out offset))
            {
                throw new ArgumentException($"Invalid READ offset: {parts[2]}", nameof(message));
            }

            if (parts.Length > 3 && !int.TryParse(parts[3], out maxBytes))
            {
                throw new ArgumentException($"Invalid READ maxBytes: {parts[3]}", nameof(message));
            }

            return new TP3ReadRequest
            {
                Tag = tag,
                Qid = qid,
                Offset = offset,
                MaxBytes = maxBytes,
            };
        }

        var pathSegments = parts.Length > 1 ? parts[1..] : Array.Empty<string>();
        if (command == TP3Command.WALK)
        {
            return new TP3WalkRequest
            {
                Tag = tag
            };
        }

        throw new ArgumentException($"Unsupported command: {command}. Only WALK and READ are supported.", nameof(message));
    }
}