﻿using System;

namespace SeekU.Sql
{
    public class SnapshotDetail
    {
        public long Id { get; set; }
        public Guid AggregateRootId { get; set; }
        public long Version { get; set; }
        public string SnapshotData { get; set; }
    }
}