using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DaemonDo.Journal
{
    public class HashtagInfo
    {
        public string Hashtag { get; set; }
        public int Count { get; set; }

        public HashtagInfo(string hashtag)
        {
            Hashtag = hashtag;
            Count = 1;
        }
    }
}