#Using EventStore.Utils

Query event store easily with minimum fuss. Send commands, inspect events etc. Made for linqpad.

##Setup

1. Clone this repo
2. Compile it
3. copy everything from `bin/debug` into your linqpad extensions folder 

##Usage:
Get started with a `EsBackEndBuilder` object that sets up the EventStore, command sender, repository etc for you. Let's you specify configuration to use with a fluent api - so you can specify only the parts you need. 

```csharp
var config = Configuration.Local;
var django = new EsBackEndBuilder("django")
		.WithEventStore(config.DjangoDB)
                .WithCommandSender(config.DjangoStorage,typeof(SimpleCommand))
         	.Build();
```




###Querying the eventstore

1. Querying for events:

```csharp
    // filter by event type
    django.EventStore.Events<TenantCreated>().Dump();

    // optionally give a predicate
    django.EventStore.Events<TenantCreated>(t => t.IdP == "Google").Dump();
	
    // filter by time as well
    django.EventStore.Events<TenantCreated>(t => t.IdP == "Google",
    											 DateTime.UtcNow - TimeSpan.FromDays(-10))
    										.Dump();
```

**There are other extensions that return `Commit` objects as well if you need to operate on commits.**

####`Configuration` object: 

That is internal - just has strings/keys/other secrets per environment. 
For my coworkers at Aditi: 
You’ll need to copy everything in the extensions file `django/assets/RR_Extensions.FW40.linq` into your MyExtensions file in Linqpad