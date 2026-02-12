using Microsoft.AspNetCore.Mvc.Filters;

namespace t1_frame.webapi.Filters
{
    public class SampleActionFilter : IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Do something before the action executes.
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            // Do something before the action executes.
        }
    }
}