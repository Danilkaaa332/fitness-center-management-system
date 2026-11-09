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
        ArgumentNullException.ThrowIfNull(membershipValidator);
        ArgumentNullException.ThrowIfNull(logger);
        _membershipValidator = membershipValidator;
        _logger = logger;
    }

    public async Task<RegisterVisitResult> RegisterVisitAsync(
        RegisterVisitRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (request.CustomerId == Guid.Empty || request.MembershipId == Guid.Empty)
            return new RegisterVisitResult(false, FitnessErrorCodes.InvalidRequest, null);

        var validation = await _membershipValidator.CheckValidityAsync(
            new MembershipCheckRequest(request.MembershipId, request.VisitDate),
            cancellationToken);

        if (!validation.IsValid)
            return new RegisterVisitResult(false, validation.ErrorCode, null);

        if (_visits.Any(v => v.CustomerId == request.CustomerId && v.VisitDate.Date == request.VisitDate.Date))
            return new RegisterVisitResult(false, FitnessErrorCodes.DuplicateVisit, null);

        var visit = new VisitDto(Guid.NewGuid(), request.CustomerId, request.MembershipId, request.VisitDate, VisitStatuses.Registered);
        _visits.Add(visit);
        return new RegisterVisitResult(true, null, visit);
    }


    public IReadOnlyList<VisitDto> GetVisits() => _visits.AsReadOnly();
}
