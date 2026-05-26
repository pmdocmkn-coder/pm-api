using System.Text.Json;
using Pm.DTOs;

namespace Pm.Services
{
    public interface ISihepiIntegrationService
    {
        Task<List<SihepiTicketDto>> GetTicketsAsync();
    }

    public class SihepiIntegrationService : ISihepiIntegrationService
    {
        private readonly HttpClient _httpClient;

        public SihepiIntegrationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<SihepiTicketDto>> GetTicketsAsync()
        {
            var response = await _httpClient.GetAsync("https://mknsmart.my.id/sihepi/tickets");
            if (!response.IsSuccessStatusCode)
            {
                return new List<SihepiTicketDto>();
            }

            var content = await response.Content.ReadAsStringAsync();

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                // The API returns an envelope: { success, updated_at, total, rows: [...] }
                var apiResponse = JsonSerializer.Deserialize<SihepiApiResponse>(content, options);
                if (apiResponse == null || !apiResponse.Success)
                    return new List<SihepiTicketDto>();

                // Filter out invalid tickets
                return apiResponse.Rows
                    .Where(t => !string.IsNullOrWhiteSpace(t.TicketNo)
                        || (!string.IsNullOrWhiteSpace(t.WoNo) && t.WoNo != ""))
                    .ToList();
            }
            catch (Exception)
            {
                return new List<SihepiTicketDto>();
            }
        }
    }
}
