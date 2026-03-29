namespace server.domain.player;

public interface IMarketListingRepository
{
    Task<MarketListing?> GetAsync(MarketListingId id);
    Task<IReadOnlyList<MarketListing>> GetActiveAsync(DateTimeOffset now);
    Task<IReadOnlyList<MarketListing>> GetBySellerAsync(PlayerId sellerId, DateTimeOffset now);
    Task<IReadOnlyList<MarketListing>> GetExpiredAsync(DateTimeOffset now);
    Task SaveAsync(MarketListing listing);
    Task DeleteAsync(MarketListingId id);
}
