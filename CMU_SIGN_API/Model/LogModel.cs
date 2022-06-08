using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMU_SIGN_API.Model
{
    public class LogModel
    {
        [Date(Name = "@timestamp")]
        public DateTime Timestamp { get; set; }
        public String ClientIp { get; set; }
        public String UserType { get; set; }
        public String cmuaccount { get; set; }
        public String LineID { get; set; }
        public String scope { get; set; }
        public String action { get; set; }
        public String granttype { get; set; }
        public String appID { get; set; }
        public String appIndex { get; set; }
        public String level { get; set; }
        public String logdata { get; set; }
        public String HttpCode { get; set; }
        public String Auth { get; set; }
        public Double responseTime { get; set; }
        public String logdate { get; set; }
    }
}
