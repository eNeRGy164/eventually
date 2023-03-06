namespace Eventually;

public record EventSourcedRecord(long Id, string AggregateId, DateTime Timestamp, long AggregateVersion, string EventType, string EventData);