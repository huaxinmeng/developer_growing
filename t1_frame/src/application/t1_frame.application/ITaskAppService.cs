using Abp.Application.Services;
using Abp.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t1_frame.application
{
    public interface ITaskAppService : IApplicationService
    {
        Task<string> GetTaskName(int id);
    }
}
