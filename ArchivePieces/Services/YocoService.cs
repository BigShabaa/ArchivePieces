using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ArchivePieces.Services
{
    public class YocoCheckoutResponse
    {
        public string Id { get; set; } = string.Empty;
        public string RedirectUrl { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class YocoService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;

        public YocoService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        public async Task<YocoCheckoutResponse?> CreateCheckoutAsync(
            decimal amountRands,
            string orderNumber,
            string successUrl,
            string cancelUrl)
        {
            var secretKey = _config["Yoco:SecretKey"];

            // Yoco requires amount in CENTS as integer
            var amountCents = (int)(amountRands * 100);

            var body = new
            {
                amount = amountCents,
                currency = "ZAR",
                successUrl = successUrl,
                cancelUrl = cancelUrl,
                metadata = new { orderNumber }
            };

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://payments.yoco.com/api/checkouts");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", secretKey);

            request.Content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json");

            var response = await _http.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode) return null;

            return JsonSerializer.Deserialize<YocoCheckoutResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }
}
