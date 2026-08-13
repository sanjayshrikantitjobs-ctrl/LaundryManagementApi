using FluentValidation;
using LaundryMgmt.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.Customers.Commands.UpdateMyProfile;

/// <summary>Lets the logged-in customer update their own profile (Settings &gt; General
/// tab) — email, WhatsApp number, and full name. Resolves the target Customer from the
/// caller's identity, so a customer can never edit anyone else's record.</summary>
public record UpdateMyProfileCommand(string FullName, string? Email, string? WhatsAppNumber) : IRequest;

public class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
{
    public UpdateMyProfileCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.WhatsAppNumber).MaximumLength(20);
    }
}

public class UpdateMyProfileCommandHandler : IRequestHandler<UpdateMyProfileCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateMyProfileCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Not signed in.");

        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.IdentityUserId == userId, cancellationToken)
            ?? throw new InvalidOperationException("No customer profile is linked to this login.");

        customer.FullName = request.FullName.Trim();
        customer.Email = request.Email?.Trim();
        customer.WhatsAppNumber = request.WhatsAppNumber?.Trim();

        await _db.SaveChangesAsync(cancellationToken);
    }
}
