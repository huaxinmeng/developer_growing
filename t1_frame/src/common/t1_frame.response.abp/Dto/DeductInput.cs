using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t1_frame.response.abp
{
    public class DeductInput
    {
        [Required]
        public string user_code { get;set; }

        public decimal cost {  get;set; }
    }
}
