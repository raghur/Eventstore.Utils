<Query Kind="Program">
  <Reference Relative="..\django\src\Worker\bin\Debug\CQRS.Engine.dll">E:\code\django\src\Worker\bin\Debug\CQRS.Engine.dll</Reference>
  <Reference Relative="..\django\src\Worker\bin\Debug\Django.Contracts.dll">E:\code\django\src\Worker\bin\Debug\Django.Contracts.dll</Reference>
  <Namespace>Django.Contracts.Commands</Namespace>
  <Namespace>Django.Contracts.Events</Namespace>
  <Namespace>Eventstore.Utils</Namespace>
</Query>

void Main()
{
	var config = Configuration.Local;
	var django = new EsBackEndBuilder("django")
					.WithEventStore(config.DjangoDB)
					.WithCommandSender(config.DjangoStorage,typeof(SimpleCommand))
					.Build();
					
	// filter by event type
	django.EventStore.Events<TenantCreated>().Dump();
	
	// optionally give a predicate
	django.EventStore.Events<TenantCreated>(t => t.IdP == "Google").Dump();
	
	// filter by time as well
	django.EventStore.Events<TenantCreated>(t => t.IdP == "Google", DateTime.UtcNow - TimeSpan.FromDays(-10)).Dump();
	
	// if you need events of different types (you could of course just do a union too)
	django.EventStore.EventMessages(null, typeof(TenantCreated), typeof(TenantSubscriptionAdded)).Dump();
	
	// the most basic version takes two predicates
		// a commit filter - 
		// an event filter.
		// date
	// It filters the commits and then filters events in the commit and returns events.
	
	// Similar extensions that operate on the Commit if you need that.
	
}

// Define other methods and classes here
