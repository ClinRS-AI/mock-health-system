using MockHealthSystem.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MockHealthSystem.Api.Filters;

/// <summary>
/// Action filter that returns a consistent 400 error response when model validation fails.
/// Use for cross-cutting validation behavior on request models (Data Annotations or Fluent Validation).
/// </summary>
public sealed class ModelValidationActionFilter : IActionFilter
{
    /// <inheritdoc />
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid)
        {
            return;
        }

        var errors = context.ModelState
            .Where(ms => ms.Value?.Errors.Count > 0)
            .SelectMany(ms => ms.Value!.Errors.Select(e => new { Key = ms.Key, Message = e.ErrorMessage }))
            .ToList();

        var detail = errors.Count == 0
            ? "One or more validation errors occurred."
            : string.Join(" ", errors.Select(e => $"{e.Key}: {e.Message}"));

        context.Result = new BadRequestObjectResult(new ApiErrorResponse
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Failed",
            Detail = detail,
            TraceId = context.HttpContext.TraceIdentifier
        });
    }

    /// <inheritdoc />
    public void OnActionExecuted(ActionExecutedContext context)
    {
        // No post-action logic for validation filter.
    }
}
