using Microsoft.AspNetCore.Mvc.Filters;

namespace t1_frame.webapi.Filters
{
    public class LoggingResponseHeaderFilterService : IResultFilter
    {
        private readonly ILogger _logger;

        public LoggingResponseHeaderFilterService(
                ILogger<LoggingResponseHeaderFilterService> logger) =>
            _logger = logger;

        public void OnResultExecuting(ResultExecutingContext context)
        {
            _logger.LogInformation(
                $"- {nameof(LoggingResponseHeaderFilterService)}.{nameof(OnResultExecuting)}");

            context.HttpContext.Response.Headers.Add(
                nameof(OnResultExecuting), nameof(LoggingResponseHeaderFilterService));
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {
            _logger.LogInformation(
                $"- {nameof(LoggingResponseHeaderFilterService)}.{nameof(OnResultExecuted)}");
        }
    }

    public class LoggingResponseHeaderFilter : ResultFilterAttribute
    {
        private readonly ILogger _logger;
        private readonly string _name;
        private readonly string _value;

        public LoggingResponseHeaderFilter(
                 string value, ILogger<LoggingResponseHeaderFilter> logger, string name)
        {
            (_name, _value) = (name, value);
            _logger = logger;
        }

        public override void OnResultExecuting(ResultExecutingContext context)
        {
            _logger.LogInformation(
                $"- {nameof(LoggingResponseHeaderFilterService)}.{nameof(OnResultExecuting)}");

            //context.HttpContext.Response.Headers.Add(
            //    nameof(OnResultExecuting), nameof(LoggingResponseHeaderFilter));
            context.HttpContext.Response.Headers.Add(_name, _value);
        }

        public override void OnResultExecuted(ResultExecutedContext context)
        {
            _logger.LogInformation(
                $"- {nameof(LoggingResponseHeaderFilterService)}.{nameof(OnResultExecuted)}");
        }
    }
}