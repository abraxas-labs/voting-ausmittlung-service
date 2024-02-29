// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Ausmittlung.Data;
using Voting.Lib.Database.Models;

namespace System.Linq;

public static class CollectionExtensions
{
    /// <summary>
    /// Builds the difference between two collections and applies the differences to the first collection.
    /// </summary>
    /// <param name="items">The reference collection of items.</param>
    /// <param name="updated">The modified collection of items.</param>
    /// <param name="identitySelector">The id selector function.</param>
    /// <param name="updateAction">The update action to apply to modified items.</param>
    /// <param name="db">The data context.</param>
    /// <param name="createFactory">The creation function for new items.</param>
    /// <typeparam name="T">The type of the items.</typeparam>
    /// <typeparam name="TIdentity">The type of the item identity.</typeparam>
    public static void Update<T, TIdentity>(
       this ICollection<T> items,
       IEnumerable<T> updated,
       Func<T, TIdentity> identitySelector,
       Action<T, T> updateAction,
       DataContext db,
       Func<T, T>? createFactory = null)
       where T : BaseEntity
       where TIdentity : notnull
    {
        createFactory ??= x => x;
        var diff = items.BuildDiff(updated, identitySelector);

        foreach (var removed in diff.Removed)
        {
            items.Remove(removed);

            // this is necessary to ensure that the ef entity gets deleted.
            db.Entry(removed).State = Microsoft.EntityFrameworkCore.EntityState.Deleted;
        }

        foreach (var modified in diff.Modified)
        {
            var existingItem = items.Single(item => identitySelector(item).Equals(identitySelector(modified)));
            updateAction(existingItem, modified);
        }

        foreach (var added in diff.Added)
        {
            items.Add(createFactory(added));
        }
    }
}
