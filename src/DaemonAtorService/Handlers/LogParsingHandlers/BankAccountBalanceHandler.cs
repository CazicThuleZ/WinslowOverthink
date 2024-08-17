using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace DaemonAtorService;

public class BankAccountBalanceHandler : ILogProcessor
{
    private readonly ILogger<BankAccountBalanceHandler> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _dashboardUrl;

    public BankAccountBalanceHandler(ILogger<BankAccountBalanceHandler> logger, HttpClient httpClient, IOptions<GlobalSettings> globalSettings)
    {
        _httpClient = httpClient;
        _logger = logger;
        _dashboardUrl = globalSettings.Value.DashboardServiceBaseEndpoint;
    }
    public async Task<bool> ProcessAsync(string fileName)
    {
        try
        {
            bool success = true;

            var bankBalance = await DeserializeBalanceLogAsync(fileName);
            bankBalance ??= await DeserializeBalanceLogOLDAsync(fileName);

            if (bankBalance == null)
                success = false;
            else
                success = await PostBankBalanceLogAsync(bankBalance);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing Bank Balance: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> PostBankBalanceLogAsync(LogAccountBalance bankBalance)
    {
        AccountBalanceDto accountBalanceDto = new AccountBalanceDto
        {
            SnapshotDateUTC = bankBalance.SnapshotDateUTC,
            AccountName = bankBalance.AccountName,
            Balance = bankBalance.Balance
        };

        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        var jsonData = JsonSerializer.Serialize(accountBalanceDto, jsonOptions);

        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(_dashboardUrl + @"/accounts/create-account-snapshot", content);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation($"Successfully added bank balance for account '{bankBalance.AccountName}' on {bankBalance.SnapshotDateUTC}.");
            return true;
        }
        else
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogError($"Failed to add bank balance for account '{bankBalance.AccountName}' on {bankBalance.SnapshotDateUTC}. " +
                             $"Status Code: {response.StatusCode}, Reason: {response.ReasonPhrase}, Response Content: {responseContent}");
            return false;
        }
    }


    private async Task<LogAccountBalance> DeserializeBalanceLogOLDAsync(string fileName)
    {
        // TODO:  mostly sure this isn't needed any more.
        try
        {
            string accountName = string.Empty;
            string fileContent = await File.ReadAllTextAsync(fileName);

            if (fileContent.ToUpper().Contains("CHECKING ACCOUNT"))
                accountName = "Lookout Credit Union";
            if (fileContent.ToUpper().Contains("HEALTH SAVINGS ACCOUNT"))
                accountName = "Fidelity Health Savings Account";
            if (fileContent.ToUpper().Contains("DISCOVER SAVINGS"))
                accountName = "Discover Savings Account";

            // string balancePattern = @"account balance as of (?<date>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}) is: \$(?<balance>[0-9,]+\.\d{2})";
            // string balancePattern = @"checking account balance as of (?<date>\d{14}) is: \$(?<balance>[0-9,]+\.\d{2})";
            // string balancePattern = @"Account balance as of (?<date>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}) is: \$(?<balance>[0-9,]+\.\d{2})";
            string balancePattern = @"Account balance as of (?<date>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}) is: \$(?<balance>[0-9,]+\.\d{2})";


            var match = Regex.Match(fileContent.ToLower(), balancePattern);

            if (match.Success)
            {
                string dateString = match.Groups["date"].Value;
                string balanceString = match.Groups["balance"].Value;

                // DateTime date = DateTime.ParseExact(dateString, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                // DateTime date = DateTime.ParseExact(dateString, "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
                // DateTime date = DateTime.ParseExact(dateString, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                DateTime date = DateTime.ParseExact(dateString, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                decimal balance = decimal.Parse(balanceString, System.Globalization.NumberStyles.Currency);

                return new LogAccountBalance
                {
                    SnapshotDateUTC = date,
                    Balance = balance,
                    AccountName = accountName
                };
            }
            else
            {
                _logger.LogError("Error parsing bank balance log: Invalid file format.");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deserializing bank balance message: {ex.Message}");
            return null;
        }
    }

    private async Task<LogAccountBalance> DeserializeBalanceLogAsync(string fileName)
    {
        try
        {
            string jsonString = await File.ReadAllTextAsync(fileName);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            LogAccountBalance logAccountBalance = JsonSerializer.Deserialize<LogAccountBalance>(jsonString, options);

            return logAccountBalance;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deserializing bank balance log: {ex.Message}");
            return null;
        }
    }
}
