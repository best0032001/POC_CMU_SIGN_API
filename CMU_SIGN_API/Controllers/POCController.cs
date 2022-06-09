
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
                //Stream stream = new MemoryStream();
                //filename.CopyTo(stream);
                //var fileStreamContent = new StreamContent(stream);
                bytes.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

                multipartFormContent.Add(bytes, name: "pdf", fileName: _filename);
                multipartFormContent.Add(new StringContent(getTokenFormHeader()), "accesstoken");
                multipartFormContent.Add(new StringContent(pass_phase), "pass_phase");
                multipartFormContent.Add(new StringContent(ref_id), "ref_id");
                multipartFormContent.Add(new StringContent(sigfield), "sigfield");
                multipartFormContent.Add(new StringContent(reason), "reason");
                multipartFormContent.Add(new StringContent(webhook), "webhook");



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
                    SignRequest _signRequest = DataCache.SignRequests.Where(w => w.filename_receive == signModel.filename.Trim()).FirstOrDefault();
                    if (_signRequest != null)
                    {
                        List<IFormFile> Attachment = new List<IFormFile>();
                        Attachment.Add(_signRequest.file);
                        _emailRepository.SendEmailAsync("POC_CMU_SIGN_API", _cmuaccount, "เอกสาร " + filename.FileName + " digital signature เสร็จสิ้น ", "", Attachment);
                    }
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
        public async Task<IActionResult> webhook(IFormFile files, IFormCollection fname)
        {
            //MemoryStream stream = new MemoryStream();
            //await Request.Body.CopyToAsync(stream);
            //var file = new FormFile(stream, 0, stream.Length, null, "name.pdf")
            //{
            //    Headers = new HeaderDictionary(),
            //    ContentType = "application/pdf"
            //};
            String _fname = "";
            try
            {
                APIModel aPIModel = new APIModel();
                if (files == null)
                {
                    aPIModel.title = "files =null ";
                    return this.StatusCodeITSC("files =null ", "webhook", 400, aPIModel);
                }
                if (files.Length==0)
                {
                    aPIModel.title = "files.Length==0 ";
                    return this.StatusCodeITSC("files.Length==0", "webhook", 400, aPIModel);
                }
                String debug = "";
                List<String> list = fname.Keys.ToList();
                foreach (String text in list)
                {
                    debug = debug + text;
                }
                _fname = fname["name"];
             
                String fileName = _fname;

                if (fileName.IndexOf("/app") > 0)
                {
                    fileName = fileName.Substring(fileName.IndexOf("/app") + 5, 44);
                }

                SignRequest signRequest = DataCache.SignRequests.Where(w => w.filename_receive == fileName.Trim()).FirstOrDefault();
                if (signRequest == null)
                {
                    SignRequest _signRequest = new SignRequest();
                    _signRequest.requestDate = DateTime.Now;
                    _signRequest.ref_id = "-";
                    _signRequest.filename_send = "-";
                    _signRequest.filename_receive = fileName;
                    _signRequest.cmuaccount = "-";
                    _signRequest.file = files;
                    DataCache.SignRequests.Add(_signRequest);
                    aPIModel.data = _signRequest;
                }
                var path = Path.Combine(Directory.GetCurrentDirectory(), "webhooksing");
                FileModel fileModel = this.SaveFile(path, files, 100);
                if (fileModel.isSave == false)
                {
                    aPIModel.title = " Server Save File Error";
                    return this.StatusCodeITSC("fileName : " + fileName, "webhook", 503, aPIModel);
                }
                aPIModel.title = "success";
                return this.StatusCodeITSC(signRequest.cmuaccount, "webhook", 200, aPIModel);
            }
            catch (Exception ex)
            {
                return this.StatusErrorITSC(_fname, "webhook", ex);
            }


        }


        [HttpPost("v1/test")]
        public async Task<IActionResult> test(IFormFile pdf)
        {

            return Ok();
        }
    }
}
