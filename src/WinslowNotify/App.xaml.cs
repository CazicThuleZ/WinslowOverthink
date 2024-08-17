using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;

namespace WinslowNotify;

public partial class App : Application
{
    public App()
    {
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        LogError("DispatcherUnhandledException", e.Exception);
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        LogError("UnhandledException", e.ExceptionObject as Exception);
    }

    private void LogError(string type, Exception ex)
    {
        string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
        using (StreamWriter writer = File.AppendText(logPath))
        {
            writer.WriteLine($"{DateTime.Now} - {type}:");
            writer.WriteLine(ex.ToString());
            writer.WriteLine();
        }
    }
}

