namespace WebApplication1
{
    using System.Security.Claims;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;

    public class ApiKeyHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IConfiguration _configuration;

        public ApiKeyHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IConfiguration configuration) : base(options, logger, encoder)
        {
            _configuration = configuration;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("dummy", out var apiKey))
            {
                return Task.FromResult(AuthenticateResult.Fail("API Key was not provided."));
            }

            var expectedKey = _configuration.GetValue<string>("ApiKey");

            if (apiKey.Count == 0 || apiKey != expectedKey)
                return Task.FromResult(AuthenticateResult.Fail("Invalid API Key."));

            var claims = new[] { new Claim(ClaimTypes.Name, "API Key User") };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
