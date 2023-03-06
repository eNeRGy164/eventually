using BasicEventSourcing;
using Eventually;
using Microsoft.EntityFrameworkCore;

// Create a new DB context for the event store. You can use ANY database type that's supported by EF Core.
// I don't care, as long as you can store JSON serialized as strings ;-)
var dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase("test").Options;
var dbContext = new ApplicationDbContext(dbContextOptions);

// Make sure you've registered the events you're using.
// This is explicit, so you can version your events or rename types if you have to without breaking your aggregates.
var eventRegistry = new EventRegistry();
eventRegistry.Register<CustomerRegistered>("customers.registered.v1");

// Fire up the event store with your database context and the event registry.
// The event store will use the event registry to determine how to serialize and deserialize events.
// The database context is used to store the events.
var eventStore = new EventStore<ApplicationDbContext>(dbContext, eventRegistry);

// Create a new aggregate root and store it in the event store.
var customer = Customer.Register("123", "Test", "Test");
await eventStore.SaveAsync(customer);

// Load a saved aggregate root from the event store.
var loadedCustomer = await eventStore.Load<Customer>("123");
Console.WriteLine(loadedCustomer.FirstName);