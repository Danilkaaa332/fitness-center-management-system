namespace FitnessClub.Contracts;

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

public interface IMembershipValidationService
{
    Task<ValidationResult> CheckValidityAsync(MembershipCheckRequest request);
}
