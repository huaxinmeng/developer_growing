using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace t1_frame_library
{
    [Route("api/[controller]/[action]")]
    public class IndexController
    {
        [HttpGet]
        public string GetStr(string name)
        {
            return "this is a text";
        }
    }
}
