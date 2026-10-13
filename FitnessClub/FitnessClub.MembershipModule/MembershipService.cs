using FitnessClub.Contracts;
using Microsoft.Extensions.Logging;

namespace FitnessClub.MembershipModule;

public class MembershipService : IMembershipValidationService
{
    private readonly ILogger<MembershipService> _logger;
    private readonly Dictionary<Guid, MembershipDto> _memberships = new();

    public MembershipService(ILogger<MembershipService> logger)
    {
        _logger = logger;
    }

    public MembershipDto CreateMembership(Guid customerId, string type, DateTime startDate, int durationDays, decimal price, string status = "Active")
    {
        var membership = new MembershipDto(
            Guid.NewGuid(),
            customerId,
            type,
            startDate,
            startDate.AddDays(durationDays),
            price,
            status
        );

        _memberships[membership.Id] = membership;

        _logger.LogInformation(
            "Создан абонемент {MembershipId} для клиента {CustomerId}. Срок: {StartDate:yyyy-MM-dd} - {EndDate:yyyy-MM-dd}, Статус: {Status}",
            membership.Id, customerId, membership.StartDate, membership.EndDate, membership.Status);

        return membership;
    }

    public Task<ValidationResult> CheckValidityAsync(MembershipCheckRequest request)
    {
        _logger.LogInformation(
            "Проверка абонемента {MembershipId} на дату визита {VisitDate:O}",
            request.MembershipId, request.VisitDate);

        if (!_memberships.TryGetValue(request.MembershipId, out var membership))
        {
            _logger.LogWarning("Абонемент {MembershipId} не найден", request.MembershipId);
            return Task.FromResult(new ValidationResult(false, "MEMBERSHIP_NOT_FOUND"));
        }

        if (string.Equals(membership.Status, "Blocked", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Абонемент {MembershipId} заблокирован", request.MembershipId);
            return Task.FromResult(new ValidationResult(false, "MEMBERSHIP_BLOCKED"));
        }

        if (request.VisitDate.Date < membership.StartDate.Date || request.VisitDate.Date > membership.EndDate.Date)
        {
            _logger.LogWarning(
                "Срок действия абонемента {MembershipId} истек (EndDate: {EndDate:yyyy-MM-dd}, VisitDate: {VisitDate:yyyy-MM-dd})",
                request.MembershipId, membership.EndDate, request.VisitDate);
            return Task.FromResult(new ValidationResult(false, "MEMBERSHIP_EXPIRED"));
        }

        _logger.LogInformation("Абонемент {MembershipId} успешно прошел проверку", request.MembershipId);
        return Task.FromResult(new ValidationResult(true, null));
    }
}
