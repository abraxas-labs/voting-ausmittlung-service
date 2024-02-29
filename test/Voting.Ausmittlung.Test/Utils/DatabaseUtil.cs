// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Voting.Ausmittlung.Test.Utils;

public static class DatabaseUtil
{
    private static bool _migrated;

    public static void Truncate(params DbContext[] dbContexts)
    {
        // on the first run, we migrate the database to ensure the same structure as the "real" DB
        if (!_migrated)
        {
            foreach (var db in dbContexts)
            {
                db.Database.Migrate();
            }

            _migrated = true;
        }

        foreach (var db in dbContexts)
        {
            // truncating tables is much faster than recreating the database
            var tableNames = db.Model.GetEntityTypes().Select(m => $@"""{m.GetTableName()}""");
            db.Database.ExecuteSqlRaw($"TRUNCATE {string.Join(",", tableNames)} CASCADE");
        }
    }
}
