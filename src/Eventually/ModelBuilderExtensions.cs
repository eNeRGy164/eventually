using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Eventually;

public static class ModelBuilderExtensions
{
    /// <summary>
    /// Configures an event sourced table for a specific aggregate type.
    /// </summary>
    /// <param name="modelBuilder">Model builder instance to use.</param>
    /// <typeparam name="T">Type of aggregate to configure.</typeparam>
    public static void EventSourcedEntity<T>(this ModelBuilder modelBuilder)
    {
        var entityConfiguration = modelBuilder.SharedTypeEntity<EventSourcedRecord>(typeof(T).Name);

        entityConfiguration.Property(x => x.EventType).HasMaxLength(500).IsRequired();
        entityConfiguration.Property(x => x.AggregateId).HasMaxLength(500).IsRequired();
        entityConfiguration.Property(x => x.EventData).IsRequired();
        
        entityConfiguration.HasIndex(x => new { x.AggregateId, x.AggregateVersion }).IsUnique();
    }
}