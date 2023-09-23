using System.Text.Json;
using MongoDB.Driver;
using MongoDB.Entities;
using SearchService.Models;
using SearchService.Services;

namespace SearchService.Data;

public class DbInitializer
{
    public static async Task InitDb(WebApplication app)
    {
        await DB.InitAsync("SearchDb",MongoClientSettings.FromConnectionString(app.Configuration.GetConnectionString("MongoDbConnection")));

        await DB.Index<Item>()
        .Key(x => x.FileName, KeyType.Text)
        .Key(x => x.ShowTitle, KeyType.Text)
        .CreateAsync();

        var count = await DB.CountAsync<Item>();

        using var scope = app.Services.CreateScope();
        var httpClient = scope.ServiceProvider.GetRequiredService<MediaFIleSvcClient>();

        var items = await httpClient.GetItemsForSearchDb();
  
        Console.WriteLine($"Found {items.Count} items from MediaFileService");
        if(items.Count > 0)
            await DB.SaveAsync(items);            

    }
}
