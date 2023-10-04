
using CMU_SIGN_API.Model;
using CMU_SIGN_API.Model.Entity;
using CMU_SIGN_API.Model.Interface;
using CMU_SING_API.Model;
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
        private ApplicationDBContext _applicationDBContext;
        public POCController(ILogger<ITSCController> logger, IHttpClientFactory clientFactory, IWebHostEnvironment env, ApplicationDBContext applicationDBContext, IEmailRepository emailRepository)
        {
            this.loadConfig(logger, clientFactory, env);
            _applicationDBContext = applicationDBContext;
            _emailRepository = emailRepository;
        }

        [HttpPost("v1/sign")]
        public async Task<IActionResult> sign(IFormFile filename, [FromHeader] String pass_phase, [FromHeader] String ref_id, [FromHeader] String sigfield, [FromHeader] String reason)
        {
            String _cmuaccount = "";
            APIModel aPIModel = new APIModel();
            try
            {
                byte[] data;
                using (var br = new BinaryReader(filename.OpenReadStream()))
                    data = br.ReadBytes((int)filename.OpenReadStream().Length);
                ByteArrayContent bytes = new ByteArrayContent(data);
                String ClientID = Environment.GetEnvironmentVariable("SINGClientID");
                _cmuaccount = await this.getCmuaccount();
                if (_cmuaccount == "unauthorized") { return Unauthorized(); }
                String ext = filename.FileName.Substring(filename.FileName.LastIndexOf('.'));

                if (ext.ToLower() != ".pdf")
                {
                    aPIModel.title = "เอกสารต้องเป็น File  PDF";
                    return this.StatusCodeITSC(_cmuaccount, "sign", 400, aPIModel);
                }
                String _filename = filename.FileName;
                String webhook = Environment.GetEnvironmentVariable("WEBHOOK");
                MultipartFormDataContent multipartFormContent = new MultipartFormDataContent();
                bytes.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                multipartFormContent.Add(bytes, name: "file", fileName: _filename);
                multipartFormContent.Add(new StringContent(getTokenFormHeader()), "accesstoken");
                //multipartFormContent.Add(new StringContent(pass_phase), "pass_phase");
                multipartFormContent.Add(new StringContent(ref_id), "ref_id");
                //multipartFormContent.Add(new StringContent(sigfield), "sigfield");
                //multipartFormContent.Add(new StringContent(reason), "reason");
                multipartFormContent.Add(new StringContent(webhook), "webhook");
                multipartFormContent.Add(new StringContent("oauth"), "type");
                String SIGNAPI = Environment.GetEnvironmentVariable("SINGAPI");
                HttpClient httpClient = _clientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + ClientID);
                HttpResponseMessage response = await httpClient.PostAsync(SIGNAPI, multipartFormContent);
                var responseString = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    responseString = await response.Content.ReadAsStringAsync();
                    SignModel signModel = JsonConvert.DeserializeObject<SignModel>(responseString);

                    if (signModel.status == "false")
                    {
                        aPIModel.title = responseString;
                        return this.StatusCodeITSC(_cmuaccount, "sign", 400, aPIModel);
                    }
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "webhooksing", signModel.filename);
                    var memory = this.loadFile(path);
                    List<IFormFile> Attachment = new List<IFormFile>();
                    var formFile = new FormFile(memory, 0, memory.Length, signModel.filename, signModel.filename);
                    Attachment.Add(formFile);
                    _emailRepository.SendEmailAsync("POC_CMU_SIGN_API", _cmuaccount, "เอกสาร " + filename.FileName + " digital signature เสร็จสิ้น ", "", Attachment);
                    aPIModel.data = signModel;
                    aPIModel.title = "success";
                    return this.StatusCodeITSC(_cmuaccount, "sign", 200, aPIModel);
                }
                else
                {
                    aPIModel.title = "" + (int)response.StatusCode + " " + responseString;
                    return this.StatusCodeITSC(_cmuaccount, "sign", (int)response.StatusCode, aPIModel);
                }
            }
            catch (Exception ex)
            {
                return this.StatusErrorITSC(_cmuaccount, "sign", ex);
            }
        }

        [HttpPost("v1/webhook")]
        public async Task<IActionResult> webhook(IFormFile files)
        {
            try
            {
                APIModel aPIModel = new APIModel();
                if (files == null)
                {
                    aPIModel.title = "files =null ";
                    return this.StatusCodeITSC("files =null ", "webhook", 400, aPIModel);
                }
                if (files.Length == 0)
                {
                    aPIModel.title = "files.Length==0 ";
                    return this.StatusCodeITSC("files.Length==0", "webhook", 400, aPIModel);
                }
                var path = Path.Combine(Directory.GetCurrentDirectory(), "webhooksing");
                FileModel fileModel = this.SaveFile(path, files, 100);
                if (fileModel.isSave == false)
                {
                    aPIModel.title = " Server Save File Error";
                    return this.StatusCodeITSC("fileName : " + files.FileName, "webhook", 503, aPIModel);
                }
                aPIModel.title = "success files name"+ files.FileName;
                return this.StatusCodeITSC("success files name" + files.FileName, "webhook", 200, aPIModel);
            }
            catch (Exception ex)
            {
                return this.StatusErrorITSC("-", "webhook", ex);
            }
        }
    }
}
