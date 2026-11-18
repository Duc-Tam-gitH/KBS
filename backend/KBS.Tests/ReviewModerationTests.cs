using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KBS.DAL.Data;
using KBS.Models.DTOs.Ai;
using KBS.Models.DTOs.Authentication;
using KBS.Models.DTOs.Products;
using KBS.Models.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KBS.Tests;

public class ReviewModerationTests
{
    private const string Key = "review-worker-test-key-at-least-32-characters";
    private sealed class Factory : TestApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("Ai:WorkerKey", Key);
        }
    }

    [Fact]
    public async Task Worker_flags_staff_moderates_and_public_statistics_exclude_hidden_reviews()
    {
        using var app = new Factory();
        using var client = app.CreateClient();
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KbsDbContext>();
        var user = await db.Users.SingleAsync(x => x.Username == "customer");
        var module = await db.AiModules.SingleAsync(x => x.Id == "ai_reviewnpl");
        var product = new Product
        {
            Name = "Review test", Category = new Category { Name = "Review category" },
            Brand = new Brand { Name = "Review brand" }
        };
        var review = new ProductReview
        {
            Product = product, UserId = user.Id, Rating = 1, Comment = "kiếm tiền online"
        };
        db.ProductReviews.Add(review);
        db.ProductReviews.Add(new ProductReview
        {
            Product = product, UserId = await db.Users.Where(x => x.Username == "staff").Select(x => x.Id).SingleAsync(),
            Rating = 5, Comment = "Gõ rất tốt", ModerationStatus = "APPROVED"
        });
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/reviews")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/api/ai-worker/reviewnpl/poll", new { })).StatusCode);
        client.DefaultRequestHeaders.Add("X-AI-Key", Key);
        var pollResponse = await client.PostAsJsonAsync("/api/ai-worker/reviewnpl/poll", new { });
        Assert.False((await pollResponse.Content.ReadFromJsonAsync<ReviewPoll>())!.IsEnabled);
        var decision = new { decision = "FLAGGED", matchedKeywords = new[] { "kiem tien online" } };
        var workerPath = $"/api/ai-worker/reviewnpl/reviews/{review.Id}/decision";
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(workerPath, decision)).StatusCode);
        module.IsEnabled = true;
        await db.SaveChangesAsync();
        var poll = await (await client.PostAsJsonAsync("/api/ai-worker/reviewnpl/poll", new { }))
            .Content.ReadFromJsonAsync<ReviewPoll>();
        Assert.Equal(review.Id, Assert.Single(poll!.Reviews).Id);
        (await client.PostAsJsonAsync(workerPath, decision)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync(workerPath, decision)).EnsureSuccessStatusCode();
        Assert.Equal(1, await db.AiEvents.CountAsync(x => x.Kind == "REVIEW_FLAGGED"));
        var publicPath = $"/api/products/{product.Id}/reviews";
        Assert.Equal(2, (await client.GetFromJsonAsync<ProductReviewsResponseDto>(publicPath))!.TotalReviews);
        await Login(client, "customer", "Customer1");
        var moderationPath = $"/api/admin/reviews/{review.Id}/moderation";
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PutAsJsonAsync(moderationPath, new { status = "HIDDEN" })).StatusCode);
        await Login(client, "admin", "Admin123");
        (await client.GetAsync("/api/admin/reviews?status=FLAGGED")).EnsureSuccessStatusCode();
        (await client.PutAsJsonAsync(moderationPath, new { status = "HIDDEN" })).EnsureSuccessStatusCode();
        var visible = await client.GetFromJsonAsync<ProductReviewsResponseDto>(publicPath);
        Assert.Equal(1, visible!.TotalReviews);
        Assert.Equal(5, visible.AverageRating);
        Assert.False(visible.RatingCounts.ContainsKey(1));
        (await client.PostAsJsonAsync(workerPath, new { decision = "APPROVED", matchedKeywords = Array.Empty<string>() }))
            .EnsureSuccessStatusCode();
        await db.Entry(review).ReloadAsync();
        Assert.Equal("HIDDEN", review.ModerationStatus);
        Assert.Equal("kiếm tiền online", review.Comment);
        Assert.Equal("kiem tien online", review.ModerationReason);
        module.IsEnabled = false;
        await db.SaveChangesAsync();
        (await client.PutAsJsonAsync(moderationPath, new { status = "APPROVED" })).EnsureSuccessStatusCode();
        Assert.Equal(2, (await client.GetFromJsonAsync<ProductReviewsResponseDto>(publicPath))!.TotalReviews);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PutAsJsonAsync(moderationPath, new { status = "PENDING" })).StatusCode);
    }

    private static async Task Login(
        HttpClient client,
        string name,
        string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { loginIdentifier = name, password });
        response.EnsureSuccessStatusCode();
        var session = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);
    }
}
