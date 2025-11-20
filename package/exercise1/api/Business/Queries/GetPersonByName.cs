using Dapper;
using MediatR;
using StargateAPI.Business.Data;
using StargateAPI.Business.Dtos;
using StargateAPI.Controllers;

namespace StargateAPI.Business.Queries
{
    public class GetPersonByName : IRequest<GetPersonByNameResult>
    {
        public required string Name { get; set; } = string.Empty;
    }

    public class GetPersonByNameHandler : IRequestHandler<GetPersonByName, GetPersonByNameResult>
    {
        private readonly StargateContext _context;

        public GetPersonByNameHandler(StargateContext context)
        {
            _context = context;
        }

        public async Task<GetPersonByNameResult> Handle(GetPersonByName request, CancellationToken cancellationToken)
        {
            var result = new GetPersonByNameResult();

            // Parameterized query — safe, case-insensitive
            var query =
                "SELECT a.Id as PersonId, a.Name, b.CurrentRank, b.CurrentDutyTitle, " +
                "b.CareerStartDate, b.CareerEndDate " +
                "FROM [Person] a " +
                "LEFT JOIN [AstronautDetail] b ON b.PersonId = a.Id " +
                "WHERE a.Name = @Name COLLATE NOCASE";

            // Normalize input
            var normalizedName = request.Name.Trim();

            // Query for a single match
            var person = await _context.Connection
                .QueryFirstOrDefaultAsync<PersonAstronaut>(query, new { Name = normalizedName });

            result.Person = person; // Person may be null and that is OK

            return result;
        }
    }

    public class GetPersonByNameResult : BaseResponse
    {
        public PersonAstronaut? Person { get; set; }
    }
}