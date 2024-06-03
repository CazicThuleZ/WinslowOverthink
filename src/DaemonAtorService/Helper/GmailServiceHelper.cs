using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Util;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Options;

namespace DaemonAtorService;

public class GmailServiceHelper
{
    static string[] Scopes = { GmailService.Scope.GmailReadonly , GmailService.Scope.GmailModify};
    public static string ApplicationName = "Winslow Overthinker";
    private readonly GmailApiSettings _gmailApiSettings;

    public GmailServiceHelper(IOptions<GmailApiSettings> gmailApiSettings)
    {
        _gmailApiSettings = gmailApiSettings.Value;
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
            await credential.RefreshTokenAsync(CancellationToken.None);

        return credential;
    }
}