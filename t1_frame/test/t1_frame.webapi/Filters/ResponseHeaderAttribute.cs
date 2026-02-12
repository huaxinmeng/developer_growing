using Microsoft.AspNetCore.Mvc.Filters;

namespace t1_frame.webapi.Filters
{
    public class ResponseHeaderAttribute : ActionFilterAttribute
    {
        private readonly string _name;
        private readonly string _value;

        public ResponseHeaderAttribute(string name, string value) =>
            (_name, _value) = (name, value);

        public override void OnResultExecuting(ResultExecutingContext context)
        {
            context.HttpContext.Response.Headers.Add(_name, _value);

            base.OnResultExecuting(context);
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            // Do something before the action executes.
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Do something before the action executes.
        }

        public override void OnResultExecuted(ResultExecutedContext context)
        {
            base.OnResultExecuted(context);
        }
    }

    public class ResponseHeaderProxyAttribute : ActionFilterAttribute
    {
        private readonly string _name;
        private readonly string _value;

        public ResponseHeaderProxyAttribute(string name, string value) =>
            (_name, _value) = (name, value);

        public override void OnResultExecuting(ResultExecutingContext context)
        {
            context.HttpContext.Response.Headers.Add(_name, _value);

            base.OnResultExecuting(context);
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            // Do something before the action executes.
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Do something before the action executes.
        }

        public override void OnResultExecuted(ResultExecutedContext context)
        {
            base.OnResultExecuted(context);
        }
    }
}