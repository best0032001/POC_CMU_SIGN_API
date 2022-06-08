using CMU_SING_API.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CMU_SING_API.Controllers
{
    [Route("api/")]
    [ApiController]
    public class ITSCController : ControllerBase
    {
        protected IHttpClientFactory _clientFactory;
        protected IWebHostEnvironment _env;
        protected ILogger<ITSCController> _logger;
        protected String _accesstoken = "";
        public ITSCController()
        {
        }
        protected void loadConfig(ILogger<ITSCController> logger, IHttpClientFactory clientFactory, IWebHostEnvironment env)
        {
            _clientFactory = clientFactory;
            _env = env;
            _logger = logger;
        }
        protected String getTokenFormHeader()
        {
            try
            {
                _accesstoken = Request.Headers["Authorization"];
                _accesstoken = _accesstoken.Split(' ')[1];
                return _accesstoken;
            }
            catch
            {
                return "";
            }
        }
        protected async Task<String> getCmuaccount()
        {
            if (_accesstoken == "") { getTokenFormHeader(); }
            String _cmuaccount = "";
            String urlOauthIntrospection = Environment.GetEnvironmentVariable("OAUTH_INTROSPEC");
            var postData = new Dictionary<string, string>
            {
                { "token", _accesstoken }
            };
            var content = new FormUrlEncodedContent(postData);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");
            HttpClient httpClient = _clientFactory.CreateClient();
            var response = await httpClient.PostAsync(urlOauthIntrospection, content);
            var responseString = await response.Content.ReadAsStringAsync();
            dynamic responseGetToken = JsonConvert.DeserializeObject<dynamic>(responseString);
            try
            {
                Boolean active = responseGetToken.active;
                if (active == false) { return "unauthorized"; }
            }
            catch { }

            String _scope = responseGetToken.scope;
            String _granttype = responseGetToken.app.grant_type_value;
            String _appID = responseGetToken.app.client_id;
            String CMU_CLIENT_ID = Environment.GetEnvironmentVariable("CMU_CLIENT_ID");
            if (_scope.Contains(DataCache.cmuitaccount_basicinfo) && _appID.Equals(CMU_CLIENT_ID) && _granttype.ToUpper() == DataCache.authorization_code.ToUpper())
            {
                _cmuaccount = responseGetToken.user.user_id + "@cmu.ac.th";
            }
            else { _cmuaccount = "unauthorized"; }
            return _cmuaccount;

        }
    }
}
