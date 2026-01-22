using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Sites.Commands.UpdateSite;

public record UpdateSiteCommand : IRequest
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Country { get; init; }
    public string? City { get; init; }
    public string? District { get; init; }
    public string? AddressLine { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public string Timezone { get; init; } = "Europe/Istanbul";
}

public class UpdateSiteCommandHandler : IRequestHandler<UpdateSiteCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateSiteCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(UpdateSiteCommand request, CancellationToken cancellationToken)
    {
        var userId = _user.Id ?? throw new UnauthorizedAccessException("User not authenticated");

        // Only the site owner can update the site (not guests)
        var entity = await _context.Sites
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.UserId == userId && !x.IsDeleted, cancellationToken);

        if (entity == null)
        {
            throw new Hems_Backend.Application.Common.Exceptions.NotFoundException(nameof(Site), request.Id);
        }

        entity.Name = request.Name;
        entity.Country = request.Country;
        entity.City = request.City;
        entity.District = request.District;
        entity.AddressLine = request.AddressLine;
        entity.Latitude = request.Latitude;
        entity.Longitude = request.Longitude;
        entity.Timezone = request.Timezone;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
