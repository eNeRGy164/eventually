using System.Diagnostics.Tracing;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Eventually;

public class EventStore<TContext> where TContext : DbContext
{
    private TContext _dataContext;
    private readonly EventRegistry _eventRegistry;

    public EventStore(TContext dataContext, EventRegistry eventRegistry)
    {
        _dataContext = dataContext;
        _eventRegistry = eventRegistry;
    }

    /// <summary>
    /// Loads an aggregate from the event store.
    /// </summary>
    /// <param name="aggregateId">Identifier for the aggregate.</param>
    /// <typeparam name="TAggregate">Type of aggregate to load.</typeparam>
    /// <returns>Returns the loaded aggregate.</returns>
    public async Task<TAggregate> Load<TAggregate>(string aggregateId) where TAggregate : IAggregateRoot
    {
        var eventRecords = await _dataContext
            .Set<EventSourcedRecord>(typeof(TAggregate).Name)
            .Where(x => x.AggregateId == aggregateId)
            .OrderBy(x => x.AggregateVersion)
            .ToListAsync();

        var aggregateVersion = eventRecords.Max(x => x.AggregateVersion);

        var events = eventRecords.Select(eventRecord =>
        {
            var eventType = _eventRegistry.GetEventType(eventRecord.EventType);
            var eventData = JsonSerializer.Deserialize(eventRecord.EventData, eventType);

            return eventData;
        });

        // We don't really care if you put a private constructor for the rehydrate operation.
        // We'll give you the basic information and you have to take care of the rest.
        var constructor = typeof(TAggregate).GetConstructor(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            new[] { typeof(string), typeof(long), typeof(IEnumerable<object>) });

        if (constructor == null)
        {
            throw new InvalidOperationException(
                "Can't find a constructor that accepts the AggregateId, AggregateVersion and Events parameters.");
        }

        return (TAggregate)constructor.Invoke(new object[] { aggregateId, aggregateVersion, events });
    }

    /// <summary>
    /// Saves an aggregate to the event store.
    /// </summary>
    /// <param name="aggregate">Aggregate instance to save.</param>
    /// <typeparam name="TAggregate">Aggregate type to save.</typeparam>
    public async Task SaveAsync<TAggregate>(TAggregate aggregate) where TAggregate : IAggregateRoot
    {
        long currentVersion = 0L;

        if (await _dataContext.Set<EventSourcedRecord>(typeof(TAggregate).Name).AnyAsync(x => x.AggregateId == aggregate.Id))
        {
            currentVersion =
                await _dataContext.Set<EventSourcedRecord>(typeof(TAggregate).Name)
                    .Where(x => x.AggregateId == aggregate.Id)
                    .MaxAsync(x => x.AggregateVersion);
        }

        var eventRecords = new List<EventSourcedRecord>();

        foreach (var pendingEvent in aggregate.PendingDomainEvents)
        {
            var eventType = _eventRegistry.GetSchemaName(pendingEvent.GetType());
            var eventData = JsonSerializer.Serialize(pendingEvent);

            var record = new EventSourcedRecord(0L, aggregate.Id, DateTime.UtcNow,
                ++currentVersion, eventType, eventData);

            eventRecords.Add(record);
        }

        await _dataContext.Set<EventSourcedRecord>(typeof(TAggregate).Name).AddRangeAsync(eventRecords);
        await _dataContext.SaveChangesAsync();
    }
}

public interface IAggregateRoot
{
    public string Id { get; }
    public IReadOnlyCollection<object> PendingDomainEvents { get; }
    public void ClearPendingDomainEvents();
}