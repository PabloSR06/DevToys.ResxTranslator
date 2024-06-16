using Newtonsoft.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using TranslatorRESXDevToys.Models;

namespace TranslatorRESXDevToys.Services
{
    public class AzureTranslatorService
    {
        private readonly HttpClient _httpClient;
        private readonly string _endpoint = "https://api.cognitive.microsofttranslator.com";
        
        private readonly string _region;
        private readonly string _subscriptionKey;

        public AzureTranslatorService(string subscriptionKey, string region)
        {
            _httpClient = new HttpClient();
            _subscriptionKey = subscriptionKey;
            _region = region;
            
        }
        
        public async Task<string> Translator(string fromLanguage, string targetLanguage, string text)
        {
            string route = $"/translate?api-version=3.0&from={fromLanguage}&to={targetLanguage}";
            string result = text;

            object[] body = new object[] { new { Text = text } };
            var requestBody = JsonConvert.SerializeObject(body);

            HttpResponseMessage response;
            do
            {
                using (var request = new HttpRequestMessage())
                {
                    request.Method = HttpMethod.Post;
                    request.RequestUri = new Uri(_endpoint + route);
                    request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                    request.Headers.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);
                    request.Headers.Add("Ocp-Apim-Subscription-Region", _region);

                    response = await _httpClient.SendAsync(request).ConfigureAwait(false);
                }

                if (response.StatusCode == (System.Net.HttpStatusCode)429)
                {
                    Console.WriteLine("Too many requests. Waiting...");
                    await Task.Delay(60000);
                }

            } while (response.StatusCode == (System.Net.HttpStatusCode)429);

            var responseresult = await response.Content.ReadAsStringAsync();
            try
            {
                var translationList = JsonConvert.DeserializeObject<List<TranslationList>>(responseresult);

                result = translationList[0].Translations[0].Text;
            }
            catch (Exception ex)
            {
                Console.Write("Request error");
                Console.WriteLine(ex);
            }

            Console.WriteLine("True");
            return result;
        }
    }
}
