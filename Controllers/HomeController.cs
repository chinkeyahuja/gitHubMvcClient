﻿using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json.Linq;
using AcessGitRepositories.Models;
using System.Text;
using System.Xml;
using System.CodeDom.Compiler;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace AcessGitRepositories.Controllers
{
    public class HomeController : Controller
    {
        public string GITHUB_PUBLIC = "https://api.github.com";
        public static List<string> InvalidJsonElements;
        public ActionResult Index()
        {



            return View();
        }
        public ActionResult ShowRepos()
        {


            IList<Repository> repository;
            List<ShowOrgRepo> showOrgRepos = new List<ShowOrgRepo>();

            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            //var client = new HttpClient();
            ///*Get Organisation of a user*/
            //var token1 = TempData["Token"].ToString();
            //TempData.Keep("Token");
            //client.DefaultRequestHeaders.Accept.Clear();
            //client.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

            //client.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");
            //client.DefaultRequestHeaders.Add("Authorization", token1);

            //// Get Organization List per User Token
            //Task<string> t1 = client.GetStringAsync("https://api.github.com/user/orgs");
            //string checkResult1 = t1.Result;
            //var result = GetListofOrganization<Organization>(checkResult1).ToList();
            IList<Organization> organizations = getOrgList();
            //List<Filename> csProjFilesList = new List<Filename>();
            List<string> dllList = new List<string>();

            //Iterate Over Organizationbs to Get Repos List
            foreach (var org in organizations)
            {
                repository = getOrgRepos(org.reposurl);


                foreach (var repo in repository)
                {
                    if (repo.language == "C#" || repo.language == "JavaScript")
                    {
                       
                        showOrgRepos.Add(new ShowOrgRepo() { login = org.login, RepositoryName = repo.RepositoryName, language = repo.language, Isdotnet = true, branchname = repo.branchname });
                        
                    }
                    else
                    {
                        showOrgRepos.Add(new ShowOrgRepo() { login = org.login, RepositoryName = repo.RepositoryName, language = repo.language, Isdotnet = false, branchname = repo.branchname });
                    }

                }
            }
           
            return View(showOrgRepos);
        }

        private List<string> getDllsForRepo(string org, string repoName, string branch)
        {
            List<string> dllList = new List<string>();

            List<Filename> csProjFilesList = GetCsProjFiles(org, repoName, branch).ToList();
            foreach (var csProj in csProjFilesList)
            {
                dllList.AddRange(GetDllListFromCsProj(org, repoName, csProj.path));
            }


            return dllList;
        }

        private Task<string> getGitHubApiJson(string repoUrl)
        {
            string URL = (string) Session["URL"];
            string repoUrlFull = "";
            Debug.WriteLine("URL From Session is " + URL);

            if (URL.Length > 0)
            {
                repoUrlFull = (repoUrl.StartsWith("https://")) ? repoUrl : URL  + "/api/v3/" + repoUrl;
            } else
            {
                repoUrlFull = (repoUrl.StartsWith("https://")) ? repoUrl : GITHUB_PUBLIC + "/" + repoUrl;
            }
            Debug.WriteLine("URL Called is " + repoUrl + " converte dto" +repoUrlFull);

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var client = new HttpClient();
            var token1 = Session["Token"].ToString();

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

            client.DefaultRequestHeaders.Add("User-Agent", ".NET Sample Project");
            client.DefaultRequestHeaders.Add("Authorization", token1);
            return client.GetStringAsync(repoUrlFull);
        }
        private List<Repository> getOrgRepos(string repoUrl)
        {
            List<Repository> repoList = new List<Repository>();

            Task<string> jsonData = getGitHubApiJson(repoUrl);
            string jsonResult = jsonData.Result;
            repoList.AddRange(GetListofRepository<Repository>(jsonResult).ToList());
            return repoList;
        }
        private List<Organization> getOrgList()
        {
            List<Organization> orgList = new List<Organization>();
            string repoUrl = "user/orgs";
            Task<string> jsonData = getGitHubApiJson(repoUrl);
            string jsonResult = jsonData.Result;
            orgList.AddRange(GetListofOrganization<Organization>(jsonResult).ToList());
            return orgList;
        }
        public JsonResult ShowDlls(string org, string repoName, string branch)
        {
            List<string> dllList = new List<string>();

            dllList.AddRange(getDllsForRepo(org, repoName, branch));
            return Json(dllList, JsonRequestBehavior.AllowGet);
        }

        public JsonResult ShowDllRepoDepenencyMap()
        {

            List<Repository> repoList = new List<Repository>();
            List<string> repoNameList = new List<string>();
            Dictionary<string, List<string>> dllToRepoMap = new Dictionary<string, List<string>>();

            var watch = Stopwatch.StartNew();
            IList<Organization> organizations = getOrgList();
            watch.Stop();
            Debug.WriteLine("It took " + watch.ElapsedMilliseconds / 1000 + " seconds to fetch Org List ");
            List<string> dllList = new List<string>();
            List<string> existingValues = new List<string>();

            foreach (var o in organizations)
            {
                watch.Start();
                repoList.AddRange(getOrgRepos(o.reposurl));
                watch.Stop();

                foreach (var rep in repoList)
                {

                    watch.Start();
                    dllList.AddRange(getDllsForRepo(o.login, rep.RepositoryName, rep.branchname));
                    foreach (string dll in dllList)
                    {
                        if (dllToRepoMap.ContainsKey(dll))
                        {
                            Debug.WriteLine("Inside Map Creator: Updating Dll " + dll + " with Repo " + rep.RepositoryName);
                            existingValues = dllToRepoMap[dll];
                            existingValues.Add(rep.RepositoryName);
                            dllToRepoMap[dll] = existingValues;
                        }
                        else
                        {
                            Debug.WriteLine("Inside Map Creator: Inserting Dll " + dll + " with Repo " + rep.RepositoryName);
                            dllToRepoMap[dll] = new List<string> { rep.RepositoryName } ;
                        }
                    }
                    dllList.Clear();
                    watch.Stop();
                    Debug.WriteLine("It took " + watch.ElapsedMilliseconds / 1000 + " seconds to fetch " + dllList.Count + "Dll List for Repo " + rep.RepositoryName);

                }
                repoList.Clear();
            }
          
            return Json(dllToRepoMap, JsonRequestBehavior.AllowGet);
        }
        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
        public IList<Filename> GetCsProjFiles(string orgname, string Reponame, string branchname)
        {


            string URL = "repos/" + orgname + "/" + Reponame + "/git/trees/" + branchname + "?recursive=1";
            Task<string> t1 = getGitHubApiJson(URL);

            string checkResult1 = t1.Result;
            var details = JObject.Parse(checkResult1);
            List<Filename> validProdcuts = new List<Filename>();


            validProdcuts = GetListofFiles<Filename>(details["tree"].ToString(), Reponame).ToList();

            return validProdcuts;
        }
        public static IList<Filename> GetListofFiles<T>(string jsonString, string Reponame)
        {
            InvalidJsonElements = null;
            var array = JArray.Parse(jsonString);
            List<Filename> objectsList = new List<Filename>();
            var name = "";
            var language = "";
            foreach (var item in array)
            {
                try
                {
                    // CorrectElements  
                    name = item["path"].ToObject<string>();

                    //objectsList.Add(name);
                    if (name.Trim().EndsWith(".csproj") == true)
                    {
                        int index = name.LastIndexOf('/') + 1;

                        //objectsList.Add(new Filename() { path = name.Substring(index,name.Length-index),RepoName=Reponame });
                        objectsList.Add(new Filename() { path = name });
                    }

                    // objectsList.Add(item.ToObject<T>());
                }
                catch (Exception ex)
                {
                    InvalidJsonElements = InvalidJsonElements ?? new List<string>();
                    InvalidJsonElements.Add(item.ToString());
                }
            }

            return objectsList;
        }
        public List<string> GetDllListFromCsProj(string orgname, string reponame, string filename)
        {


            //var token1 = "token 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";
            string URL = "repos/" + orgname + "/" + reponame + "/contents/" + filename;

            //Task<string> t1 = client.GetStringAsync("https://api.github.com/repos/SelfRajan/testDotNet/contents/src/Azure/AzureAD/Authentication.AzureAD.UI/src/Microsoft.AspNetCore.Authentication.AzureAD.UI.csproj");
            Task<string> t1 = getGitHubApiJson(URL);

            string checkResult1 = t1.Result;

            var data = (JObject)JsonConvert.DeserializeObject(checkResult1);
            string content = data["content"].Value<string>();
            byte[] data1 = Convert.FromBase64String(content);
            string decodedString = Encoding.UTF8.GetString(data1);
            string tempDirectory = @"c:\\temp";
            TempFileCollection coll = new TempFileCollection(tempDirectory, true);
            string fileName = @"C:\Temp\csProj.xml";
            string output = DeleteLines(decodedString, 1);

            if (!output.Contains("<HintPath>"))
            {
                Debug.WriteLine("csProj File: " + fileName + " does not contain any Externa DLL");
                return new List<string>();
            }
            XmlDocument temp = new XmlDocument();
            try
            {
                temp.LoadXml(output);
                temp.Save(fileName);
            }
            catch (XmlException x)
            {
                Debug.WriteLine("GOT EXCEPTION When Reading String :\n" + output + " as Excel");
                return new List<string>();
            }


            List<string> dllListForRepo = CheckForDllReferences(fileName);
            Dictionary<string, List<string>> repoDLLMap = new Dictionary<string, List<string>>();
            repoDLLMap["Dummy Repo Name"] = dllListForRepo;
            //createDllDependencyMap(dllListForRepo, "Dummy Repo Name");

            //List<Filename> validProdcuts = new List<Filename>();


            //validProdcuts = GetListofFiles<Filename>(details["tree"].ToString(), Reponame).ToList();
            return dllListForRepo;
        }
        public static string DeleteLines(string s, int linesToRemove)
        {
            s = Regex.Replace(s, @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline);
            if (s.StartsWith("<?xml version"))
            {
                return s.Split(Environment.NewLine.ToCharArray(),
                           linesToRemove + 1
                ).Skip(linesToRemove)
                .FirstOrDefault();
            }
            else
            {
                return s;
            }

        }
        public static List<string> CheckForDllReferences(String csprojFile)
        {
            List<string> dllList = new List<string>();
            XmlDocument xdDoc = new XmlDocument();
            xdDoc.Load(csprojFile);

            XmlNamespaceManager xnManager =
             new XmlNamespaceManager(xdDoc.NameTable);
            xnManager.AddNamespace("tu",
             "http://schemas.microsoft.com/developer/msbuild/2003");

            XmlNode xnRoot = xdDoc.DocumentElement;
            XmlNodeList xnlPages = xnRoot.SelectNodes("//tu:HintPath", xnManager);

            foreach (XmlNode node in xnlPages)
            {
                string location = node.InnerText.ToLower();
                Console.WriteLine("DLL Found is " + location);
                dllList.Add(location.Split('\\').Last());
            }
            return dllList;
        }

        //private void createOrUpdateDependencyMaps(List<string> dlls, List<string> repos)
        //{
        //    List<string> existingValues = new List<string>();
        //    foreach (string k in dlls)
        //    {
        //        if (dllToRepoMap.ContainsKey(k))
        //        {
        //            existingValues = dllToRepoMap[k];
        //            existingValues.AddRange(repos);
        //            Debug.WriteLine("Inside Map Creator: DLL Exists and New list of Repo Count is " + repos.Count);

        //            dllToRepoMap[k] = existingValues;
        //            existingValues.Clear();
        //        }
        //        else
        //        {
        //            Debug.WriteLine("Inside Map Creator: DLL Doesnt Exists and New list of Repo Count is " + repos.Count);
        //            dllToRepoMap[k] = repos;
        //        }
        //    }
        //    Debug.WriteLine("Exiting Map Creator with " + dlls.Count + " Dlls and " + repos.Count + " repos");

        //}

        public static IList<Organization> GetListofOrganization<T>(string jsonString)
        {
            InvalidJsonElements = null;
            var array = JArray.Parse(jsonString);
            List<Organization> objectsList = new List<Organization>();
            var login = "";
            var repourl = "";
            foreach (var item in array)
            {
                try
                {
                    // CorrectElements  
                    login = item["login"].ToObject<string>();
                    repourl = item["repos_url"].ToObject<string>();
                    //objectsList.Add(name);
                    objectsList.Add(new Organization() { login = login, reposurl = repourl });
                    // objectsList.Add(item.ToObject<T>());
                }
                catch (Exception ex)
                {
                    InvalidJsonElements = InvalidJsonElements ?? new List<string>();
                    InvalidJsonElements.Add(item.ToString());
                }
            }

            return objectsList;
        }

        public static IList<Repository> GetListofRepository<T>(string jsonString)
        {
            InvalidJsonElements = null;
            var array = JArray.Parse(jsonString);
            List<Repository> objectsList = new List<Repository>();
            var name = "";
            var language = "";
            var branchname = "";
            foreach (var item in array)
            {
                try
                {
                    // CorrectElements  
                    name = item["name"].ToObject<string>();
                    language = item["language"].ToObject<string>();
                    branchname = item["default_branch"].ToObject<string>();
                    //objectsList.Add(name);
                    objectsList.Add(new Repository() { RepositoryName = name, language = language, branchname = branchname });
                    // objectsList.Add(item.ToObject<T>());
                }
                catch (Exception ex)
                {
                    InvalidJsonElements = InvalidJsonElements ?? new List<string>();
                    InvalidJsonElements.Add(item.ToString());
                }
            }

            return objectsList;
        }

        //public IList<Repository> GetReposData(string repourl)
        //{
        //    //var httpClient = new HttpClient();
        //    //httpClient.DefaultRequestHeaders.UserAgent.Add(
        //    //    new ProductInfoHeaderValue("MyApplication", "1"));
        //    //var repo = "rajansehgal/rmdb";
        //    //var contentsUrl = $"https://api.github.com/repos/{repo}/contents";
        //    //var contentsJson = await httpClient.GetStringAsync(contentsUrl);
        //    //var contents = (JArray)JsonConvert.DeserializeObject(contentsJson);
        //    //foreach (var file in contents)
        //    //{
        //    //    var fileType = (string)file["type"];
        //    //    if (fileType == "dir")
        //    //    {
        //    //        var directoryContentsUrl = (string)file["url"];
        //    //        // use this URL to list the contents of the folder
        //    //        Console.WriteLine($"DIR: {directoryContentsUrl}");
        //    //    }
        //    //    else if (fileType == "file")
        //    //    {
        //    //        var downloadUrl = (string)file["download_url"];
        //    //        // use this URL to download the contents of the file
        //    //        Console.WriteLine($"DOWNLOAD: {downloadUrl}");
        //    //    }
        //    //}
        //    IList<Repository> repositories;
        //    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        //    var client = new HttpClient();
        //    var token1 = TempData["Token"].ToString();
        //    client.DefaultRequestHeaders.Accept.Clear();
        //    client.DefaultRequestHeaders.Accept.Add(
        //        new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

        //    client.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");
        //    client.DefaultRequestHeaders.Add("Authorization", token1);
        //    Task<string> t1 = client.GetStringAsync(repourl);
        //    string checkResult1 = t1.Result;
        //    var result = GetListofRepository<Repository>(checkResult1).ToList();
        //    repositories = result;
        //    return repositories;
        //}
        

        public ActionResult Login()
        {

            return View();

        }
        [HttpPost]
        public ActionResult Login(Login login,FormCollection form)
        {
            using (var client = new HttpClient())
            {
                //client.BaseAddress = new Uri("https://api.github.com");
                //HTTP GET
                var token = "token " + login.Token;
                Session["Token"] = token;
                string URL = Request.Form["txturl"];
                Session["URL"] = URL;
                if (URL == "")
                {
                     URL = "https://api.github.com";
                }
                else {
                    URL = URL + "api/v3";
                }
                client.DefaultRequestHeaders.Accept.Clear();

                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
                //client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("token", token);
                client.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");
                client.DefaultRequestHeaders.Add("Authorization", token);
                Debug.WriteLine("URL Called is " + URL);
                var responseTask = client.GetAsync(URL, HttpCompletionOption.ResponseHeadersRead);
                responseTask.Wait();
                //TempData["Token"] = token;
                
                var result = responseTask.Result;
                if (result.IsSuccessStatusCode)
                {
                    return RedirectToAction("ShowRepos");
                }
                else
                {
                    ViewBag.Message = "Username/Token is incorrect.";
                }
            }
            return View();
        }
        public ActionResult ShowReposFile()
        {
            IList<Filename> filenames = GetCsProjFiles("loadDirectories", "Test", "default");
            return View(filenames);
        }
        public ActionResult Test()
        {
            string fileName = @"C:\Temp\Test.xml";
            // GetDllListFromCsProj("loadDirectories", "Test","test");

            CheckForDllReferences(fileName);
            return View();
        }
    }

}