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

    public async Task<SetupDto?> Handle(GetSetupByIdQuery request, CancellationToken cancellationToken)
    {
        var setup = await _context.Setups
            .FindAsync([request.Id], cancellationToken);

        return setup == null ? null : _mapper.Map<SetupDto>(setup);
    }
}








