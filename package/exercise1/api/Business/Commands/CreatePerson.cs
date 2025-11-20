using MediatR;
using MediatR.Pipeline;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Controllers;

namespace StargateAPI.Business.Commands
{
    public class CreatePerson : IRequest<CreatePersonResult>
    {
        public required string Name { get; set; } = string.Empty;
    }

    public class CreatePersonPreProcessor : IRequestPreProcessor<CreatePerson>
    {
        private readonly StargateContext _context;

        public CreatePersonPreProcessor(StargateContext context)
        {
            _context = context;
        }

        public Task Process(CreatePerson request, CancellationToken cancellationToken)
        {
            var trimmedName = request.Name.Trim();
            request.Name = trimmedName;

            var person = _context.People
                .AsNoTracking()
                .FirstOrDefault(z => z.Name == trimmedName);

            if (person is not null)
            {
                throw new BadHttpRequestException("Bad Request");
            }

            return Task.CompletedTask;
        }
    }

    public class CreatePersonHandler : IRequestHandler<CreatePerson, CreatePersonResult>
    {
        private readonly StargateContext _context;
        private readonly ILogger<CreatePersonHandler> _logger;

        public CreatePersonHandler(StargateContext context, ILogger<CreatePersonHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CreatePersonResult> Handle(CreatePerson request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting CreatePerson handler for Name: {Name}", request.Name);

            var newPerson = new Person
            {
                Name = request.Name.Trim()
            };

            await _context.People.AddAsync(newPerson, cancellationToken);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Successfully created person with Id {Id}", newPerson.Id);
            }
            catch ( Exception ex )
            {
                _logger.LogError(ex, "Error occurred while saving new person with Name {Name}", request.Name);
                throw;
            }

            return new CreatePersonResult
            {
                Id = newPerson.Id
            };
        }
    }

    public class CreatePersonResult : BaseResponse
    {
        public int Id { get; set; }
    }
}