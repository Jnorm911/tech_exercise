using Dapper;
using MediatR;
using Microsoft.Extensions.Logging; // Logging.
using StargateAPI.Business.Data;
using StargateAPI.Business.Dtos;
using StargateAPI.Controllers;

namespace StargateAPI.Business.Queries
{
    public class GetAstronautDutiesByName : IRequest<GetAstronautDutiesByNameResult>
    {
        public string Name { get; set; } = string.Empty;
    }

    public class GetAstronautDutiesByNameHandler : IRequestHandler<GetAstronautDutiesByName, GetAstronautDutiesByNameResult>
    {
        private readonly StargateContext _context;
        private readonly ILogger<GetAstronautDutiesByNameHandler> _logger; // Logger.

        public GetAstronautDutiesByNameHandler(StargateContext context, ILogger<GetAstronautDutiesByNameHandler> logger)
        {
            _context = context; // Context.
            _logger = logger; // Logger.
        }

        public async Task<GetAstronautDutiesByNameResult> Handle(GetAstronautDutiesByName request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Querying duties for {Name}", request.Name); // Begin.

            var result = new GetAstronautDutiesByNameResult();

            // Parameterized SQL to remove injection risk.
            var query = "SELECT a.Id as PersonId, a.Name, b.CurrentRank, b.CurrentDutyTitle, b.CareerStartDate, b.CareerEndDate FROM [Person] a LEFT JOIN [AstronautDetail] b on b.PersonId = a.Id WHERE a.Name = @Name";

            var person = await _context.Connection.QueryFirstOrDefaultAsync<PersonAstronaut>(query, new { Name = request.Name });

            // Queries can come back empty, so guard against a null result.
            if (person == null)
            {
                _logger.LogWarning("No astronaut found for {Name}", request.Name); // No result.
                return result;
            }

            result.Person = person;

            // Uses parameters to avoid raw ID injection.
            query = "SELECT * FROM [AstronautDuty] WHERE PersonId = @PersonId ORDER BY DutyStartDate DESC";

            var duties = await _context.Connection.QueryAsync<AstronautDuty>(query, new { PersonId = person.PersonId });

            result.AstronautDuties = duties.ToList();

            _logger.LogInformation("Duties retrieved for {Name}", request.Name); // Retrieved.

            return result;
        }
    }
    
    public class GetAstronautDutiesByNameResult : BaseResponse
    {
        // Marked nullable because this property isn't set in the constructor.
        // It's populated by the query, so the compiler can't guarantee non-null.
        public PersonAstronaut? Person { get; set; }
        public List<AstronautDuty> AstronautDuties { get; set; } = new List<AstronautDuty>();    }
}