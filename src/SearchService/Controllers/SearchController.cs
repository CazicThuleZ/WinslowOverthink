using Microsoft.AspNetCore.Mvc;
using MongoDB.Entities;
using SearchService.Models;
using SearchService.RequestHelpers;


namespace SearchService;

[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<Item>>> SearchItems([FromQuery]SearchParams searchParams)
    {
        var query = DB.PagedSearch<Item, Item>();

        if (!string.IsNullOrEmpty(searchParams.SearchTerm))
        {
            query = query.Match(Search.Full,searchParams.SearchTerm).SortByTextScore();
        }

        query = searchParams.OrderBy switch
        {
            "showtitle" => query.Sort(x => x.Ascending(s => s.ShowTitle)),
            "episodetitle" => query.Sort(x => x.Ascending(e => e.EpisodeTitle)),
            _ => query.Sort(x => x.Descending(s => s.FileCreateDateUTC))
        };

        query = searchParams.FilterBy switch
        {
            "movies" => query.Sort(x => x.Ascending(s => s.EpisodeTitle.Contains("Movie"))),
            "recent" => query.Sort(x => x.Ascending(e => e.FileCreateDateUTC > DateTime.Now.AddDays(-30))),
            _ => query.Sort(x => x.Descending(s => s.FileCreateDateUTC))
        };    

        if (!string.IsNullOrEmpty(searchParams.ShowTitle))
        {
            query = query.Match(x => x.ShowTitle == searchParams.ShowTitle);
        }

        if (!string.IsNullOrEmpty(searchParams.EpisodeTitle))
        {
            query = query.Match(x => x.EpisodeTitle == searchParams.EpisodeTitle);
        }  

        query.PageNumber(searchParams.PageNumber);
        query.PageSize(searchParams.PageSize);

        var result = await query.ExecuteAsync();
        return Ok(new
         {
            result = result.Results,
            pageCount = result.PageCount,
            totalCount = result.TotalCount
         });
        
    }
}
