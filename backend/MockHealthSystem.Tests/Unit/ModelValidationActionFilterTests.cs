using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using MockHealthSystem.Api.Filters;
using MockHealthSystem.Api.Models;
using Xunit;

namespace MockHealthSystem.Tests.Unit;

public sealed class ModelValidationActionFilterTests
{
    private static ActionExecutingContext CreateContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "trace-123";

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            controller: new object());
    }

    [Fact]
    public void OnActionExecuting_SetsBadRequest_WhenModelStateInvalid()
    {
        var filter = new ModelValidationActionFilter();
        var context = CreateContext();
        context.ModelState.AddModelError("Name", "Name is required.");
        context.ModelState.AddModelError("Age", "Age must be positive.");

        filter.OnActionExecuting(context);

        var badRequest = Assert.IsType<BadRequestObjectResult>(context.Result);
        var payload = Assert.IsType<ApiErrorResponse>(badRequest.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, payload.Status);
        Assert.Equal("Validation Failed", payload.Title);
        Assert.Contains("Name: Name is required.", payload.Detail);
        Assert.Contains("Age: Age must be positive.", payload.Detail);
        Assert.Equal("trace-123", payload.TraceId);
    }

    [Fact]
    public void OnActionExecuting_DoesNothing_WhenModelStateValid()
    {
        var filter = new ModelValidationActionFilter();
        var context = CreateContext();

        filter.OnActionExecuting(context);

        Assert.Null(context.Result);
    }
}
