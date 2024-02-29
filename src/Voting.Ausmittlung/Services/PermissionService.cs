// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1.Models;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Voting.Lib.Iam.Store;
using ServiceBase = Abraxas.Voting.Ausmittlung.Services.V1.PermissionService.PermissionServiceBase;

namespace Voting.Ausmittlung.Services;

public class PermissionService : ServiceBase
{
    private readonly IAuth _auth;

    public PermissionService(IAuth auth)
    {
        _auth = auth;
    }

    public override Task<Permissions> List(Empty request, ServerCallContext context)
    {
        return Task.FromResult(new Permissions
        {
            Permission = { _auth.Permissions },
        });
    }
}
