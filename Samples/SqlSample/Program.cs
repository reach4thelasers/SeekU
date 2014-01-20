﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SampleDomain.Commands;
using SampleDomain.Domain;
using SeekU;
using SeekU.Commanding;
using SeekU.Eventing;
using SeekU.Sql.Eventing;
using StructureMap;

namespace SqlSample
{
    class Program
    {
        static void Main(string[] args)
        {
            // Using StructureMap for IoC.  You can use Ninject, AutoFac, Windsor, or whatever
            // supports the methods you need to override in HostConfiguration<T>
            var config = new HostConfiguration<StructureMapResolver>();

            // Configure the host to use SQL to store events and snapshots.  You don't have to use
            // the configuration action - both providers will default to a connection string
            // named "SeekU."  This simply shows how you can configure each provider at runtime.
            config
                // Sample of using configuration actions to set connectionstrings
                .ForEventStore().Use<SqlEventStore>(store =>{store.ConnectionStringName = "MyConnectionString";})
                // This could be a different connection if necessary
                .ForSnapshotStore().Use<SqlSnapshotStore>(store =>{store.ConnectionStringName = "MyConnectionString";});

            // Using the dfault conenction string would look like this:
            //config.ForEventStore().Use<SqlEventStore>().ForSnapshotStore().Use<SqlSnapshotStore>();



            var host = new Host(config);
            var bus = host.GetCommandBus();

            // I'm not a proponent of Guids for primary keys.  This method returns
            // a sequential Guid to make database sorting behave like integers.
            // http://www.informit.com/articles/article.asp?p=25862
            var id = SequentialGuid.NewId();

            // Create the account
            bus.Send(new CreateNewAccountCommand(id, 950));

            // Use the account to create a history of events including a snapshot
            bus.Send(new DebitAccountCommand(id, 50));
            bus.Send(new CreditAccountCommand(id, 120));
            bus.Send(new DebitAccountCommand(id, 350));

            Console.Read();
        }
    }

    public class StructureMapResolver : IDependencyResolver
    {
        private readonly IContainer _container;

        public StructureMapResolver()
        {
            ObjectFactory.Initialize(x => x.Scan(scan =>
            {
                scan.TheCallingAssembly();
                scan.AssemblyContainingType<BankAccount>();
                scan.WithDefaultConventions();
                scan.ConnectImplementationsToTypesClosing(typeof(IHandleCommands<>));
                scan.ConnectImplementationsToTypesClosing(typeof(IHandleDomainEvents<>));
            }));

            _container = ObjectFactory.Container;
        }

        [DebuggerStepThrough]
        public T Resolve<T>()
        {
            return _container.GetInstance<T>();
        }

        public IEnumerable<T> ResolveAll<T>()
        {
            return _container.GetAllInstances<T>();
        }

        public IEnumerable<object> ResolveAll(Type type)
        {
            var instances = _container.GetAllInstances(type);

            return instances.Cast<object>();
        }

        public object Resolve(Type type)
        {
            return _container.GetInstance(type);
        }

        public void Register<T, K>()
            where T : class
            where K : T
        {
            _container.Configure(x => x.For<T>().Use<K>());
        }

        public void Register<T, TK>(Action<TK> configurationAction)
            where T : class
            where TK : T
        {
            _container.Configure(x => x.For<T>().Use<TK>().OnCreation(configurationAction));
        }

        public void Register<T>(T instance)
        {
            _container.Configure(x => x.For<T>().Use(instance));
        }

        public void Dispose()
        {
            _container.Dispose();
        }
    }
}
