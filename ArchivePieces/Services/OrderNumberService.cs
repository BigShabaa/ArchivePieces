using ArchivePieces.Data;

namespace ArchivePieces.Services
{
    public class OrderNumberService
    {
        private readonly AppDbContext _db;

        public OrderNumberService(AppDbContext db)
        {
            _db = db;
        }

        public string Generate()
        {
            var year = DateTime.UtcNow.Year;
            var count = _db.Orders.Count(o => o.CreatedAt.Year == year) + 1;
            return $"AP-{year}{count:D5}";
        }
    }
}
