// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using AutoMapper;
using AutoMapper.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Voting.Ausmittlung.Test.MockedData.Mapping;

// the TestMapper is separated into an own mapper class, that is only available inside the tests
// otherwise it could not be detected if there is a mapping configured in the tests which is also
// needed in the production code.
public class TestMapper
{
    private readonly IMapper _mapper;

    public TestMapper(IServiceProvider sp)
    {
        var configExpr = new MapperConfigurationExpression();
        AddExistingConfiguration(sp, configExpr);
        configExpr.AddMaps(typeof(TestMapper));

        // this should reduce memory usage
        // https://github.com/AutoMapper/AutoMapper/issues/2001#issuecomment-286924323
        // otherwise automapper uses huge memory
        // maybe this could be improved with https://jira.abraxas-tools.ch/jira/browse/VOTING-1560
        configExpr.ForAllPropertyMaps(_ => true, (_, m) => m.MapAtRuntime());
        var config = new MapperConfiguration(configExpr);
        _mapper = new Mapper(config, sp.GetService);
    }

    public T Map<T>(object? source)
        => _mapper.Map<T>(source);

    public object Map(object source, Type sourceType, Type destinationType)
        => _mapper.Map(source, sourceType, destinationType);

    private void AddExistingConfiguration(IServiceProvider sp, MapperConfigurationExpression config)
    {
        var existingConfig = sp.GetService<IConfigureOptions<MapperConfigurationExpression>>();
        existingConfig?.Configure(config);
    }
}
