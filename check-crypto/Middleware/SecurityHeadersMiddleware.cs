namespace check_crypto.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SecurityHeadersMiddleware> _logger;

        private readonly HashSet<string> _sensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
        {
            // Binance rate limit headers
            "x-mbx-used-weight",
            "x-mbx-used-weight-1m",
            "x-mbx-order-count-10s",
            "x-mbx-order-count-1d",
            "x-mbx-uuid",
            "retry-after",
            
            // General API security headers
            "x-ratelimit-limit",
            "x-ratelimit-remaining",
            "x-ratelimit-reset",
            "x-rate-limit-limit",
            "x-rate-limit-remaining",
            "x-rate-limit-reset",
            
            // Server internal headers
            "server",
            "x-powered-by",
            "x-aspnet-version",
            "x-sourcefiles",
            
            // Cloud provider headers
            "x-cache",
            "x-amz-cf-pop",
            "x-amz-cf-id",
            "via",
            
            // Internal debugging
            "x-debug-token",
            "x-debug-token-link"
        };

        public SecurityHeadersMiddleware(RequestDelegate next, ILogger<SecurityHeadersMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Execute the next middleware in the pipeline
            await _next(context);

            // Filter sensitive headers from response
            FilterSensitiveHeaders(context.Response);
            
            // Add security headers
            AddSecurityHeaders(context.Response);
        }

        private void FilterSensitiveHeaders(HttpResponse response)
        {
            var headersToRemove = new List<string>();

            foreach (var header in response.Headers)
            {
                if (_sensitiveHeaders.Contains(header.Key))
                {
                    headersToRemove.Add(header.Key);
                    _logger.LogDebug("Filtered sensitive header: {HeaderName}", header.Key);
                }
            }

            foreach (var headerName in headersToRemove)
            {
                response.Headers.Remove(headerName);
            }
        }

        private void AddSecurityHeaders(HttpResponse response)
        {
            // Only add if not already present
            if (!response.Headers.ContainsKey("X-Content-Type-Options"))
                response.Headers["X-Content-Type-Options"] = "nosniff";

            if (!response.Headers.ContainsKey("X-Frame-Options"))
                response.Headers["X-Frame-Options"] = "DENY";

            if (!response.Headers.ContainsKey("X-XSS-Protection"))
                response.Headers["X-XSS-Protection"] = "1; mode=block";

            if (!response.Headers.ContainsKey("Referrer-Policy"))
                response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        }
    }
}