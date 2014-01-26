﻿using System;
using System.Collections.Generic;
using System.Linq;
using ReflectionMagic;
using SeekU.Eventing;

namespace SeekU.Domain
{
    /// <summary>
    /// Base class for creating domain objects
    /// </summary>
    public abstract class AggregateRoot
    {
        private readonly List<DomainEvent> _appliedEvents = new List<DomainEvent>();
        private readonly List<Entity> _entities = new List<Entity>(); 

        /// <summary>
        /// Creates a new aggregate root with a new ID
        /// </summary>
        protected AggregateRoot() : this(SequentialGuid.NewId())
        { }

        /// <summary>
        /// Creates a new aggregate root
        /// </summary>
        /// <param name="id">Id of the root</param>
        protected AggregateRoot(Guid id)
        {
            Id = id;
        }
        
        /// <summary>
        /// Instance's identifier
        /// </summary>
        public Guid Id { get; protected internal set; }

        /// <summary>
        /// Current version of the instance
        /// </summary>
        public long Version { get; protected internal set; }

        /// <summary>
        /// Events that have been applied and await persistance
        /// </summary>
        public List<DomainEvent> AppliedEvents { get { return _appliedEvents; } } 

        /// <summary>
        /// Applies an event to the instance augmenting the current version
        /// for each event applied
        /// </summary>
        /// <param name="domainEvent">Event to apply</param>
        /// <param name="isNew">True if the event is new to the event stream; otherwise false</param>
        public void ApplyEvent(DomainEvent domainEvent, bool isNew = true)
        {
            Version++;

            if (isNew)
            {
                domainEvent.Sequence = Version;
                domainEvent.EventDate = DateTime.UtcNow;
            }

            // Call the apply method on the domain model instance
            if (!(domainEvent is DomainEntityEvent))
            {
                ApplyEventToSelf(domainEvent);
            }
            else
            {
                ApplyEventToEntities(domainEvent as DomainEntityEvent);
            }

            // Save the event for persistance if it's new
            if (isNew)
            {
                _appliedEvents.Add(domainEvent);
            }
        }

        /// <summary>
        /// Replays a stream of events to bring the instance up to the current
        /// state and version
        /// </summary>
        /// <param name="events">Stream of events to replay</param>
        public void ReplayEvents(IEnumerable<DomainEvent> events)
        {
            if(events == null)
            {
                return;
            }

            foreach (var domainEvent in events)
            {
                ApplyEvent(domainEvent, false);
            }
        }

        /// <summary>
        /// Associates an entity with the aggregate root
        /// </summary>
        /// <param name="entity">Entity to associate</param>
        internal void Associate(Entity entity)
        {
            if (!_entities.Contains(entity))
            {
                _entities.Add(entity);
            }
        }

        /// <summary>
        /// Applies a domain event to the current instance
        /// </summary>
        /// <param name="domainEvent">Event to apply</param>
        private void ApplyEventToSelf(DomainEvent domainEvent)
        {
            try
            {
                this.AsDynamic().Apply(domainEvent);
            }
            catch (ApplicationException)
            {
                // The aggregate root has no internal handler for the event.  No 
                // need to trow an error.
            }
        }

        /// <summary>
        /// Applies an entity event to the appropriate entity
        /// </summary>
        /// <param name="entityEvent">Entity event to apply</param>
        private void ApplyEventToEntities(DomainEntityEvent entityEvent)
        {
            var entity = _entities.FirstOrDefault(e => e.Id == entityEvent.EntityId);

            if (entity != null)
            {
                entity.AsDynamic().Apply(entityEvent);
            }
        }
    }
}