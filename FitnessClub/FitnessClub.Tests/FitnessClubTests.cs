using FitnessClub.Contracts;
using FitnessClub.MembershipModule;
using FitnessClub.VisitModule;
using Microsoft.Extensions.Logging.Abstractions;

namespace FitnessClub.Tests;

public class FitnessClubTests
{
    [Fact]
    public async Task MembershipService_WhenMembershipExpired_ReturnsMembershipExpiredError()
    {
        var service = new MembershipService(NullLogger<MembershipService>.Instance);
        var startDate = new DateTime(2026, 9, 1);
        var membership = service.CreateMembership(Guid.NewGuid(), "Месячный безлимит", startDate, 30, 3500m);

        var visitDate = new DateTime(2026, 10, 15);
        var result = await service.CheckValidityAsync(new MembershipCheckRequest(membership.Id, visitDate));

        Assert.False(result.IsValid);
        Assert.Equal("MEMBERSHIP_EXPIRED", result.ErrorCode);
    }

    [Fact]
    public async Task VisitIntegration_WhenMembershipIsActive_RegistersVisitSuccessfully()
    {
        var membershipService = new MembershipService(NullLogger<MembershipService>.Instance);
        var visitService = new VisitService(membershipService, NullLogger<VisitService>.Instance);

        var customerId = Guid.NewGuid();
        var startDate = new DateTime(2026, 10, 1);
        var membership = membershipService.CreateMembership(customerId, "Месячный безлимит", startDate, 30, 3500m);

        var request = new RegisterVisitRequest(customerId, membership.Id, new DateTime(2026, 10, 15));
        var (success, errorCode, visit) = await visitService.RegisterVisitAsync(request);

        Assert.True(success);
        Assert.Null(errorCode);
        Assert.NotNull(visit);
        Assert.Single(visitService.GetVisits());
    }

    [Fact]
    public async Task VisitIntegration_WhenMembershipIsBlocked_RejectsVisit()
    {
        var membershipService = new MembershipService(NullLogger<MembershipService>.Instance);
        var visitService = new VisitService(membershipService, NullLogger<VisitService>.Instance);

        var customerId = Guid.NewGuid();
        var startDate = new DateTime(2026, 10, 1);
        var membership = membershipService.CreateMembership(customerId, "Годовой с бассейном", startDate, 365, 25000m, status: "Blocked");

        var request = new RegisterVisitRequest(customerId, membership.Id, new DateTime(2026, 10, 15));
        var (success, errorCode, visit) = await visitService.RegisterVisitAsync(request);

        Assert.False(success);
        Assert.Equal("MEMBERSHIP_BLOCKED", errorCode);
        Assert.Null(visit);
        Assert.Empty(visitService.GetVisits());
    }

    [Fact]
    public async Task Regression_CheckValidity_OnLastDayEvening_ShouldBeValid()
    {
        var service = new MembershipService(NullLogger<MembershipService>.Instance);
        var startDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var membership = service.CreateMembership(Guid.NewGuid(), "Месячный безлимит", startDate, 30, 3500m);

        var eveningVisitOnLastDay = new DateTime(2026, 10, 31, 18, 30, 0, DateTimeKind.Utc);
        var result = await service.CheckValidityAsync(new MembershipCheckRequest(membership.Id, eveningVisitOnLastDay));

        Assert.True(result.IsValid);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    public void CreateMembership_WhenDurationOrPriceIsInvalid_ThrowsArgumentOutOfRangeException()
    {
        var service = new MembershipService(NullLogger<MembershipService>.Instance);
        var startDate = new DateTime(2026, 10, 1);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            service.CreateMembership(Guid.NewGuid(), "Месячный", startDate, durationDays: 0, price: 3500m));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            service.CreateMembership(Guid.NewGuid(), "Месячный", startDate, durationDays: 30, price: -100m));
    }

    [Fact]
    public async Task RegisterVisitAsync_WhenCancellationRequested_ThrowsOperationCanceledException()
    {
        var membershipService = new MembershipService(NullLogger<MembershipService>.Instance);
        var visitService = new VisitService(membershipService, NullLogger<VisitService>.Instance);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var request = new RegisterVisitRequest(Guid.NewGuid(), Guid.NewGuid(), new DateTime(2026, 10, 15));

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            visitService.RegisterVisitAsync(request, cts.Token));
    }
}
