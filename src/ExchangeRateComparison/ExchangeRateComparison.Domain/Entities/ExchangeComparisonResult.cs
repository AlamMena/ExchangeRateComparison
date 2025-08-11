using ExchangeRateComparison.Domain.Enums;

namespace ExchangeRateComparison.Domain.Entities;

/// <summary>
/// Represents the result of comparing exchange rates from multiple providers
/// </summary>
public record ExchangeComparisonResult
{
    public ProcessStatus Status { get; init; }
    public ExchangeRequest Input { get; init; }
    public ExchangeRateOffer? BestOffer { get; init; }
    public IReadOnlyList<ExchangeRateOffer> AllOffers { get; init; }
    public DateTime ProcessedAt { get; init; }
    public TimeSpan ProcessingDuration { get; init; }

    public ExchangeComparisonResult(
        ProcessStatus status,
        ExchangeRequest input,
        ExchangeRateOffer? bestOffer,
        IEnumerable<ExchangeRateOffer> allOffers,
        DateTime processedAt,
        TimeSpan processingDuration)
    {
        Status = status;
        Input = input ?? throw new ArgumentNullException(nameof(input));
        BestOffer = bestOffer;
        AllOffers = allOffers?.ToList().AsReadOnly() ?? new List<ExchangeRateOffer>().AsReadOnly();
        ProcessedAt = processedAt;
        ProcessingDuration = processingDuration;

        // Validate business rules
        ValidateResult();
    }

    /// <summary>
    /// Determines if the process was successful (has at least one valid offer)
    /// </summary>
    public bool HasValidOffers => AllOffers.Any(offer => offer.IsSuccessful);

    /// <summary>
    /// Gets the number of successful offers
    /// </summary>
    public int SuccessfulOffersCount => AllOffers.Count(offer => offer.IsSuccessful);

    /// <summary>
    /// Gets the number of failed offers
    /// </summary>
    public int FailedOffersCount => AllOffers.Count(offer => !offer.IsSuccessful);

    /// <summary>
    /// Gets all successful offers ordered by converted amount (descending)
    /// </summary>
    public IEnumerable<ExchangeRateOffer> SuccessfulOffers => 
        AllOffers.Where(offer => offer.IsSuccessful)
                 .OrderByDescending(offer => offer.ConvertedAmount);

    /// <summary>
    /// Gets all failed offers ordered by provider name
    /// </summary>
    public IEnumerable<ExchangeRateOffer> FailedOffers => 
        AllOffers.Where(offer => !offer.IsSuccessful)
                 .OrderBy(offer => offer.ProviderName);

    /// <summary>
    /// Calculates the savings achieved by selecting the best offer vs worst offer
    /// </summary>
    public decimal CalculateSavings()
    {
        var successfulOffers = SuccessfulOffers.ToList();
        
        if (successfulOffers.Count < 2)
            return 0;

        var best = successfulOffers.First();
        var worst = successfulOffers.Last();
        
        return best.ConvertedAmount - worst.ConvertedAmount;
    }

    /// <summary>
    /// Gets the savings percentage between best and worst offers
    /// </summary>
    public decimal CalculateSavingsPercentage()
    {
        var successfulOffers = SuccessfulOffers.ToList();
        
        if (successfulOffers.Count < 2)
            return 0;

        var best = successfulOffers.First();
        var worst = successfulOffers.Last();

        return worst.ConvertedAmount > 0 
            ? ((best.ConvertedAmount - worst.ConvertedAmount) / worst.ConvertedAmount) * 100 
            : 0;
    }

    /// <summary>
    /// Creates a successful comparison result
    /// </summary>
    public static ExchangeComparisonResult CreateSuccessful(
        ExchangeRequest input,
        IEnumerable<ExchangeRateOffer> allOffers,
        TimeSpan processingDuration)
    {
        var offersList = allOffers.ToList();
        var bestOffer = offersList
            .Where(offer => offer.IsSuccessful)
            .OrderByDescending(offer => offer.ConvertedAmount)
            .FirstOrDefault();

        return new ExchangeComparisonResult(
            ProcessStatus.Completed,
            input,
            bestOffer,
            offersList,
            DateTime.UtcNow,
            processingDuration);
    }

    /// <summary>
    /// Creates a failed comparison result
    /// </summary>
    public static ExchangeComparisonResult CreateFailed(
        ExchangeRequest input,
        TimeSpan processingDuration,
        IEnumerable<ExchangeRateOffer>? partialOffers = null)
    {
        return new ExchangeComparisonResult(
            ProcessStatus.Failed,
            input,
            null,
            partialOffers ?? Array.Empty<ExchangeRateOffer>(),
            DateTime.UtcNow,
            processingDuration);
    }

    /// <summary>
    /// Validates the business rules for the result
    /// </summary>
    private void ValidateResult()
    {
        // If status is Completed and we have offers, there should be a best offer if any are successful
        if (Status == ProcessStatus.Completed && HasValidOffers && BestOffer == null)
        {
            throw new InvalidOperationException("Best offer cannot be null when there are successful offers");
        }

        // If we have a best offer, it must be successful
        if (BestOffer != null && !BestOffer.IsSuccessful)
        {
            throw new InvalidOperationException("Best offer must be successful");
        }

        // If we have a best offer, it must be in the all offers list
        if (BestOffer != null && !AllOffers.Contains(BestOffer))
        {
            throw new InvalidOperationException("Best offer must be included in the all offers list");
        }

        // Best offer should be the one with highest converted amount among successful offers
        if (BestOffer != null && HasValidOffers)
        {
            var actualBest = AllOffers
                .Where(offer => offer.IsSuccessful)
                .OrderByDescending(offer => offer.ConvertedAmount)
                .First();

            if (BestOffer.ConvertedAmount != actualBest.ConvertedAmount)
            {
                throw new InvalidOperationException("Best offer is not actually the best among successful offers");
            }
        }
    }

    /// <summary>
    /// Returns a summary string of the comparison result
    /// </summary>
    public override string ToString()
    {
        return Status switch
        {
            ProcessStatus.Completed when BestOffer != null => 
                $"Best: {BestOffer.ProviderName} ({BestOffer.ConvertedAmount:F2}) from {AllOffers.Count} providers",
            ProcessStatus.Completed => 
                $"No successful offers from {AllOffers.Count} providers",
            ProcessStatus.Failed => 
                $"Failed after {ProcessingDuration.TotalMilliseconds:F0}ms",
            _ => 
                $"Processing {Input}"
        };
    }
}