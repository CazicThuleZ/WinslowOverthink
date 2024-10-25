namespace DaemonAtorService;

public class GmailApiSettings
{
    public string EmailAddress { get; set; }
    public string AppPassword { get; set; }
    public string ImapServer { get; set; } = "imap.gmail.com";
    public int ImapPort { get; set; } = 993;
    public string EmailSaveDirectory { get; set; }
    public string AttachmentSaveLocation { get; set; }
}
