using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace t1_frame.webapi.Filters
{
    public class ShortCircuitingResourceFilterAttribute : Attribute, IResourceFilter
    {
        public void OnResourceExecuted(ResourceExecutedContext context)
        {
        }

        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            context.Result = new ContentResult
            {
                Content = nameof(ShortCircuitingResourceFilterAttribute)
            };
        }
    }
}