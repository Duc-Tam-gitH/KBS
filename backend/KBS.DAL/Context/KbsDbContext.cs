using Microsoft.EntityFrameworkCore;

namespace KBS.DAL.Context;

public class KbsDbContext(DbContextOptions<KbsDbContext> options) : DbContext(options)
{
}
