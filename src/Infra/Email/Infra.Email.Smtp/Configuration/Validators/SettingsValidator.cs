﻿using System;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable CA2208

namespace Infra.Email.Smtp.Configuration.Validators
{
    public static class SettingsValidator
    {
        public static bool TryValidate(Settings settings, out AggregateException validationExceptions)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var exceptions = new List<Exception>();

            if (string.IsNullOrWhiteSpace(settings.Host))
                exceptions.Add(new ArgumentNullException(nameof(settings.Host)));

            if (settings.Port is < 0 or > 65535)
                exceptions.Add(new ArgumentOutOfRangeException(nameof(settings.Port)));

            validationExceptions = new AggregateException(exceptions);

            return !exceptions.Any();
        }
    }
}
