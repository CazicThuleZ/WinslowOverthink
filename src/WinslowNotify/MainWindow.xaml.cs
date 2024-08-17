using System;
using System.Windows;
using System.IO;
using System.Text.Json;
using Microsoft.Toolkit.Uwp.Notifications;
using Hardcodet.Wpf.TaskbarNotification;
using WinslowNotify.Models;
using System.Windows.Controls;
using Windows.UI.Notifications;

namespace WinslowNotify
{
    public partial class MainWindow : Window
    {
        private FileSystemWatcher watcher;
        private TaskbarIcon notifyIcon;

        public MainWindow()
        {
            InitializeComponent();
            SetupSystemTrayIcon();
            SetupFileWatcher();

            // Hide the window on startup
            this.Hide();

            // Prevent the application from closing when the window is closed
            this.Closing += (s, e) =>
            {
                e.Cancel = true;
                this.Hide();
            };
        }

        private void SetupSystemTrayIcon()
        {
            try
            {
                notifyIcon = new TaskbarIcon();

                string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Icons", "text_loudspeaker.ico");
                if (File.Exists(iconPath))
                {
                    notifyIcon.Icon = new System.Drawing.Icon(iconPath);
                }
                else
                {
                    File.AppendAllText("debug.log", $"Icon file not found at: {iconPath}\n");
                    // Use a default system icon as fallback
                    notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                }

                notifyIcon.ToolTipText = "Winslow Notification Tray App";

                // Setup context menu
                var menu = new System.Windows.Controls.ContextMenu();
                var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
                exitItem.Click += (s, e) =>
                {
                    notifyIcon.Dispose();
                    Application.Current.Shutdown();
                };
                menu.Items.Add(exitItem);

                notifyIcon.ContextMenu = menu;

                File.AppendAllText("debug.log", "System tray icon set up successfully.\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText("debug.log", $"Error setting up system tray icon: {ex}\n");
            }
        }

        private void SetupFileWatcher()
        {
            try
            {
                string path = @"C:\Services\WinslowNotify\Notify";
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                watcher = new FileSystemWatcher(path)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                    Filter = "*.json"
                };

                watcher.Created += OnCreated;
                watcher.Renamed += OnRenamed;
                watcher.EnableRaisingEvents = true;

                File.AppendAllText("debug.log", $"File watcher set up for {path}\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText("debug.log", $"Error setting up file watcher: {ex}\n");
            }
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            File.AppendAllText("debug.log", $"File created: {e.FullPath}\n");

            // Add a small delay to ensure the file is fully written
            System.Threading.Thread.Sleep(100);

            ProcessJsonFile(e.FullPath);
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {

            File.AppendAllText("debug.log", $"File renamed: {e.OldFullPath} to {e.FullPath}\n");
            System.Threading.Thread.Sleep(100);

            ProcessJsonFile(e.FullPath);
        }

        private void ProcessJsonFile(string filePath)
        {
            try
            {
                string jsonString = File.ReadAllText(filePath);
                File.AppendAllText("debug.log", $"File contents: {jsonString}\n");

                var notification = System.Text.Json.JsonSerializer.Deserialize<NotificationData>(jsonString);

                if (notification != null && notification.Notification != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ClearPreviousNotifications();
                        ShowNotification(notification.Notification.Title, notification.Notification.Message);
                    });
                }

                File.Delete(filePath);
                File.AppendAllText("debug.log", $"Processed and deleted file: {filePath}\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText("debug.log", $"Error processing file {filePath}: {ex}\n");
            }
        }

        private void ShowNotification(string title, string message)
        {
            try
            {
                // Create the toast content
                var content = new ToastContentBuilder()
                    .AddText(title)
                    .AddText(message)
                    .AddButton(new ToastButton()
                      .SetContent("Copy to Clipboard")
                      .AddArgument("action", "copy")
                      .SetBackgroundActivation())
                    .AddButton(new ToastButton()
                        .SetContent("Dismiss")
                        .AddArgument("action", "dismiss")
                        .SetBackgroundActivation())
                    .GetToastContent();

                var toast = new ToastNotification(content.GetXml())
                {
                    ExpirationTime = null,
                    Tag = "persistent",
                    Group = "persistentGroup"
                };

                toast.Activated += (s, e) =>
                {
                    var args = e as ToastActivatedEventArgs;
                    if (args != null && args.Arguments == "action=copy")
                    {
                        // Copy the value to the clipboard on the UI thread
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Clipboard.SetText(title);
                            File.AppendAllText("debug.log", $"Message copied to clipboard - Title: {title}, Message: {message}\n");
                        });
                    }
                };

                ToastNotificationManagerCompat.CreateToastNotifier().Show(toast);

                File.AppendAllText("debug.log", $"Persistent notification with dismiss button shown - Title: {title}, Message: {message}\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText("debug.log", $"Error showing notification: {ex}\n");
            }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ClearPreviousNotifications()
        {
            ToastNotificationManagerCompat.History.RemoveGroup("persistentGroup");
        }
    }

    public class NotificationData
    {
        public NotificationInfo Notification { get; set; }
    }
}