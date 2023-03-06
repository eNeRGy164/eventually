namespace Eventually;

/// <summary>
/// Implement this abstract class to create an aggregate root.
/// </summary>
public abstract class AggregateRoot : IAggregateRoot
{
    private readonly List<object> _pendingDomainEvents = new();

    /// <summary>
    /// Gets whether the aggregate state is being restored.
    /// </summary>
    protected bool IsRestoring { get; private set; }

    /// <summary>
    /// Gets the ID of the aggregate.
    /// </summary>
    public string Id { get; protected set; }

    /// <summary>
    /// Gets the current version of the aggregate.
    /// </summary>
    public long Version { get; protected set; }

    /// <summary>
    /// Gets the pending domain events.
    /// </summary>
    public IReadOnlyCollection<object> PendingDomainEvents => _pendingDomainEvents.AsReadOnly();

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRoot"/> class.
    /// </summary>
    /// <param name="id">ID of the aggregate.</param>
    protected AggregateRoot(string id)
    {
        Id = id;
        Version = 0L;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRoot"/> class.
    /// </summary>
    /// <param name="id">ID of the aggregate.</param>
    /// <param name="version">The version of the aggregate being restored.</param>
    /// <param name="events">The list of events that need to be replayed.</param>
    /// <exception cref="InvalidOperationException">Gets thrown when one of the events could not be replayed.</exception>
    protected AggregateRoot(string id, long version, IEnumerable<object> events)
    {
        Id = id;
        Version = version;
        IsRestoring = true;

        foreach (var pendingEvent in events)
        {
            if (!TryApplyEvent(pendingEvent))
            {
                throw new InvalidOperationException($"Can't restore aggregate state from event {pendingEvent}");
            }
        }

        IsRestoring = false;
    }

    /// <summary>
    /// Clears the pending domain events.
    /// </summary>
    public void ClearPendingDomainEvents()
    {
        _pendingDomainEvents.Clear();
    }

    /// <summary>
    /// Emits a new domain event to update the state of the aggregate.
    /// The emitted event is added to the pending domain events list if it gets handled successfully.
    /// </summary>
    /// <param name="domainEvent">Domain event to emit.</param>
    protected void Emit(object domainEvent)
    {
        if (TryApplyEvent(domainEvent))
        {
            _pendingDomainEvents.Add(domainEvent);
            Version++;
        }
    }

    /// <summary>
    /// Implement this method to handle emitted domain events.
    /// </summary>
    /// <param name="domainEvent">Domain event instance that was emitted.</param>
    /// <returns>Returns true when the event was handled. Otherwise false.</returns>
    protected abstract bool TryApplyEvent(object domainEvent);
}