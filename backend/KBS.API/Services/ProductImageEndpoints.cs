using System.Buffers.Binary;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using KBS.BLL.Interfaces;
using KBS.Models.DTOs.Products;
using Microsoft.AspNetCore.Http.Features;

namespace KBS.API.Services;

public static class ProductImageEndpoints
{
    private const int ImageLimit = 5 * 1024 * 1024;
    private const long RequestLimit = 51L * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void MapProductImages(this WebApplication app)
    {
        var admin = app.MapGroup("/api/products").RequireAuthorization(policy => policy.RequireRole("Admin"));
        admin.MapPost("/with-images", (
            HttpContext context, IProductService products, IWebHostEnvironment env, IConfiguration config) =>
            SaveAsync(null, context, products, env, config));
        admin.MapPut("/{id:long}/with-images", (
            long id, HttpContext context, IProductService products, IWebHostEnvironment env, IConfiguration config) =>
            SaveAsync(id, context, products, env, config));

        app.MapGet("/api/product-images/{name}", IResult (
            string name, HttpContext context, IWebHostEnvironment env, IConfiguration config) =>
        {
            var extension = Path.GetExtension(name);
            var contentType = extension switch
            {
                ".png" => "image/png", ".jpg" => "image/jpeg", ".webp" => "image/webp", _ => null
            };
            if (contentType is null || !Guid.TryParseExact(Path.GetFileNameWithoutExtension(name), "N", out var id)
                || name != $"{id:N}{extension}") return TypedResults.NotFound();
            var path = Path.Combine(ImageDirectory(env, config), name);
            if (!File.Exists(path)) return TypedResults.NotFound();
            context.Response.Headers.XContentTypeOptions = "nosniff";
            context.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
            return TypedResults.PhysicalFile(path, contentType);
        }).AllowAnonymous();
    }

    private static async Task<IResult> SaveAsync(
        long? id,
        HttpContext context,
        IProductService products,
        IWebHostEnvironment env,
        IConfiguration config)
    {
        List<string> written = [];
        var saved = false;
        var ct = context.RequestAborted;
        try
        {
            var request = context.Request;
            if (!request.HasFormContentType)
                return TypedResults.BadRequest(new { message = "Gửi thông tin sản phẩm và ảnh bằng multipart/form-data." });
            if (request.ContentLength > RequestLimit) return TypedResults.StatusCode(StatusCodes.Status413PayloadTooLarge);
            var bodyLimit = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
            if (bodyLimit is { IsReadOnly: false }) bodyLimit.MaxRequestBodySize = RequestLimit;
            var form = await request.ReadFormAsync(new FormOptions
            {
                MultipartBodyLengthLimit = RequestLimit, ValueLengthLimit = 128 * 1024
            }, ct);
            var json = form["product"].ToString();
            var dto = JsonSerializer.Deserialize<ProductCreateDto>(json, JsonOptions)
                ?? throw new ArgumentException("Thiếu thông tin sản phẩm.");
            if (dto.Images is null || dto.Images.Any(image => image is null))
                throw new ArgumentException("Danh sách ảnh không hợp lệ.");
            List<ValidationResult> errors = [];
            var valid = Validator.TryValidateObject(dto, new ValidationContext(dto), errors, true);
            foreach (var image in dto.Images)
                valid &= Validator.TryValidateObject(image, new ValidationContext(image), errors, true);
            if (!valid) throw new ArgumentException(string.Join(" ", errors.Select(error => error.ErrorMessage)));
            if (dto.Images.Count + form.Files.Count > 10)
                throw new ArgumentException("Tối đa 10 ảnh cho một sản phẩm.");

            var origin = (config["Media:PublicBaseUrl"] ?? $"{request.Scheme}://{request.Host}{request.PathBase}")
                .TrimEnd('/');
            foreach (var file in form.Files)
            {
                if (file.Length is <= 0 or > ImageLimit)
                    throw new ArgumentException("Mỗi ảnh phải có nội dung và không vượt quá 5 MB.");
                using var buffer = new MemoryStream();
                await file.CopyToAsync(buffer, ct);
                var data = buffer.ToArray();
                var extension = ImageExtension(data, file.ContentType);
                var name = $"{Guid.NewGuid():N}{extension}";
                var directory = ImageDirectory(env, config);
                Directory.CreateDirectory(directory);
                var path = Path.Combine(directory, name);
                written.Add(path);
                await File.WriteAllBytesAsync(path, data, ct);
                dto.Images.Add(new ProductImageDto
                {
                    ImageUrl = $"{origin}/api/product-images/{name}", AltText = dto.Name
                });
            }
            for (var i = 0; i < dto.Images.Count; i++) dto.Images[i].SortOrder = i;
            ProductResponseDto? result;
            if (id.HasValue)
            {
                var update = JsonSerializer.Deserialize<ProductUpdateDto>(json, JsonOptions)!;
                update.Images = dto.Images;
                result = await products.UpdateAsync(id.Value, update, ct);
                if (result is null) return TypedResults.NotFound();
            }
            else result = await products.CreateAsync(dto, ct);
            saved = true;
            return id.HasValue ? TypedResults.Ok(result) : TypedResults.Created($"/api/products/{result.Id}", result);
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException
            or JsonException or InvalidDataException)
        {
            return TypedResults.BadRequest(new
            {
                message = error is JsonException or InvalidDataException ? "Dữ liệu tải lên không hợp lệ." : error.Message
            });
        }
        finally
        {
            if (!saved)
            {
                foreach (var path in written) File.Delete(path);
            }
        }
    }

    private static string ImageDirectory(IWebHostEnvironment env, IConfiguration config) =>
        Path.GetFullPath(config["Media:ProductImageDirectory"] ?? Path.Combine(env.ContentRootPath, "App_Data", "products"));

    private static string ImageExtension(byte[] data, string contentType)
    {
        var bytes = data.AsSpan();
        var png = bytes.Length >= 45 && bytes[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
            && bytes.Slice(12, 4).SequenceEqual("IHDR"u8) && bytes.Slice(bytes.Length - 8, 4).SequenceEqual("IEND"u8);
        var jpeg = bytes.Length >= 4 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255
            && bytes[^2] == 255 && bytes[^1] == 217;
        var webp = bytes.Length >= 20 && bytes[..4].SequenceEqual("RIFF"u8)
            && BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(4, 4)) == bytes.Length - 8
            && bytes.Slice(8, 4).SequenceEqual("WEBP"u8)
            && (bytes.Slice(12, 4).SequenceEqual("VP8 "u8) || bytes.Slice(12, 4).SequenceEqual("VP8L"u8)
                || bytes.Slice(12, 4).SequenceEqual("VP8X"u8));
        return contentType switch
        {
            "image/png" when png => ".png",
            "image/jpeg" when jpeg => ".jpg",
            "image/webp" when webp => ".webp",
            _ => throw new ArgumentException("Chỉ nhận ảnh JPG, PNG hoặc WebP có nội dung phù hợp định dạng.")
        };
    }
}
