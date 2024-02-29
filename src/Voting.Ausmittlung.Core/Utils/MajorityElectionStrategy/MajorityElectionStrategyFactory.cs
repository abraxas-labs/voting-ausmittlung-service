// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Utils.MajorityElectionStrategy;

public class MajorityElectionStrategyFactory
{
    private readonly Dictionary<(CantonMajorityElectionAbsoluteMajorityAlgorithm? AbsoluteMajorityAlgorithm, MajorityElectionMandateAlgorithm MandateAlgorithm), IMajorityElectionMandateAlgorithmStrategy> _mandateAlgorithmStrategies;

    public MajorityElectionStrategyFactory(IEnumerable<IMajorityElectionMandateAlgorithmStrategy> mandateAlgorithmStrategies)
    {
        _mandateAlgorithmStrategies = mandateAlgorithmStrategies.ToDictionary(x => (x.AbsoluteMajorityAlgorithm, x.MandateAlgorithm));
    }

    public IMajorityElectionMandateAlgorithmStrategy GetMajorityElectionMandateAlgorithmStrategy(CantonMajorityElectionAbsoluteMajorityAlgorithm absoluteMajorityAlgorithm, MajorityElectionMandateAlgorithm mandateAlgorithm)
    {
        return _mandateAlgorithmStrategies.TryGetValue((absoluteMajorityAlgorithm, mandateAlgorithm), out var strategy)
            ? strategy
            : _mandateAlgorithmStrategies[(null, mandateAlgorithm)];
    }
}
