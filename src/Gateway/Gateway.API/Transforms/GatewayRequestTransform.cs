using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace Gateway.API.Transforms;

public class GatewayRequestTransformProvider : ITransformProvider
{
    public void ValidateRoute(TransformRouteValidationContext context)
    {
        // Validation logic if needed
    }

    public void ValidateCluster(TransformClusterValidationContext context)
    {
        // Validation logic if needed
    }

    public void Apply(TransformBuilderContext context)
    {
        // Add request ID header
        context.AddRequestTransform(transformContext =>
        {
            var requestId = transformContext.HttpContext.TraceIdentifier;
            transformContext.ProxyRequest.Headers.Add("X-Request-Id", requestId);
            transformContext.ProxyRequest.Headers.Add("X-Gateway-Source", "API-Gateway");
            transformContext.ProxyRequest.Headers.Add("X-Gateway-Timestamp", DateTimeOffset.UtcNow.ToString("O"));
            
            return ValueTask.CompletedTask;
        });

        // Add response headers
        context.AddResponseTransform(transformContext =>
        {
            transformContext.HttpContext.Response.Headers.Add("X-Gateway-Processed", "true");
            transformContext.HttpContext.Response.Headers.Add("X-Request-Id", transformContext.HttpContext.TraceIdentifier);
            
            return ValueTask.CompletedTask;
        });

        // Log request/response
        context.AddRequestTransform(async transformContext =>
        {
            var logger = transformContext.HttpContext.RequestServices
                .GetRequiredService<ILogger<GatewayRequestTransformProvider>>();

            logger.LogInformation("Proxying request {RequestId} to {Destination}", 
                transformContext.HttpContext.TraceIdentifier,
                transformContext.DestinationPrefix);

            await ValueTask.CompletedTask;
        });
    }
}