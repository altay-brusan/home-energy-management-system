using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Mappings;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Domain.Entities;
using MediatR;

namespace Hems_Backend.Application.Setups.Queries.GetSetupById;

public record GetSetupByIdQuery(Guid Id) : IRequest<SetupDto?>;

public class GetSetupByIdQueryHandler : IRequestHandler<GetSetupByIdQuery, SetupDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetSetupByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    /// <summary>
    /// Handles the GetSetupByIdQuery.
    /// Note: this handler uses <c>FindAsync</c> and therefore does NOT load the related <c>SetupDevice</c> navigation property.
    /// The returned <see cref="SetupDto"/> will have <c>SetupDevice == null</c> unless the query explicitly loads that navigation (e.g. using Include/ProjectTo).
    /// </summary>
    public async Task<SetupDto?> Handle(GetSetupByIdQuery request, CancellationToken cancellationToken)
    {
        var setup = await _context.Setups
            .FindAsync([request.Id], cancellationToken);

        return setup == null ? null : _mapper.Map<SetupDto>(setup);
    }
}




















