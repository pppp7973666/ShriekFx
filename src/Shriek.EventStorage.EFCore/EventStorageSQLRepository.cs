﻿using System;
using System.Collections.Generic;
using System.Linq;
using Shriek.Events;
using Shriek.EventSourcing;
using Shriek.Storage;

namespace Shriek.EventStorage.EFCore
{
    public class EventStorageSQLRepository : IEventStorageRepository
    {
        private EventStorageSQLContext context;

        public EventStorageSQLRepository(EventStorageSQLContext context)
        {
            this.context = context;
        }

        public IEnumerable<StoredEvent> All(Guid aggregateId)
        {
            return context.Set<StoredEvent>().Where(e => e.AggregateId == aggregateId);
        }

        public void Dispose()
        {
            context.Dispose();
        }

        public Event GetLastEvent(Guid aggregateId)
        {
            return context.Set<StoredEvent>().Where(e => e.AggregateId == aggregateId).OrderBy(e => e.Timestamp).LastOrDefault();
        }

        public void Store(StoredEvent theEvent)
        {
            context.Set<StoredEvent>().Add(theEvent);
            context.SaveChanges();
        }
    }
}