
using WinslowUI.DTOs;

namespace WinslowUI;

public class DietStatRepository : IDietStatRepository
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    public DietStatRepository(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiBaseUrl = configuration["ApiBaseUrl"];        
    }
    public async Task<List<DietStatDto>> GetDietStatsAsync(DateTime beginDate, DateTime endDate)
    {
        var beginDateFormatted = beginDate.ToString("yyyy-MM-dd");
        var endDateFormatted = endDate.ToString("yyyy-MM-dd");

        var response = await _httpClient.GetFromJsonAsync<List<DietStatDto>>(
            $"{_apiBaseUrl}/dashboard/diet/get-diet-diary?beginDate={beginDateFormatted}&endDate={endDateFormatted}");

        return response;
    }
}
