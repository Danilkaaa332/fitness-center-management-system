using System.Diagnostics;
using FitnessClub.Contracts;
using Microsoft.Extensions.Logging;

namespace FitnessClub.VisitModule;

public class VisitService
{
    private readonly IMembershipValidationService _membershipValidator;
    private readonly ILogger<VisitService> _logger;
    private readonly List<VisitDto> _visits = new();

    public VisitService(IMembershipValidationService membershipValidator, ILogger<VisitService> logger)
    {
        _membershipValidator = membershipValidator;
        _logger = logger;
    }

    public async Task<(bool Success, string? ErrorCode, VisitDto? Visit)> RegisterVisitAsync(RegisterVisitRequest request)
    {
        _logger.LogInformation(
            "Запрос на регистрацию визита: CustomerId={CustomerId}, MembershipId={MembershipId}, VisitDate={VisitDate:O}",
            request.CustomerId, request.MembershipId, request.VisitDate);

        bool alreadyVisitedToday = _visits.Any(v =>
            v.CustomerId == request.CustomerId &&
            v.MembershipId == request.MembershipId &&
            v.VisitDate.Date == request.VisitDate.Date &&
            v.Status == "Registered");

        if (alreadyVisitedToday)
        {
            Debug.WriteLine($"[Debug VisitService] Duplicate visit detected for CustomerId={request.CustomerId} on {request.VisitDate:yyyy-MM-dd}");
            _logger.LogWarning(
                "Повторная регистрация визита отклонена: клиент {CustomerId} уже зарегистрирован {VisitDate:yyyy-MM-dd}",
                request.CustomerId, request.VisitDate);
            return (false, "DUPLICATE_VISIT_SAME_DAY", null);
        }

        var validation = await _membershipValidator.CheckValidityAsync(
            new MembershipCheckRequest(request.MembershipId, request.VisitDate));

        if (!validation.IsValid)
        {
            _logger.LogWarning(
                "Отказ в регистрации визита для клиента {CustomerId} по абонементу {MembershipId}. Код ошибки: {ErrorCode}",
                request.CustomerId, request.MembershipId, validation.ErrorCode);

            return (false, validation.ErrorCode, null);
        }

        var visit = new VisitDto(
            Guid.NewGuid(),
            request.CustomerId,
            request.MembershipId,
            request.VisitDate,
            "Registered"
        );

        _visits.Add(visit);

        _logger.LogInformation(
            "Визит {VisitId} успешно зарегистрирован для клиента {CustomerId}",
            visit.Id, request.CustomerId);

        return (true, null, visit);
    }

    public IReadOnlyList<VisitDto> GetVisits() => _visits.AsReadOnly();
}
