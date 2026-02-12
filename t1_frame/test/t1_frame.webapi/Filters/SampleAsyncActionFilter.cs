using Microsoft.AspNetCore.Mvc.Filters;

namespace t1_frame.webapi.Filters
{
    public class SampleAsyncActionFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Do something before the action executes.
            await next();
            // Do something after the action executes.
        }
    }
}