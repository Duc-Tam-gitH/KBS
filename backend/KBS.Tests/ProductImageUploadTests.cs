using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using KBS.Models.DTOs.Authentication;
using KBS.Models.DTOs.Brands;
using KBS.Models.DTOs.Products;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace KBS.Tests;

public class ProductImageUploadTests
{
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aXioAAAAASUVORK5CYII=");

    [Fact]
    public async Task Upload_requires_admin_validates_files_and_preserves_images_on_edit()
    {
        using var app = new ImageFactory();
        using var client = app.CreateClient();
        var dto = new ProductCreateDto { Name = "Image test", Price = 100, CategoryId = 1 };
        using (var body = Form(dto, Png))
        {
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await client.PostAsync("/api/products/with-images", body)).StatusCode);
        }
        await Login(client, "customer", "Customer1");
        using (var body = Form(dto, Png))
        {
            Assert.Equal(HttpStatusCode.Forbidden,
                (await client.PostAsync("/api/products/with-images", body)).StatusCode);
        }
        await Login(client, "admin", "Admin123");
        var brandResponse = await client.PostAsJsonAsync("/api/brands", new { name = "Upload test" });
        brandResponse.EnsureSuccessStatusCode();
        dto.BrandId = (await brandResponse.Content.ReadFromJsonAsync<BrandResponseDto>())!.Id;
        foreach (var invalid in new[] { Array.Empty<byte>(), "not an image"u8.ToArray(), new byte[5 * 1024 * 1024 + 1] })
        {
            using var body = Form(dto, invalid);
            Assert.Equal(HttpStatusCode.BadRequest,
                (await client.PostAsync("/api/products/with-images", body)).StatusCode);
        }
        using (var body = Form(dto, Enumerable.Repeat(Png, 11).ToArray()))
        {
            Assert.Equal(HttpStatusCode.BadRequest,
                (await client.PostAsync("/api/products/with-images", body)).StatusCode);
        }
        dto.CategoryId = long.MaxValue;
        using (var body = Form(dto, Png))
        {
            Assert.Equal(HttpStatusCode.BadRequest,
                (await client.PostAsync("/api/products/with-images", body)).StatusCode);
            Assert.Empty(Directory.Exists(app.Images) ? Directory.GetFiles(app.Images) : []);
            dto.CategoryId = 1;
        }
        using var createBody = Form(dto, Png, Png);
        var response = await client.PostAsync("/api/products/with-images", createBody);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = (await response.Content.ReadFromJsonAsync<ProductResponseDto>())!;
        Assert.Equal(2, product.Images.Count);
        Assert.Equal(new[] { 0, 1 }, product.Images.Select(x => x.SortOrder));
        var original = product.Images[0].ImageUrl;
        using var publicClient = app.CreateClient();
        var image = await publicClient.GetAsync(new Uri(original).PathAndQuery);
        Assert.Equal("image/png", image.Content.Headers.ContentType!.MediaType);
        Assert.Equal(Png, await image.Content.ReadAsByteArrayAsync());
        Assert.Equal("nosniff", Assert.Single(image.Headers.GetValues("X-Content-Type-Options")));

        var update = new ProductUpdateDto
        {
            Name = product.Name, Price = 200, CategoryId = product.CategoryId, BrandId = product.BrandId,
            IsActive = true, Images = [product.Images[0]]
        };
        using var updateBody = Form(update, Png);
        var updatedResponse = await client.PutAsync($"/api/products/{product.Id}/with-images", updateBody);
        updatedResponse.EnsureSuccessStatusCode();
        var updated = (await updatedResponse.Content.ReadFromJsonAsync<ProductResponseDto>())!;
        Assert.Equal(2, updated.Images.Count);
        Assert.Equal(original, updated.Images[0].ImageUrl);
        Assert.NotEqual(product.Images[1].ImageUrl, updated.Images[1].ImageUrl);
        var catalog = await publicClient.GetFromJsonAsync<ProductResponseDto>($"/api/products/{product.Id}");
        Assert.Equal(updated.Images.Select(x => x.ImageUrl), catalog!.Images.Select(x => x.ImageUrl));
    }

    private static MultipartFormDataContent Form(object product, params byte[][] images)
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent(JsonSerializer.Serialize(product)), "product");
        foreach (var image in images)
        {
            var content = new ByteArrayContent(image);
            content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(content, "files", "../../untrusted.png");
        }
        return form;
    }

    private static async Task Login(
        HttpClient client,
        string username,
        string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { loginIdentifier = username, password });
        response.EnsureSuccessStatusCode();
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
    }

    private sealed class ImageFactory : TestApiFactory
    {
        public string Images { get; } = Path.Combine(Path.GetTempPath(), "kbs-images-" + Guid.NewGuid());

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Media:ProductImageDirectory"] = Images }));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && Directory.Exists(Images)) Directory.Delete(Images, true);
        }
    }
}
