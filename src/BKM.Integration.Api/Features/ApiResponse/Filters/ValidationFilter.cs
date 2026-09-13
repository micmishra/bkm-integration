using BKM.Integration.Application.Features.ApiResponse.Builders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BKM.Integration.Api.Features.ApiResponse.Filters;

/// <summary>
/// Action filter that intercepts invalid ModelState before the controller action runs
/// and returns a standardised <see cref="Application.Features.ApiResponse.Models.ApiResponse{T}"/>
/// validation error response — so controllers never need to check ModelState.IsValid manually.
///
/// Registered globally in Program.cs via AddControllers(o => o.Filters.Add&lt;...&gt;())
/// </summary>
public sealed class ValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid) return;

        var fieldErrors = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .SelectMany(x => x.Value!.Errors
                .Select(e => (Field: x.Key, Message: e.ErrorMessage)));

        var response = ApiResponseBuilder.ValidationError(fieldErrors);

        context.Result = new BadRequestObjectResult(response);
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
