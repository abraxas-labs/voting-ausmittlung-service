// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Exceptions;

public class EventOutOfOrderException(string message) : Exception(message);