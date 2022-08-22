// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Core.Configuration;
using Voting.Ausmittlung.Test.ServiceModes;

namespace Voting.Basis.Test.ServiceModes;

public class EventProcessorServiceModeTest : BaseServiceModeTest<EventProcessorServiceModeTest.EventProcessorAppFactory>
{
    public EventProcessorServiceModeTest(EventProcessorAppFactory factory)
        : base(factory, ServiceMode.EventProcessor)
    {
    }

    public class EventProcessorAppFactory : ServiceModeAppFactory
    {
        public EventProcessorAppFactory()
            : base(ServiceMode.EventProcessor)
        {
        }
    }
}
