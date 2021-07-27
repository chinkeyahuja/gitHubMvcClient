using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AcessGitRepositories.Models
{
    public class Organization
    {
        public string login { get; set; }
        public string reposurl { get; set; }
        public List<Organization> organizations { get; set; }
    }
}