﻿using System;

namespace Zen.Web.Data.Controller.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class Behavior : Attribute
    {
        public Type SummaryType { get; set; }
        public bool MustPaginate { get; set; }
        public bool CacheResults { get; set; }
    }
}