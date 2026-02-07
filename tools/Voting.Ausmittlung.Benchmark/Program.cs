// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
