using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AcessGitRepositories.Models
{
    public class ShowOrgRepo
    {
        public string login { get; set; }
        public string RepositoryName { get; set; }
        public string language { get; set; }
        public bool Isdotnet { get; set; }
        public string branchname { get; set; }
        //public string filename { get; set; }
        public string dllname { get; set; }
        public List<ShowOrgRepo> showOrgRepos;
    }
}