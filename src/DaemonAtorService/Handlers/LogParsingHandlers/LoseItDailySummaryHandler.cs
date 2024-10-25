using DaemonAtorService.DTOs;
using System.Text.Json;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace DaemonAtorService
{
    public class LoseItDailySummaryHandler : ILogProcessor
    {
        private readonly ILogger<LoseItDailySummaryHandler> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _dashboardUrl;

        public LoseItDailySummaryHandler(ILogger<LoseItDailySummaryHandler> logger, HttpClient httpClient, IOptions<GlobalSettings> globalSettings)
        {
            _httpClient = httpClient;
            _logger = logger;
            _dashboardUrl = globalSettings.Value.DashboardServiceBaseEndpoint;
        }

        public async Task<bool> ProcessAsync(string fileName)
        {
            _logger.LogInformation("Processing LoseIt Daily summary log");

            try
            {
                var parser = new CsvParser();
                var dietStatistics = parser.ParseCsv(fileName);

                var success = await LogPricingAsync(dietStatistics);
                if (success)
                {
                    await LogMealsAsync(dietStatistics);
                    var (totalCost, allCostsDetermined) = await CalculateTotalCostAsync(dietStatistics);

                    if (!allCostsDetermined)
                        totalCost = 0; // The idea being that if even one item in the meal log is indeterminate, the total cost for the day should be 0 (additional attempts to get it right will be made as part of the end-of-day processing)

                    success = await LogDietAsync(dietStatistics, totalCost);

                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing LoseIt Daily summary: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> LogPricingAsync(List<DietStatistic> dietStatistics)
        {
            bool success = true;
            foreach (var dietStat in dietStatistics)
            {
                if (dietStat.Type.ToLower() != "exercise")
                {
                    var foodPriceDto = new FoodPriceDto
                    {
                        Name = dietStat.Name,
                        Quantity = dietStat.Quantity,
                        UnitOfMeasure = dietStat.Units
                    };

                    var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                    var jsonData = JsonSerializer.Serialize(foodPriceDto, jsonOptions);
                    var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                    var response = await _httpClient.PostAsync(_dashboardUrl + "/diet/add-food-price", content);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation($"Successfully added food price for {dietStat.Name}");
                    }
                    else
                    {
                        success = false;
                        _logger.LogError($"Failed to add food price for {dietStat.Name}");
                    }
                }
            }

            return success;
        }

        private async Task<(decimal TotalCost, bool AllCostsDetermined)> CalculateTotalCostAsync(List<DietStatistic> dietStatistics)
        {
            decimal totalCost = 0;
            bool allCostsDetermined = true;

            foreach (var dietStat in dietStatistics)
            {
                if (dietStat.Type.ToLower() == "exercise")
                    continue;

                var (foodPrice, success) = await GetFoodPriceAsync(dietStat);
                if (success)
                {
                    totalCost += foodPrice;
                }
                else
                {
                    allCostsDetermined = false;
                }
            }

            return (totalCost, allCostsDetermined);
        }

        private async Task<(decimal Price, bool Success)> GetFoodPriceAsync(DietStatistic dietStat)
        {
            var response = await _httpClient.GetAsync(
                _dashboardUrl + $"/diet/calculate-food-price?name={dietStat.Name}&unitOfMeasure={dietStat.Units}&quantity={dietStat.Quantity}");

            if (response.IsSuccessStatusCode)
            {
                var price = await response.Content.ReadFromJsonAsync<decimal>();
                return (price, true);
            }

            _logger.LogInformation("Failed to determine food price for {FoodName} at {Date}. Status Code: {StatusCode}, Response: {ResponseContent}",
                dietStat.Name, dietStat.Date, response.StatusCode, await response.Content.ReadAsStringAsync());
            return (0, false);
        }

        private async Task LogMealsAsync(List<DietStatistic> dietStatistics)
        {
            foreach (var dietStat in dietStatistics)
            {
                if (dietStat.Type.ToLower() == "exercise") continue;

                var (foodPrice, _) = await GetFoodPriceAsync(dietStat);
                await LogMealEntryAsync(dietStat, foodPrice);
            }
        }

        private async Task LogMealEntryAsync(DietStatistic dietStat, decimal foodPrice)
        {
            var mealLogDto = new MealLogDto
            {
                SnapshotDateUTC = dietStat.Date,
                Name = dietStat.Name,
                MealType = dietStat.Type,
                Quantity = dietStat.Quantity,
                UnitOfMeasure = dietStat.Units,
                Calories = dietStat.Calories,
                FatGrams = dietStat.Fat,
                CarbGrams = dietStat.Carbohydrates,
                SugarGrams = dietStat.Sugars,
                ProteinGrams = dietStat.Protein,
                Cost = foodPrice
            };

            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
            var jsonData = JsonSerializer.Serialize(mealLogDto, jsonOptions);

            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_dashboardUrl + "/diet/create-meal-log-entry", content);

            if (response.IsSuccessStatusCode)
                _logger.LogInformation("Successfully logged meal data for {MealName} at {Date}", dietStat.Name, dietStat.Date);
            else
                _logger.LogError("Failed to log meal data for {MealName} at {Date}. Status Code: {StatusCode}, Response: {ResponseContent}",
                    dietStat.Name, dietStat.Date, response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        private async Task<bool> LogDietAsync(List<DietStatistic> dietStatistics, decimal totalCost)
        {
            try
            {
                if (dietStatistics == null || !dietStatistics.Any())
                {
                    _logger.LogInformation($"No dietary data to log.");
                    return false;
                }

                var reportDate = dietStatistics.First().Date;

                var dietStatDto = new DietStatDto
                {
                    SnapshotDateUTC = reportDate,
                    Calories = dietStatistics
                        .Where(stat => stat.Type.ToLower() != "exercise")
                        .Sum(stat => stat.Calories),
                    ExerciseCalories = dietStatistics
                        .Where(stat => stat.Type.ToLower() == "exercise")
                        .Sum(stat => stat.Calories),
                    FatGrams = dietStatistics.Sum(stat => stat.Fat),
                    ProteinGrams = dietStatistics.Sum(stat => stat.Protein),
                    CarbGrams = dietStatistics.Sum(stat => stat.Carbohydrates),
                    SaturatedFatGrams = dietStatistics.Sum(stat => stat.SaturatedFat),
                    SugarGrams = dietStatistics.Sum(stat => stat.Sugars),
                    FiberGrams = dietStatistics.Sum(stat => stat.Fiber),
                    CholesterolGrams = dietStatistics.Sum(stat => stat.Cholesterol),
                    SodiumMilliGrams = dietStatistics.Sum(stat => stat.Sodium),
                    Cost = totalCost
                };

                var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                var jsonData = JsonSerializer.Serialize(dietStatDto, jsonOptions);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_dashboardUrl + "/diet/create-diet-diary-snapshot", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Successfully processed and posted dietary data for {reportDate}");
                    return true;
                }
                else
                {
                    _logger.LogError($"Failed to post data dietary data for {reportDate}. Status Code: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to log diet data for file");
                throw;
            }
        }
    }
}