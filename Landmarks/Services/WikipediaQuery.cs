using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Landmarks.Services
{
    public class WikipediaQuery
    {
        public string Action { get; set; }
        public string Search { get; set; }
        public int Limit { get; set; }
        public int Namespace { get; set; }
        public string Format { get; set; }


        public WikipediaQuery(string query)
        {
            Action = "opensearch";
            Limit = 1;
            Namespace = 0;
            Format = "json";
            Search = query; 
        }

        public async Task<string> PerformQuery()
        {
            HttpClient client = HttpClientFactory.Create();

            var request = new HttpRequestMessage(HttpMethod.Get,
             $"https://en.wikipedia.org/w/api.php?action={Action}&search={Search}&limit={Limit}&namespace={Namespace}&format={Format}");

            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));


            var response = await client.SendAsync(request);
            var message = await response.Content.ReadAsStringAsync();
            var sub = message.Substring(message.IndexOf("]", StringComparison.CurrentCulture));
            var desc = sub.Substring(sub.IndexOf("\"", StringComparison.CurrentCulture) + 1, sub.IndexOf("http", 1, StringComparison.CurrentCulture) - sub.IndexOf("\"", StringComparison.CurrentCulture) - 6);
            return DecodeEncodedNonAsciiCharacters(desc);
        }

        private string DecodeEncodedNonAsciiCharacters(string value)
        {
            return Regex.Replace(
            value,
            @"\\u(?<Value>[a-zA-Z0-9]{4})",
            m => {
                return ((char)int.Parse(m.Groups["Value"].Value, NumberStyles.HexNumber)).ToString();
            });
        }
    }
}
