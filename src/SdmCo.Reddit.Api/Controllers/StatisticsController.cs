using System.Net;
using Microsoft.AspNetCore.Mvc;
using SdmCo.Reddit.Api.Entities.Dtos;
using SdmCo.Reddit.Api.Persistence;

namespace SdmCo.Reddit.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class StatisticsController : ControllerBase
{
    private readonly ILogger<StatisticsController> _logger;
    private readonly IRedditRepository _repository;

    public StatisticsController(IRedditRepository repository, ILogger<StatisticsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet("{subreddit:alpha}")]
    [ProducesResponseType(typeof(StatisticsResponse), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<StatisticsResponse>> GetStatistics(string subreddit)
    {
        _logger.LogDebug("Statistics request received for subreddit {SubredditName}", subreddit);

        var post = await _repository.GetMostUpvotedPostAsync(subreddit);
        var user = await _repository.GetUserWithMostPostsAsync(subreddit);

        var mostUpdvotedPost = new MostUpvotedPost(post.Title, post.Ups);
        var userWithMostPosts = new UserWithMostPosts(user.Username, user.PostCount);

        var response = new StatisticsResponse(subreddit, mostUpdvotedPost, userWithMostPosts);

        _logger.LogDebug("Returning statistics response {StatisticsResponse}", response);

        return Ok(response);
    }
}