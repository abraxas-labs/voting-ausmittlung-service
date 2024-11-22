// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using FluentAssertions;
using Voting.Ausmittlung.Ech.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.UtilsTest;

public class DeliveryHeaderUtilsTest
{
    [Fact]
    public void ShouldEnrichComment()
    {
        var comment = "Staging";
        comment = DeliveryHeaderUtils.EnrichComment(comment, false);
        comment.Should().Be("Staging / Testphase");
    }

    [Fact]
    public void ShouldEnrichCommentWithTestingPhaseEnded()
    {
        var comment = "Staging";
        comment = DeliveryHeaderUtils.EnrichComment(comment, true);
        comment.Should().Be("Staging / Live");
    }
}
