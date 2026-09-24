using System;

namespace Hypnonema.Shared.Media;

public class QueueLimitExceededException : Exception
{
    public QueueLimitExceededException(int limit)
        : base($"The queue limit of {limit} has been reached.")
    {
    }
}