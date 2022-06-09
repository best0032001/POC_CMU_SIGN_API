
using CMU_SIGN_API.Model;
using CMU_SIGN_API.Model.Entity;
using CMU_SIGN_API.Model.Interface;
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

                String ClientID = Environment.GetEnvironmentVariable("ClientID");

                _cmuaccount = await this.getCmuaccount();
                if (_cmuaccount == "unauthorized") { return Unauthorized(); }

                String ext = filename.FileName.Substring(filename.FileName.LastIndexOf('.'));

                if (ext.ToLower() != ".pdf")
                {
                    aPIModel.title = "เอกสารต้องเป็น File  PDF";
                    return this.StatusCodeITSC(_cmuaccount, "sign", 400, aPIModel);
                }

                SignRequest signRequest = _applicationDBContext.SignRequests.Where(w => w.ref_id == ref_id).FirstOrDefault();
                if (signRequest != null)
                {
                    aPIModel.title = "ref_id ซ้ำ";
                    return this.StatusCodeITSC(_cmuaccount, "sign", 400, aPIModel);
                }

                String webhook = Environment.GetEnvironmentVariable("WEBHOOK");
                MultipartFormDataContent multipartFormContent = new MultipartFormDataContent();
                Stream stream = new MemoryStream();
                filename.CopyTo(stream);
                var fileStreamContent = new StreamContent(stream);
                multipartFormContent.Add(fileStreamContent, name: "pdf", fileName: filename.FileName);
                multipartFormContent.Add(new StringContent("accesstoken"), this._accesstoken);
                multipartFormContent.Add(new StringContent("pass_phase"), pass_phase);
                multipartFormContent.Add(new StringContent("ref_id"), ref_id);
                multipartFormContent.Add(new StringContent("sigfield"), sigfield);
                multipartFormContent.Add(new StringContent("reason"), reason);
                multipartFormContent.Add(new StringContent("webhook"), webhook);

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
                        return this.StatusCodeITSC(_cmuaccount, "sign",400, aPIModel);
                    }
                    SignRequest _signRequest = new SignRequest();
                    _signRequest.requestDate = DateTime.Now;
                    _signRequest.ref_id = ref_id;
                    _signRequest.filename_send = filename.FileName;
                    _signRequest.filename_receive = signModel.filename;
                    _signRequest.cmuaccount = _cmuaccount;
                    _applicationDBContext.SignRequests.Add(_signRequest);
                    _applicationDBContext.SaveChanges();
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
                _fname = fname["fname"];
                APIModel aPIModel = new APIModel();
                String fileName = _fname;
                SignRequest signRequest = _applicationDBContext.SignRequests.Where(w => w.filename_receive == fileName).FirstOrDefault();
                if (signRequest == null)
                {
                    aPIModel.title = "fileName not found";
                    return this.StatusCodeITSC("fileName : " + fileName, "webhook", 400, aPIModel);
                }
                var path = Path.Combine(Directory.GetCurrentDirectory(), "webhooksing");
                FileModel fileModel = this.SaveFile(path, files, 100);
                if (fileModel.isSave == false)
                {
                    aPIModel.title = " Server Save File Error";
                    return this.StatusCodeITSC("fileName : " + fileName, "webhook", 503, aPIModel);
                }
                List<IFormFile> Attachment = new List<IFormFile>();
                Attachment.Add(files);
                _emailRepository.SendEmailAsync("POC_CMU_SIGN_API", signRequest.cmuaccount, "เอกสาร " + signRequest.filename_send + " digital signature เสร็จสิ้น ", "", Attachment);
                aPIModel.data = signRequest;
                aPIModel.title = "success";
                return this.StatusCodeITSC(signRequest.cmuaccount, "webhook", 200, aPIModel);
            }
            catch (Exception ex)
            {
                return this.StatusErrorITSC(_fname, "webhook", ex);
            }


        }
    }
}
