using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace AcessGitRepositories.Models
{
    public class Login
    {
        [Required(ErrorMessage = "Please enter user name.")]
        public string UserName { get; set; }
        [Required(ErrorMessage = "Please enter token.")]
        [DataType(DataType.Password)]
        public string Token { get; set; }
    }
}