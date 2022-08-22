// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Scheduler;

namespace Voting.Ausmittlung.Core.Configuration;

public class ResultExportJobConfig : JobConfig
{
    public ResultExportJobConfig()
    {
        Interval = TimeSpan.FromMinutes(1);
    }

    // currently we use a configurable language,
    // in future we may want to provide the exports in multiple languages
    // or configure the language on the domain of influence.
    public string Language { get; set; } = "de";
}
