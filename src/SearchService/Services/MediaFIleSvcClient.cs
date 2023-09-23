using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Services
{
    public class MediaFIleSvcClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        public MediaFIleSvcClient(HttpClient httpClient, IConfiguration config)
        {
            _config = config;
            _httpClient = httpClient;            
        }

        public async Task<List<Item>> GetItemsForSearchDb() 
        {
            var lastUpdated = await DB.Find<Item, string>()
                                      .Sort(x => x.Descending(x => x.FileCreateDateUTC))
                                      .Project(x => x.FileCreateDateUTC.ToString())
                                      .ExecuteFirstAsync();
            
            return await _httpClient.GetFromJsonAsync<List<Item>>(_config["VideoFileServiceUrl"] + $"api/MediaFiles?date={lastUpdated}");




        }
    }
}