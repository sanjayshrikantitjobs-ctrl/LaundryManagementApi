using FluentValidation;
using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Entities;
using LaundryMgmt.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.ContactMessages.Commands.CreateContactMessage;

public record CreateContactMessageCommand(ContactMessageType Type, string Message, string? ImageUrl) : IRequest<Guid>;

public class CreateContactMessageCommandValidator : AbstractValidator<CreateContactMessageCommand>
{
    public CreateContactMessageCommandValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.ImageUrl).MaximumLength(500);
    }
}

public class CreateContactMessageCommandHandler : IRequestHandler<CreateContactMessageCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateContactMessageCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateContactMessageCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Not signed in.");

        var customerId = await _db.Customers
            .Where(c => c.IdentityUserId == userId)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No customer profile is linked to this login.");

        var message = new ContactMessage
        {
            CustomerId = customerId,
            Type = request.Type,
            Message = request.Message.Trim(),
            ImageUrl = request.ImageUrl
        };

        _db.ContactMessages.Add(message);
        await _db.SaveChangesAsync(cancellationToken);

        return message.Id;
    }
}
