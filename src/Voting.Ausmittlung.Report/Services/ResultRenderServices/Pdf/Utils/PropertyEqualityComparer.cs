// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;

public class PropertyEqualityComparer<T, TProp> : IEqualityComparer<T>
    where T : class
    where TProp : notnull
{
    private readonly Func<T, TProp> _propertySelector;

    public PropertyEqualityComparer(Func<T, TProp> propertySelector)
    {
        _propertySelector = propertySelector;
    }

    public bool Equals(T? x, T? y)
    {
        if (x == null || y == null)
        {
            return x == null && y == null;
        }

        var propX = _propertySelector(x);
        var propY = _propertySelector(y);
        return propX.Equals(propY);
    }

    public int GetHashCode(T obj) => _propertySelector(obj).GetHashCode();
}
