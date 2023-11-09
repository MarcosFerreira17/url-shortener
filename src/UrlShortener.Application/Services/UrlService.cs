using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Newtonsoft.Json;
using UrlShortener.Application.DTO;
using UrlShortener.Application.Helpers;
using UrlShortener.Application.Interfaces;
using UrlShortener.Domain.Url.Entities;
using UrlShortener.Domain.Url.Repositories.Interfaces;

namespace UrlShortener.Application.Services;
public class UrlService : IUrlService
{
    private readonly IUrlRepository _urlRepository;
    private readonly ILogger<UrlService> _logger;
    private readonly ICacheService _cache;
    public UrlService(IUrlRepository urlRepository, ILogger<UrlService> logger, ICacheService cache)
    {
        _urlRepository = urlRepository ?? throw new ArgumentNullException(nameof(urlRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }
    public async Task<ShortUrlDTO> CreateShortUrlAsync(UrlDTO urlDTO)
    {
        ShortUrl shortUrl = new(DateTime.Now, urlDTO.Url);

        await _urlRepository.InsertOneAsync(shortUrl);

        string applicationUrl = EnvironmentProperties.GetApplicationUrl();

        return new ShortUrlDTO
        {
            ShortUrl = $"{applicationUrl}/{shortUrl.Text}",
            Hash = shortUrl.Text,
            OriginalUrl = shortUrl.LongUrlText,
            CreatedAt = shortUrl.CreatedAt
        };
    }

    public async Task<string> GetUrlAsync(string hash)
    {
        FilterDefinition<ShortUrl> filter = Builders<ShortUrl>.Filter.Eq(x => x.Text, hash);

        ShortUrl shortUrl = null;

        try
        {
            await _cache.GetOrSetAsync("shortUrl", async ()
                                                => shortUrl = await _urlRepository.GetByFilterAsync(filter));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while trying to get url from cache.");
            shortUrl = await _urlRepository.GetByFilterAsync(filter);
        }

        if (shortUrl is null)
            return null;

        return shortUrl.LongUrlText;
    }
}
