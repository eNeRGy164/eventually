namespace Eventually;

public class EventRegistry
{
    private readonly Dictionary<string, Type> _eventSchemaToType = new();
    private readonly Dictionary<Type, string> _eventTypeToSchema = new();

    public void Register<T>(string schemaName)
    {
        if (_eventSchemaToType.ContainsKey(schemaName))
        {
            throw new ArgumentException("An event with the same name is already registered", nameof(schemaName));
        }

        _eventSchemaToType.Add(schemaName, typeof(T));
        _eventTypeToSchema.Add(typeof(T), schemaName);
    }

    public Type GetEventType(string schemaName)
    {
        if (!_eventSchemaToType.TryGetValue(schemaName, out var type))
        {
            throw new ArgumentException("No event with the specified name is registered", nameof(schemaName));
        }

        return type;
    }

    public string GetSchemaName(Type type)
    {
        if (!_eventTypeToSchema.TryGetValue(type, out var schemaName))
        {
            throw new ArgumentException("No event with the specified type is registered", nameof(type));
        }

        return schemaName;
    }
}