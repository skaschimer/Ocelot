using Microsoft.AspNetCore.Http;
using Ocelot.Authorization.Middleware;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Headers;
using Ocelot.Headers.Middleware;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;

namespace Ocelot.UnitTests.Headers;

public class HttpHeadersTransformationMiddlewareTests : UnitTest
{
    private readonly Mock<IHttpContextRequestHeaderReplacer> _preReplacer;
    private readonly Mock<IHttpResponseHeaderReplacer> _postReplacer;
    private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
    private readonly Mock<IOcelotLogger> _logger;
    private readonly HttpHeadersTransformationMiddleware _middleware;
    private readonly RequestDelegate _next;
    private readonly Mock<IAddHeadersToResponse> _addHeadersToResponse;
    private readonly Mock<IAddHeadersToRequest> _addHeadersToRequest;
    private readonly DefaultHttpContext _httpContext;

    public HttpHeadersTransformationMiddlewareTests()
    {
        _httpContext = new DefaultHttpContext();
        _preReplacer = new Mock<IHttpContextRequestHeaderReplacer>();
        _postReplacer = new Mock<IHttpResponseHeaderReplacer>();
        _loggerFactory = new Mock<IOcelotLoggerFactory>();
        _logger = new Mock<IOcelotLogger>();
        _loggerFactory.Setup(x => x.CreateLogger<AuthorizationMiddleware>()).Returns(_logger.Object);
        _next = context => Task.CompletedTask;
        _addHeadersToResponse = new Mock<IAddHeadersToResponse>();
        _addHeadersToRequest = new Mock<IAddHeadersToRequest>();
        _middleware = new HttpHeadersTransformationMiddleware(
            _next, _loggerFactory.Object, _preReplacer.Object,
            _postReplacer.Object, _addHeadersToResponse.Object, _addHeadersToRequest.Object);
    }

    [Fact]
    public async Task Should_call_pre_and_post_header_transforms()
    {
        // Arrange
        GivenTheFollowingRequest();
        GivenTheDownstreamRequestIs();
        GivenTheRouteHasPreFindAndReplaceSetUp();
        GivenTheHttpResponseMessageIs();

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        ThenTheIHttpContextRequestHeaderReplacerIsCalledCorrectly();
        ThenAddHeadersToRequestIsCalledCorrectly();
        ThenTheIHttpResponseHeaderReplacerIsCalledCorrectly();
        ThenAddHeadersToResponseIsCalledCorrectly();
    }

    private void ThenAddHeadersToResponseIsCalledCorrectly()
    {
        _addHeadersToResponse
            .Verify(x => x.Add(_httpContext.Items.DownstreamRoute().AddHeadersToDownstream, _httpContext.Items.DownstreamResponse()), Times.Once);
    }

    private void ThenAddHeadersToRequestIsCalledCorrectly()
    {
        _addHeadersToRequest
            .Verify(x => x.SetHeadersOnDownstreamRequest(_httpContext.Items.DownstreamRoute().AddHeadersToUpstream, _httpContext), Times.Once);
    }

    private void GivenTheDownstreamRequestIs()
    {
        _httpContext.Items.UpsertDownstreamRequest(new DownstreamRequest(new HttpRequestMessage(HttpMethod.Get, "http://test.com")));
    }

    private void GivenTheHttpResponseMessageIs()
    {
        _httpContext.Items.UpsertDownstreamResponse(new DownstreamResponse(new HttpResponseMessage()));
    }

    private void GivenTheRouteHasPreFindAndReplaceSetUp()
    {
        var fAndRs = new List<HeaderFindAndReplace>();
        var route = new RouteBuilder()
            .WithDownstreamRoute(new DownstreamRouteBuilder().WithUpstreamHeaderFindAndReplace(fAndRs)
                .WithDownstreamHeaderFindAndReplace(fAndRs).Build())
            .Build();

        var dR = new Ocelot.DownstreamRouteFinder.DownstreamRouteHolder(null, route);

        _httpContext.Items.UpsertTemplatePlaceholderNameAndValues(dR.TemplatePlaceholderNameAndValues);
        _httpContext.Items.UpsertDownstreamRoute(dR.Route.DownstreamRoute[0]);
    }

    private void ThenTheIHttpContextRequestHeaderReplacerIsCalledCorrectly()
    {
        _preReplacer.Verify(x => x.Replace(It.IsAny<HttpContext>(), It.IsAny<List<HeaderFindAndReplace>>()), Times.Once);
    }

    private void ThenTheIHttpResponseHeaderReplacerIsCalledCorrectly()
    {
        _postReplacer.Verify(x => x.Replace(It.IsAny<HttpContext>(), It.IsAny<List<HeaderFindAndReplace>>()), Times.Once);
    }

    private void GivenTheFollowingRequest()
    {
        _httpContext.Request.Headers.Append("test", "test");
    }
}
