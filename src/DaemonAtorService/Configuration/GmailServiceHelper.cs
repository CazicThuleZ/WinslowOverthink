using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Util;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Options;

namespace DaemonAtorService;

public class GmailServiceHelper
{
    static string[] Scopes = { GmailService.Scope.GmailReadonly, GmailService.Scope.GmailModify };
    public static string ApplicationName = "Winslow Overthinker";
    private readonly GmailApiSettings _gmailApiSettings;
    private readonly ILogger<EmailReadJob> _logger;

    public GmailServiceHelper(ILogger<EmailReadJob> logger, IOptions<GmailApiSettings> gmailApiSettings)
    {
        _gmailApiSettings = gmailApiSettings.Value;
        _logger = logger;
    }

    public async Task<UserCredential> GetUserCredentialAsync()
    {
        var clientSecrets = new ClientSecrets
        {
            ClientId = _gmailApiSettings.ClientId,
            ClientSecret = _gmailApiSettings.ClientSecret
        };

        var credPath = _gmailApiSettings.CredentialsPath;

        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            clientSecrets,
            Scopes,
            "user",
            CancellationToken.None,
            new FileDataStore(credPath, true));

        if (credential.Token.IsStale)
        {
            _logger.LogInformation("Refreshed Oauth Token {time}", DateTimeOffset.Now);
            await credential.RefreshTokenAsync(CancellationToken.None);
        }

        return credential;
    }
}