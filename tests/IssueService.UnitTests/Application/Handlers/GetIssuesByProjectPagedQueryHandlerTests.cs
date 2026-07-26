using System.Collections.Concurrent;
using AutoMapper;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.DTOs;
using BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssuesByProjectPaged;
using BitirmeProject.IssueService.Application.ReadModels;
using BitirmeProject.IssueService.Domain.Entities;
using BitirmeProject.IssueService.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using NSubstitute;

namespace IssueService.UnitTests.Application.Handlers;

public sealed class GetIssuesByProjectPagedQueryHandlerTests
{
    private static (IIssueBoardRepository repo, IMapper mapper, IssueBoardItemDto dto) NewDependencies(Guid projectId)
    {
        var repo = Substitute.For<IIssueBoardRepository>();
        var mapper = Substitute.For<IMapper>();

        var issue = new Issue(projectId, "T1", null, IssuePriority.Low, Guid.NewGuid());
        var item = new IssueBoardItem(issue);
        var dto = new IssueBoardItemDto { IssueId = issue.Id, ProjectId = projectId, Title = "T1" };

        repo.GetByProjectIdPagedAsync(
                Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<Guid?>(),
                Arg.Any<bool>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<IssueBoardItem>)new List<IssueBoardItem> { item }, 1));
        mapper.Map<IssueBoardItemDto>(item).Returns(dto);

        return (repo, mapper, dto);
    }

    [Fact]
    public async Task Handle_CacheMiss_QueriesRepositoryAndReturnsMappedResult()
    {
        var projectId = Guid.NewGuid();
        var (repo, mapper, dto) = NewDependencies(projectId);
        var handler = new GetIssuesByProjectPagedQueryHandler(repo, mapper, new FakeDistributedCache());
        var query = new GetIssuesByProjectPagedQuery(projectId, 1, 20, null, false, Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(dto);
        result.TotalCount.Should().Be(1);
        await repo.Received(1).GetByProjectIdPagedAsync(
            Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<Guid?>(),
            Arg.Any<bool>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CacheHit_SecondCallServedFromCacheWithoutHittingRepository()
    {
        var projectId = Guid.NewGuid();
        var (repo, mapper, dto) = NewDependencies(projectId);
        var handler = new GetIssuesByProjectPagedQueryHandler(repo, mapper, new FakeDistributedCache());
        var query = new GetIssuesByProjectPagedQuery(projectId, 1, 20, null, false, Guid.NewGuid());

        var first = await handler.Handle(query, CancellationToken.None);
        var second = await handler.Handle(query, CancellationToken.None);

        second.Should().BeEquivalentTo(first);
        second.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(dto);
        // Repository hit exactly once — the second call was served from the cache.
        await repo.Received(1).GetByProjectIdPagedAsync(
            Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<Guid?>(),
            Arg.Any<bool>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    // Minimal in-memory IDistributedCache so the test exercises the real GetStringAsync/
    // SetStringAsync round-trip without depending on a concrete cache package.
    private sealed class FakeDistributedCache : IDistributedCache
    {
        private readonly ConcurrentDictionary<string, byte[]> _store = new();

        public byte[]? Get(string key) => _store.TryGetValue(key, out var value) ? value : null;

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult(Get(key));

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => _store[key] = value;

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }

        public void Refresh(string key) { }

        public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

        public void Remove(string key) => _store.TryRemove(key, out _);

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }
    }
}
