using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ComponentBOMTool.Services
{
    public class MouserService
    {
        private readonly string apiKey;
        
        public MouserService(string apiKey)
        {
            this.apiKey = apiKey;
        }


        private static readonly HttpClient client = new HttpClient();

        // ✅ CACHE
        private static readonly ConcurrentDictionary<string, JObject> cache = new();

        // ✅ RATE LIMIT (20 calls / min)
        private static readonly SemaphoreSlim limiter = new SemaphoreSlim(1, 1);
        private static DateTime lastCall = DateTime.MinValue;
        private const int MinDelayMs = 3000;

        public async Task<JObject> SearchAsync(string partNumber)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new Exception("MOUSER_API_KEY is missing");

            if (cache.TryGetValue(partNumber, out var cached))
                return cached;

            JObject result = await ExecuteWithRetry(() => SearchByPart(partNumber));

            var parts = result?["SearchResults"]?["Parts"];

            // ✅ fallback
            if (parts == null || !parts.HasValues)
            {
                result = await ExecuteWithRetry(() => SearchByKeyword(partNumber));
            }

            cache[partNumber] = result;

            return result;
        }

        private async Task<JObject> ExecuteWithRetry(Func<Task<JObject>> action)
        {
            int retries = 3;

            for (int i = 0; i < retries; i++)
            {
                try
                {
                    await EnforceRateLimit();

                    return await action();
                }
                catch (Exception ex)
                {
                    if (i == retries - 1)
                        throw new Exception($"Mouser network error: {ex.Message}");

                    await Task.Delay((int)Math.Pow(2, i) * 1000);
                }
            }

            throw new Exception("Max retries exceeded");
        }

        private async Task EnforceRateLimit()
        {
            await limiter.WaitAsync();

            try
            {
                var now = DateTime.UtcNow;
                var diff = (now - lastCall).TotalMilliseconds;

                if (diff < MinDelayMs)
                    await Task.Delay(MinDelayMs - (int)diff);

                lastCall = DateTime.UtcNow;
            }
            finally
            {
                limiter.Release();
            }
        }

        private async Task<JObject> SearchByPart(string partNumber)
        {
            string url = $"https://api.mouser.com/api/v1/search/partnumber?apiKey={apiKey}";

            var payload = new
            {
                SearchByPartRequest = new { mouserPartNumber = partNumber }
            };

            return await Post(url, payload);
        }

        private async Task<JObject> SearchByKeyword(string partNumber)
        {
            string url = $"https://api.mouser.com/api/v1/search/keyword?apiKey={apiKey}";

            var payload = new
            {
                SearchByKeywordRequest = new { keyword = partNumber }
            };

            return await Post(url, payload);
        }

        private async Task<JObject> Post(string url, object payload)
        {
            var content = new StringContent(
                Newtonsoft.Json.JsonConvert.SerializeObject(payload),
                Encoding.UTF8,
                "application/json"
            );

            var resp = await client.PostAsync(url, content);

            var json = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode || json.Contains("TooManyRequests"))
                throw new Exception($"API Error: {resp.StatusCode}");

            return JObject.Parse(json);
        }
    }
}
