using FluentValidation;
using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.ContactMessages.Commands.RespondToContactMessage;

public record RespondToContactMessageCommand(Guid ContactMessageId, string Response) : IRequest;

public class RespondToContactMessageCommandValidator : AbstractValidator<RespondToContactMessageCommand>
{
    public RespondToContactMessageCommandValidator()
    {
        RuleFor(x => x.Response).NotEmpty().MaximumLength(2000);
    }
}

public class RespondToContactMessageCommandHandler : IRequestHandler<RespondToContactMessageCommand>
{
    private readonly IApplicationDbContext _db;

    public RespondToContactMessageCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(RespondToContactMessageCommand request, CancellationToken cancellationToken)
    {
        var message = await _db.ContactMessages
            .FirstOrDefaultAsync(m => m.Id == request.ContactMessageId, cancellationToken)
            ?? throw new KeyNotFoundException($"Contact message {request.ContactMessageId} not found.");

        message.Response = request.Response.Trim();
        message.Status = ContactMessageStatus.Resolved;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
