using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMU_SIGN_API.Model
{
    public class FileModel
    {
        public Boolean isSave { get; set; }
        public String fileName { get; set; }
        public String error { get; set; }

        public String fullPath { get; set; }
        public String dbPath { get; set; }
    }
}
