using System;
using System.Reflection;
using CommonDomain;
using CommonDomain.Persistence;

namespace Eventstore.Utils
{
    public class AggregateFactory : IConstructAggregates
    {
        public IAggregate Build(Type type, Guid id, IMemento snapshot)
        {
            var types = snapshot == null ? new[] { typeof(Guid) } : new[] { typeof(Guid), typeof(IMemento) };
            var args = snapshot == null ? new object[] { id } : new object[] { id, snapshot };

            ConstructorInfo constructor = type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, types, null);

            return constructor.Invoke(args) as IAggregate;
        }
    }
}