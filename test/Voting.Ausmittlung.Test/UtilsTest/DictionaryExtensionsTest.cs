// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Voting.Ausmittlung.Test.UtilsTest;

public class DictionaryExtensionsTest
{
    [Fact]
    public void TestGetOrAddAdd()
    {
        var d = new Dictionary<string, string>();
        d.GetOrAdd("foo", () => "bar")
            .Should()
            .Be("bar");
        d.Count.Should().Be(1);
        d["foo"].Should().Be("bar");
    }

    [Fact]
    public void TestGetOrAddUpdate()
    {
        var d = new Dictionary<string, string>
            {
                { "foo", "bar" },
            };
        d.GetOrAdd("foo", () => throw new InvalidOperationException())
            .Should()
            .Be("bar");
        d.Count.Should().Be(1);
        d["foo"].Should().Be("bar");
    }

    [Fact]
    public void TestAddOrUpdateAdd()
    {
        var d = new Dictionary<string, string>();
        d.AddOrUpdate("foo", () => "bar", _ => throw new InvalidOperationException())
            .Should()
            .Be("bar");
        d.Count.Should().Be(1);
        d["foo"].Should().Be("bar");
    }

    [Fact]
    public void TestAddOrUpdateUpdate()
    {
        var d = new Dictionary<string, string>
            {
                { "foo", "bar" },
            };
        d.AddOrUpdate("foo", () => throw new InvalidOperationException(), x => x + "-updated")
            .Should()
            .Be("bar-updated");
        d.Count.Should().Be(1);
        d["foo"].Should().Be("bar-updated");
    }
}
