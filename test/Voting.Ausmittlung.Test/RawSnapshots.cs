// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.IO;
using Voting.Lib.Testing.Utils;

namespace Voting.Ausmittlung.Test;

public static class RawSnapshots
{
    public static void MatchRawTextSnapshot(this string content, params string[] pathSegments)
    {
        var path = Path.Join(TestSourcePaths.TestProjectSourceDirectory, Path.Join(pathSegments));

#if UPDATE_SNAPSHOTS
        var updateSnapshot = true;
#else
        var updateSnapshot = false;
#endif
        content.MatchRawSnapshot(path, updateSnapshot);
    }
}
