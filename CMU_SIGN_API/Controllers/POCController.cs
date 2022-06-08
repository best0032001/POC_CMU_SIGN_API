
using CMU_SIGN_API.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace CMU_SING_API.Controllers
{
    [Produces("application/json")]
    [Route("api/")]
    [ApiController]
    public class POCController : ITSCController
    {
        public POCController(ILogger<ITSCController> logger, IHttpClientFactory clientFactory, IWebHostEnvironment env)
        {
            this.loadConfig(logger, clientFactory, env);
        }

        [HttpPost("v1/sign")]
        public async Task<IActionResult> sign(IFormFile filename, [FromHeader] String pass_phase, [FromHeader] String ref_id, [FromHeader] String sigfield, [FromHeader] String reason)
        {
            try
            {
                String Cmuaccount = "";
                Cmuaccount = await this.getCmuaccount();
                if (Cmuaccount == "unauthorized") { return Unauthorized(); }

                String webhook = Environment.GetEnvironmentVariable("WEBHOOK");
                MultipartFormDataContent multipartFormContent = new MultipartFormDataContent();
                Stream stream = new MemoryStream();
                filename.CopyTo(stream);
                var fileStreamContent = new StreamContent(stream);
                multipartFormContent.Add(fileStreamContent, name: "pdf", fileName: "test.pdf");

                multipartFormContent.Add(new StringContent("accesstoken"), this._accesstoken);
                multipartFormContent.Add(new StringContent("pass_phase"), pass_phase);
                multipartFormContent.Add(new StringContent("ref_id"), ref_id);
                multipartFormContent.Add(new StringContent("sigfield"), sigfield);
                multipartFormContent.Add(new StringContent("reason"), reason);

                multipartFormContent.Add(new StringContent("webhook"), webhook);

                String SIGNAPI = Environment.GetEnvironmentVariable("SINGAPI");
                HttpClient httpClient = _clientFactory.CreateClient();
                HttpResponseMessage response = await httpClient.PostAsync(SIGNAPI, multipartFormContent);
                var responseString = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    responseString = await response.Content.ReadAsStringAsync();
                    SignModel signModel = JsonConvert.DeserializeObject<SignModel>(responseString);
                    return Ok();
                }
                else
                {

                    String textReturn = "" + (int)response.StatusCode + " " + responseString;
                    return Ok(textReturn);
                }
            }
            catch (Exception ex)
            {
                return Ok(ex.StackTrace);
            }




        }
    }
}
