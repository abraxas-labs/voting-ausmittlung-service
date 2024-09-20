// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Voting.Lib.VotingExports.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices;

public class ResultRenderServiceRegistry
{
    private readonly IDictionary<string, Type> _resultRenderServices = new Dictionary<string, Type>();

    internal void Add<TRenderer>(params TemplateModel[] templates)
        where TRenderer : IRendererService
    {
        foreach (var template in templates)
        {
            Add<TRenderer>(template);
        }
    }

    internal IRendererService? GetRenderService(string key, IServiceProvider serviceProvider)
    {
        return _resultRenderServices.TryGetValue(key, out var resultRenderServiceType)
            ? serviceProvider.GetService(resultRenderServiceType) as IRendererService
            : null;
    }

    private void Add<TRenderer>(TemplateModel template)
    {
        if (_resultRenderServices.ContainsKey(template.Key))
        {
            throw new ValidationException($"Result renderer for template key {template.Key} already registered");
        }

        _resultRenderServices.Add(template.Key, typeof(TRenderer));
    }
}
