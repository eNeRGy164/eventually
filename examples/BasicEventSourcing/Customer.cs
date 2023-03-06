using Eventually;

namespace BasicEventSourcing;

public class Customer : AggregateRoot
{
    private Customer(string id) : base(id)
    {
    }

    private Customer(string id, long version, IEnumerable<object> events) : base(id, version, events)
    {
    }

    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    protected override bool TryApplyEvent(object domainEvent)
    {
        switch (domainEvent)
        {
            case CustomerRegistered customerRegistered:
                Apply(customerRegistered);
                break;
            default:
                return false;
        }

        return true;
    }

    private void Apply(CustomerRegistered customerRegistered)
    {
        FirstName = customerRegistered.FirstName;
        LastName = customerRegistered.LastName;
    }

    public static Customer Register(string id, string firstName, string lastName)
    {
        var instance = new Customer(id);
        instance.Emit(new CustomerRegistered(id, firstName,lastName));

        return instance;
    }
}