// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Utils;

public sealed class DisposableWrapper : IDisposable
{
    private readonly Action _dispose;

    private DisposableWrapper(Action dispose) => _dispose = dispose;

    public static IDisposable Wrap(Action onDispose) => new DisposableWrapper(onDispose);

    public void Dispose() => _dispose();
}
