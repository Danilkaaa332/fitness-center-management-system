using System.Diagnostics;
using FitnessClub.Contracts;
using Microsoft.Extensions.Logging;

namespace FitnessClub.MembershipModule;

public class MembershipService : IMembershipValidationService
{
    private readonly ILogger<MembershipService> _logger;
    private readonly Dictionary<Guid, MembershipDto> _memberships = new();

    public MembershipService(ILogger<MembershipService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public MembershipDto CreateMembership(
        Guid customerId,
        string type,
        DateTime startDate,
        int durationDays,
        decimal price,
        string status = MembershipStatuses.Active)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("Идентификатор клиента не может быть пустым.", nameof(customerId));
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Тип абонемента не может быть пустым.", nameof(type));
        if (durationDays <= 0)
            throw new ArgumentOutOfRangeException(nameof(durationDays), "Длительность должна быть больше 0.");
        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Стоимость не может быть отрицательной.");
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Статус абонемента не может быть пустым.", nameof(status));

        var membership = new MembershipDto(
            Guid.NewGuid(), customerId, type.Trim(), startDate, startDate.AddDays(durationDays), price, status.Trim());

        _memberships[membership.Id] = membership;
        return membership;
    }

    public Task<ValidationResult> CheckValidityAsync(
        MembershipCheckRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (!_memberships.TryGetValue(request.MembershipId, out var membership))
            return Task.FromResult(new ValidationResult(false, FitnessErrorCodes.MembershipNotFound));

        var normalizedStatus = membership.Status?.Trim() ?? string.Empty;
        if (!string.Equals(normalizedStatus, MembershipStatuses.Active, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(new ValidationResult(false, FitnessErrorCodes.MembershipBlocked));

        if (request.VisitDate.Date < membership.StartDate.Date || request.VisitDate.Date > membership.EndDate.Date)
            return Task.FromResult(new ValidationResult(false, FitnessErrorCodes.MembershipExpired));

        return Task.FromResult(new ValidationResult(true, null));
    }

}
