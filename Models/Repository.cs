using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AcessGitRepositories.Models
{
    public class Repository
    {
        
        public string RepositoryName { get; set; }
        public string language { get; set; }
        public bool Isdotnet { get; set; }
        public string branchname { get; set; }
        public List<Repository> repositories;
     
    }
}