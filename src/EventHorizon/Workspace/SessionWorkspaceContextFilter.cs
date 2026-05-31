using EventHorizon.Engine.Sessions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EventHorizon.Workspace;

public sealed class SessionWorkspaceContextFilter : IAsyncActionFilter
{
    private readonly ISessionStore _sessionStore;
    private readonly IWorkspaceContextAccessor _workspaceContextAccessor;

    public SessionWorkspaceContextFilter(ISessionStore sessionStore, IWorkspaceContextAccessor workspaceContextAccessor)
    {
        _sessionStore = sessionStore;
        _workspaceContextAccessor = workspaceContextAccessor;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!TryGetSessionId(context, out var sessionId))
        {
            await next().ConfigureAwait(false);
            return;
        }

        var session = await _sessionStore.LoadAsync(sessionId, context.HttpContext.RequestAborted).ConfigureAwait(false);
        if (session is null)
        {
            context.Result = new NotFoundResult();
            return;
        }

        if (string.IsNullOrWhiteSpace(session.WorkspaceRoot))
        {
            context.Result = new BadRequestObjectResult(new ProblemDetails
            {
                Title = "Session workspace root is not configured.",
            });
            return;
        }

        _workspaceContextAccessor.WorkspaceContext = new WorkspaceContext(session.WorkspaceRoot);
        await next().ConfigureAwait(false);
    }

    private static bool TryGetSessionId(ActionExecutingContext context, out string sessionId)
    {
        sessionId = string.Empty;
        if (!context.RouteData.Values.TryGetValue("sessionId", out var value) || value is null)
        {
            return false;
        }

        sessionId = value.ToString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(sessionId);
    }
}
