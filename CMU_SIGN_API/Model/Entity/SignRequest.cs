using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMU_SIGN_API.Model.Entity
{
    public class SignRequest
    {
        public int SignRequestId { get; set; }
        public string ref_id { get; set; }

        public DateTime requestDate { get; set; }
        public DateTime? receiveDate { get; set; }
        public string filename_send { get; set; }
        public string filename_receive { get; set; }
    }
}
