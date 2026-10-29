using InvoiceApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace InvoiceApi.Filters
{
    public class ApiExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            switch (context.Exception)
            {
                case NotFoundException ex:
                    context.Result = new NotFoundObjectResult(new { message = ex.Message });
                    context.ExceptionHandled = true;
                    break;
                case ForbiddenException ex:
                    context.Result = new ObjectResult(new { message = ex.Message }) { StatusCode = 403 };
                    context.ExceptionHandled = true;
                    break;
                case BusinessRuleException ex:
                    context.Result = new BadRequestObjectResult(new { message = ex.Message });
                    context.ExceptionHandled = true;
                    break;
            }
        }
    }
}
