﻿using Infra.Core.Time;

namespace Infra.Time
{
    public class TimeWrapper : ITimeWrapper
    {
        public DateTime Now => DateTime.Now;

        public DateTime UtcNow => DateTime.UtcNow;
    }
}
