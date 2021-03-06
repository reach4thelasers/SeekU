﻿using System;
using SampleDomain;
using SampleDomain.Commands;
using SampleDomain.Domain;
using SeekU;
using SeekU.Commanding;
using SeekU.Eventing;
using SeekU.FileIO.Eventing;
using SeekU.StructureMap;
using StructureMap;
using StructureMap.Graph;

namespace TextFileSample
{
    class Program
    {
        static void Main(string[] args)
        {
            // Using StructureMap for IoC.  You can use Ninject, AutoFac, Windsor, or whatever
            // supports the methods you need to override in SeekUHostConfiguration<T>
            var host = new SeekUHostConfiguration<SeekUDemoDependencyResolver>();

            // Configure file-based event storeage
            host.ForEventStore().Use<JsonFileEventStore>()
                // Example of using an optional configuration action.  In this cas it sets the snapshot store's file name
                .ForSnapshotStore().Use<JsonFileSnapshotStore>(store =>
                {
                    store.FileName = "snapshot-instance.json";
                });
            
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

    public class SeekUDemoDependencyResolver : SeekUStructureMapResolver
    {
        public SeekUDemoDependencyResolver()
        {
            Container.Configure(x => x.Scan(scan =>
            {
                scan.TheCallingAssembly();
                scan.AssemblyContainingType<BankAccount>();
                scan.WithDefaultConventions();
                scan.ConnectImplementationsToTypesClosing(typeof(IHandleCommands<>));
                scan.ConnectImplementationsToTypesClosing(typeof(IHandleDomainEvents<>));
            }));

            Container = ObjectFactory.Container;
        }
    }
}
