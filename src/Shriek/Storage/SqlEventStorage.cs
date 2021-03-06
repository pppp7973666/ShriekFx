﻿using System.Linq;
using System.Collections.Concurrent;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Shriek.Domains;
using Shriek.Events;
using Shriek.EventSourcing;

namespace Shriek.Storage
{
    public class SqlEventStorage : IEventStorage
    {
        private readonly IEventStorageRepository _eventStoreRepository;
        private ConcurrentDictionary<Guid, ConcurrentBag<Event>> _events;

        public SqlEventStorage(IEventStorageRepository eventStoreRepository)
        {
            _eventStoreRepository = eventStoreRepository;
            _events = new ConcurrentDictionary<Guid, ConcurrentBag<Event>>();
        }

        public IEnumerable<Event> GetEvents(Guid aggregateId)
        {
            _events.TryGetValue(aggregateId, out var events);
            if (events == null)
            {
                var storeEvents = _eventStoreRepository.All(aggregateId);
                _events[aggregateId] = new ConcurrentBag<Event>();

                foreach (var e in storeEvents)
                {
                    var eventType = Type.GetType(e.EventType);
                    _events[aggregateId].Add(JsonConvert.DeserializeObject(e.Data, eventType) as Event);
                }
            }
            return _events[aggregateId].OrderBy(x => x.Timestamp);
        }

        public Event GetLastEvent(Guid aggregateId)
        {
            return GetEvents(aggregateId).LastOrDefault();
        }

        public void Save<T>(T theEvent) where T : Event
        {
            _events.TryGetValue(theEvent.AggregateId, out var events);
            if (events == null)
                _events[theEvent.AggregateId] = new ConcurrentBag<Event>();

            _events[theEvent.AggregateId].Add(theEvent);

            var serializedData = JsonConvert.SerializeObject(theEvent);

            var storedEvent = new StoredEvent(
                theEvent,
                serializedData,
                ""
                );

            _eventStoreRepository.Store(storedEvent);
        }

        public void SaveAggregateRoot<TAggregateRoot>(TAggregateRoot aggregate) where TAggregateRoot : IAggregateRoot, IEventProvider
        {
            var uncommittedChanges = aggregate.GetUncommittedChanges();
            var version = aggregate.Version;

            foreach (var @event in uncommittedChanges)
            {
                version++;
                @event.Version = version;
                Save(@event);
            }
        }

        public TAggregateRoot Source<TAggregateRoot>(Guid aggregateId) where TAggregateRoot : IAggregateRoot, IEventProvider, new()
        {
            var obj = new TAggregateRoot();
            var events = GetEvents(aggregateId);
            //重现历史更改
            obj.LoadsFromHistory(events);
            return obj;
        }
    }
}