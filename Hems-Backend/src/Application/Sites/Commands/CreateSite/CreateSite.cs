using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using MediatR;

namespace Hems_Backend.Application.Sites.Commands.CreateSite;

public record CreateSiteCommand : IRequest<Guid>
{
    public string Name { get; init; } = string.Empty;
    public string? Country { get; init; }
    public string? City { get; init; }
    public string? District { get; init; }
    public string? AddressLine { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public string Timezone { get; init; } = "Europe/Istanbul";
}

public class CreateSiteCommandHandler : IRequestHandler<CreateSiteCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly TimeProvider _timeProvider;

    public CreateSiteCommandHandler(IApplicationDbContext context, IUser user, TimeProvider timeProvider)
    {
        _context = context;
        _user = user;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> Handle(CreateSiteCommand request, CancellationToken cancellationToken)
    {
        var userId = _user.Id ?? throw new UnauthorizedAccessException("User not authenticated");
        var utcNow = _timeProvider.GetUtcNow();
        
        var entity = new Site
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            Country = request.Country,
            City = request.City,
            District = request.District,
            AddressLine = request.AddressLine,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Timezone = request.Timezone,
            IsDeleted = false,
        };

        _context.Sites.Add(entity);

        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}

