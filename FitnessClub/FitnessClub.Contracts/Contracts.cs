namespace FitnessClub.Contracts;

public static class MembershipStatuses
{
    public const string Active = "Active";
    public const string Blocked = "Blocked";
    public const string Frozen = "Frozen";
}

public static class VisitStatuses
{
    public const string Registered = "Registered";
    public const string Cancelled = "Cancelled";
}

public static class FitnessErrorCodes
{
    public const string InvalidRequest = "INVALID_REQUEST";
    public const string MembershipNotFound = "MEMBERSHIP_NOT_FOUND";
    public const string MembershipBlocked = "MEMBERSHIP_BLOCKED";
    public const string MembershipExpired = "MEMBERSHIP_EXPIRED";
    public const string DuplicateVisit = "DUPLICATE_VISIT";
}

public record RegisterVisitResult(bool Success, string? ErrorCode = null, VisitDto? Visit = null);

public interface IMembershipValidationService
{
    Task<ValidationResult> CheckValidityAsync(
        MembershipCheckRequest request,
        CancellationToken cancellationToken = default);
}


public record CustomerDto(
    Guid Id,
    string FullName,
    string Phone,
    DateTime BirthDate
);

public record MembershipDto(
    Guid Id,
    Guid CustomerId,
    string Type,
    DateTime StartDate,
    DateTime EndDate,
    decimal Price,
    string Status
);

public record RegisterVisitRequest(
    Guid CustomerId,
    Guid MembershipId,
    DateTime VisitDate
);

public record MembershipCheckRequest(
    Guid MembershipId,
    DateTime VisitDate
);

public record ValidationResult(
    bool IsValid,
    string? ErrorCode = null
);

public record VisitDto(
    Guid Id,
    Guid CustomerId,
    Guid MembershipId,
    DateTime VisitDate,
    string Status
);

