// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Voting.Ausmittlung.Test.Utils;

public static class HttpClientExtensions
{
    public static async Task<HttpResponseMessage> PostFiles(this HttpClient client, Uri uri, params (string Name, string Path)[] files)
    {
        using var content = new MultipartFormDataContent();

        foreach (var (name, path) in files)
        {
            var filePath = Path.Combine(TestSourcePaths.TestProjectSourceDirectory, path);
            var fileContent = new StreamContent(File.OpenRead(filePath));
            content.Add(fileContent, name, Path.GetFileName(filePath));
        }

        return await client.PostAsync(uri, content);
    }
}
