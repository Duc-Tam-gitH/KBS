using System.Text;

namespace KBS.DAL.Data;

/// <summary>
/// Đọc nội dung các tệp SQL được nhúng trong assembly KBS.DAL.
/// Các tệp .sql nằm trong thư mục database ở gốc kho mã và được khai báo
/// bằng EmbeddedResource trong KBS.DAL.csproj.
/// </summary>
internal static class SqlResourceReader
{
    /// <summary>
    /// Đọc toàn bộ nội dung của một tài nguyên nhúng theo tên đầy đủ.
    /// </summary>
    /// <param name="resourceName">
    /// Tên tài nguyên nhúng, ví dụ "KBS.DAL.Sql.seed_data.sql".
    /// </param>
    /// <returns>Nội dung tệp SQL dưới dạng chuỗi UTF-8.</returns>
    internal static string Read(string resourceName)
    {
        var assembly = typeof(SqlResourceReader).Assembly;

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Không tìm thấy tài nguyên nhúng '{resourceName}'. " +
                $"Tài nguyên hiện có: {string.Join(", ", assembly.GetManifestResourceNames())}");

        // Đọc theo UTF-8 để giữ đúng tiếng Việt trong dữ liệu mẫu.
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
