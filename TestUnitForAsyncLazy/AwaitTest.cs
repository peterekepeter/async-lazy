using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestUnitForAsyncLazy
{
    [TestClass]
    public class AwaitTest
    {
        HttpClient HttpClient = new HttpClient();

        public async Task<String> DownloadStringV1(String url)
        {
            // good code
            var request = await HttpClient.GetAsync(url);
            var download = await request.Content.ReadAsStringAsync();
            return download;
        }

        public Task<String> DownloadStringV2(String url)
        {
            // okay code
            var request = HttpClient.GetAsync(url);
            var download = request.ContinueWith(http => http.Result.Content.ReadAsStringAsync());
            return download.Unwrap();
        }


        public String DownloadStringV3(String url)
        {
            // poor code
            var request = HttpClient.GetAsync(url).Result;
            var download = request.Content.ReadAsStringAsync().Result;
            return download;
        }

        public String DownloadStringV4(String url)
        {
            // BAD CODE
            return Task.Run(async () =>
            {
                var request = await HttpClient.GetAsync(url);
                var download = await request.Content.ReadAsStringAsync();
                return download;
            }).Result;
        }
        public String DownloadStringV5(String url)
        {
            // BAD CODE
            return Task.Run(() =>
            {
                var request = HttpClient.GetAsync(url).Result;
                var download = request.Content.ReadAsStringAsync().Result;
                return download;
            }).Result;
        }

        [TestMethod]
        public async Task ShouldDoTheSameThing()
        {
            var url = "https://www.google.com/robots.txt";
            var v1 = await DownloadStringV1(url);
            var v2 = await DownloadStringV2(url);
            var v3 = DownloadStringV3(url);
            var v4 = DownloadStringV4(url);
            var v5 = DownloadStringV5(url);
            v1.Should().Be(v2);
            v2.Should().Be(v3);
            v3.Should().Be(v4);
            v4.Should().Be(v5);
        }

    }
}
