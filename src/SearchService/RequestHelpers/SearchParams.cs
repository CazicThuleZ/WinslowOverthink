using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace SearchService.RequestHelpers
{
    public class SearchParams
    {
        public string SearchTerm { get; set; }
        public int PageSize { get; set; } = 2;
        public int PageNumber { get; set; } = 1;
        public string OrderBy { get; set; }
        public string ShowTitle { get; set; }
        public string EpisodeTitle { get; set; }
        public string FilterBy { get; set; }
    }
}