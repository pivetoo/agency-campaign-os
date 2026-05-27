using AgencyCampaign.Application.Services;
using AgencyCampaign.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace AgencyCampaign.Infrastructure.Clients
{
    public sealed class ApifySocialMetricsClient : IApifySocialMetricsClient
    {
        private readonly HttpClient httpClient;
        private readonly ApifyOptions options;
        private readonly ILogger<ApifySocialMetricsClient> logger;

        public ApifySocialMetricsClient(HttpClient httpClient, IOptions<ApifyOptions> options, ILogger<ApifySocialMetricsClient> logger)
        {
            this.httpClient = httpClient;
            this.options = options.Value;
            this.logger = logger;
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(options.Token) && options.Platforms.Count > 0;

        public async Task<SocialMetricsResult?> FetchAsync(string platformName, string url, CancellationToken cancellationToken = default)
        {
            if (!IsConfigured || string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(platformName))
            {
                return null;
            }

            ApifyPlatformProfile? profile = options.Platforms.FirstOrDefault(item =>
                !string.IsNullOrWhiteSpace(item.Match) &&
                platformName.Contains(item.Match, StringComparison.OrdinalIgnoreCase));

            if (profile is null || string.IsNullOrWhiteSpace(profile.ActorId))
            {
                return null;
            }

            try
            {
                object urlValue = profile.UrlAsObject ? new[] { new { url } } : (object)new[] { url };
                Dictionary<string, object?> input = new()
                {
                    [profile.UrlField] = urlValue,
                    ["resultsLimit"] = 1
                };

                string requestUri = $"https://api.apify.com/v2/acts/{profile.ActorId}/run-sync-get-dataset-items";
                using HttpRequestMessage request = new(HttpMethod.Post, requestUri)
                {
                    Content = JsonContent.Create(input)
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Token);

                using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Apify run failed for platform {Platform} with status {Status}.", platformName, response.StatusCode);
                    return null;
                }

                await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                if (document.RootElement.ValueKind != JsonValueKind.Array || document.RootElement.GetArrayLength() == 0)
                {
                    return null;
                }

                JsonElement item = document.RootElement[0];
                return new SocialMetricsResult
                {
                    Likes = ReadInt(item, profile.LikesField),
                    Comments = ReadInt(item, profile.CommentsField),
                    Views = ReadLong(item, profile.ViewsField),
                    Shares = ReadInt(item, profile.SharesField)
                };
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Apify fetch failed for platform {Platform}.", platformName);
                return null;
            }
        }

        public async Task<SocialProfileResult?> FetchProfileAsync(string platformName, string? handle, string? profileUrl, CancellationToken cancellationToken = default)
        {
            if (!IsConfigured || string.IsNullOrWhiteSpace(platformName))
            {
                return null;
            }

            ApifyPlatformProfile? profile = options.Platforms.FirstOrDefault(item =>
                !string.IsNullOrWhiteSpace(item.Match) &&
                platformName.Contains(item.Match, StringComparison.OrdinalIgnoreCase));

            if (profile is null || string.IsNullOrWhiteSpace(profile.ProfileActorId))
            {
                return null;
            }

            string? value = profile.ProfileUsesHandle ? handle?.TrimStart('@').Trim() : profileUrl?.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            try
            {
                object inputValue = profile.ProfileUrlAsObject ? new[] { new { url = value } } : (object)new[] { value };
                Dictionary<string, object?> input = new()
                {
                    [profile.ProfileInputField] = inputValue,
                    ["resultsLimit"] = 1
                };

                string requestUri = $"https://api.apify.com/v2/acts/{profile.ProfileActorId}/run-sync-get-dataset-items";
                using HttpRequestMessage request = new(HttpMethod.Post, requestUri)
                {
                    Content = JsonContent.Create(input)
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Token);

                using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Apify profile run failed for platform {Platform} with status {Status}.", platformName, response.StatusCode);
                    return null;
                }

                await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                if (document.RootElement.ValueKind != JsonValueKind.Array || document.RootElement.GetArrayLength() == 0)
                {
                    return null;
                }

                JsonElement item = document.RootElement[0];
                return new SocialProfileResult
                {
                    Followers = ReadLong(item, profile.FollowersField),
                    EngagementRate = ReadDecimal(item, profile.ProfileEngagementField)
                };
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Apify profile fetch failed for platform {Platform}.", platformName);
                return null;
            }
        }

        private static decimal? ReadDecimal(JsonElement item, string? field)
        {
            if (string.IsNullOrWhiteSpace(field) || !TryResolve(item, field, out JsonElement value))
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out decimal number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal parsed))
            {
                return parsed;
            }

            return null;
        }

        private static long? ReadLong(JsonElement item, string? field)
        {
            if (string.IsNullOrWhiteSpace(field) || !TryResolve(item, field, out JsonElement value))
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out long number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String && long.TryParse(value.GetString(), out long parsed))
            {
                return parsed;
            }

            return null;
        }

        private static int? ReadInt(JsonElement item, string? field)
        {
            long? value = ReadLong(item, field);
            if (!value.HasValue)
            {
                return null;
            }

            return value.Value > int.MaxValue ? int.MaxValue : (int)value.Value;
        }

        // Resolve um campo que pode ser aninhado via caminho com pontos (ex.: "authorMeta.fans").
        private static bool TryResolve(JsonElement item, string field, out JsonElement value)
        {
            JsonElement current = item;
            foreach (string segment in field.Split('.', StringSplitOptions.RemoveEmptyEntries))
            {
                if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out JsonElement next))
                {
                    value = default;
                    return false;
                }

                current = next;
            }

            value = current;
            return true;
        }
    }
}
