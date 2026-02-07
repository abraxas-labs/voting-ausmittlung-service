// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Exceptions;

public class Ech0110RequiredException : Exception
{
    public Ech0110RequiredException()
    : base("eCH 0110 file and file name are required for eVoting eCH 0220 v1.0 imports.")
    {
    }
}
