using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using BitirmeProject.NotificationService.Api.Events.Handlers;
using BitirmeProject.NotificationService.Api.Models;
using BitirmeProject.NotificationService.Application.Features.Notifications.Commands.CreateNotification;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shared.Contracts.Events;

namespace NotificationService.UnitTests.Consumers;

public sealed class CommentAddedEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_Ignores_WhenIssueNotFound()
    {
        var logger = Substitute.For<ILogger<CommentAddedEventHandler>>();
        var mediator = Substitute.For<IMediator>();
        var factory = new TestHttpClientFactory(new HttpResponseMessage(HttpStatusCode.NotFound));

        var handler = new CommentAddedEventHandler(logger, mediator, factory);
        var evt = new CommentAddedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        await handler.HandleAsync(evt, CancellationToken.None);

        await mediator.DidNotReceive().Send(Arg.Any<CreateNotificationCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Ignores_WhenRecipientIsAuthor()
    {
        var logger = Substitute.For<ILogger<CommentAddedEventHandler>>();
        var mediator = Substitute.For<IMediator>();

        var authorId = Guid.NewGuid();
        var issue = new IssueDto { Id = Guid.NewGuid(), CreatedByUserId = authorId, AssigneeUserId = authorId };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(issue))
        };
        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        var factory = new TestHttpClientFactory(response);
        var handler = new CommentAddedEventHandler(logger, mediator, factory);
        var evt = new CommentAddedEvent(Guid.NewGuid(), issue.Id, Guid.NewGuid(), authorId, Guid.NewGuid());

        await handler.HandleAsync(evt, CancellationToken.None);

        await mediator.DidNotReceive().Send(Arg.Any<CreateNotificationCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SendsNotification_WhenRecipientDifferent()
    {
        var logger = Substitute.For<ILogger<CommentAddedEventHandler>>();
        var mediator = Substitute.For<IMediator>();

        var authorId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        var issue = new IssueDto { Id = Guid.NewGuid(), CreatedByUserId = Guid.NewGuid(), AssigneeUserId = assigneeId };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(issue))
        };
        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        var factory = new TestHttpClientFactory(response);
        var handler = new CommentAddedEventHandler(logger, mediator, factory);
        var evt = new CommentAddedEvent(Guid.NewGuid(), issue.Id, Guid.NewGuid(), authorId, Guid.NewGuid());

        await handler.HandleAsync(evt, CancellationToken.None);

        await mediator.Received(1).Send(Arg.Is<CreateNotificationCommand>(c =>
            c.UserId == assigneeId &&
            c.EntityType == "Issue" &&
            c.EntityId == issue.Id), Arg.Any<CancellationToken>());
    }

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public TestHttpClientFactory(HttpResponseMessage response)
        {
            _client = new HttpClient(new StubHandler(response)) { BaseAddress = new Uri("http://localhost") };
        }

        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public StubHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_response);
    }
}
