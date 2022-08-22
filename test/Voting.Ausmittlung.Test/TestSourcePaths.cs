// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.IO;
using System.Reflection;

namespace Voting.Ausmittlung.Test;

public static class TestSourcePaths
{
    public static readonly string TestProjectSourceDirectory = Path.Join(
        FindProjectSourceDirectory(),
        "test",
        "Voting.Ausmittlung.Test");

    private static string FindProjectSourceDirectory()
    {
        var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                  ?? throw new InvalidOperationException();

        do
        {
            if (Directory.GetFiles(dir, "*.sln", SearchOption.TopDirectoryOnly).Length > 0)
            {
                return dir;
            }

            dir = Path.GetDirectoryName(dir);
        }
        while (dir != null);

        throw new InvalidOperationException();
    }
}
