using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace t1_frame.response.abp
{
    public class UserAcountInput
    {
        [JsonIgnore]
        public long user_id { get; set; }

        public decimal amount { get; set; }

        [Required]
        public string user_code { get; set; }
    }
}
