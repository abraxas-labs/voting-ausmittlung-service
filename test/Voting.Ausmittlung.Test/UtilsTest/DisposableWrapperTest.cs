// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using FluentAssertions;
using Voting.Ausmittlung.Core.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.UtilsTest;

public class DisposableWrapperTest
{
    [Fact]
    public void ShouldCallDispose()
    {
        var called = false;
        var wrapper = DisposableWrapper.Wrap(() => called = true);
        called.Should().BeFalse();
        wrapper.Dispose();
        called.Should().BeTrue();
    }
}
