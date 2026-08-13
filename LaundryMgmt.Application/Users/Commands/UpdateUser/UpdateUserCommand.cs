using FluentValidation;
using LaundryMgmt.Application.Common.Interfaces;
using MediatR;

namespace LaundryMgmt.Application.Users.Commands.UpdateUser;

public record UpdateUserCommand(Guid UserId, string FullName, string? Email, string? PhoneNumber) : IRequest;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand>
{
    private readonly IUserManagementService _userManagement;

    public UpdateUserCommandHandler(IUserManagementService userManagement) => _userManagement = userManagement;

    public async Task Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        await _userManagement.UpdateUserAsync(
            request.UserId, request.FullName.Trim(), request.Email?.Trim(), request.PhoneNumber?.Trim(), cancellationToken);
    }
}
