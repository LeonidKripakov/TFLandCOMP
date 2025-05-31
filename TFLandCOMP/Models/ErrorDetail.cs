using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFLandCOMP.Models
{
    public class ErrorDetail
    {
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string Position { get; set; }
    }

    public class Quadruple
    {
        public string Operation { get; set; }
        public string Arg1 { get; set; }
        public string Arg2 { get; set; }
        public string Result { get; set; }

        public override string ToString()
        {
            return $"{Result} = {Arg1} {Operation} {Arg2}";
        }
    }
}
