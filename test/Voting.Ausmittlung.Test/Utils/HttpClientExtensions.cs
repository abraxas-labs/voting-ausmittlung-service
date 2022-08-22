// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Voting.Ausmittlung.Test.Utils;

public static class HttpClientExtensions
{
    public static async Task<HttpResponseMessage> PostFile(this HttpClient client, Uri uri, string path)
    {
        var filePath = Path.Combine(TestSourcePaths.TestProjectSourceDirectory, path);
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(File.OpenRead(filePath));
        content.Add(fileContent, "file", Path.GetFileName(filePath));
        return await client.PostAsync(uri, content);
    }
}
