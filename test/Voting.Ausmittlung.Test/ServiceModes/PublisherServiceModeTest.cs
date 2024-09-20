// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Core.Configuration;

namespace Voting.Ausmittlung.Test.ServiceModes;

public class PublisherServiceModeTest : BaseServiceModeTest<PublisherServiceModeTest.PublisherAppFactory>
{
    public PublisherServiceModeTest(PublisherAppFactory factory)
        : base(factory, ServiceMode.Publisher)
    {
    }

    public class PublisherAppFactory : ServiceModeAppFactory
    {
        public PublisherAppFactory()
            : base(ServiceMode.Publisher)
        {
        }
    }
}
