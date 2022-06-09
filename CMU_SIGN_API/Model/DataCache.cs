using CMU_SIGN_API.Model.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMU_SING_API.Model
{
    public static class DataCache
    {
        public static String cmuitaccount_basicinfo = "mishr.self.basicinfo";
        public static String authorization_code = "authorization_code";
        public static List<SignRequest> SignRequests { get; set; }
    }
}
