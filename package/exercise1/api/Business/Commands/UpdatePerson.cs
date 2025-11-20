using MediatR;
using MediatR.Pipeline;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Controllers;

namespace StargateAPI.Business.Commands
{
    public class UpdatePerson : IRequest<UpdatePersonResult>
    {
        public required string CurrentName { get; set; } = string.Empty;
        public required string NewName { get; set; } = string.Empty;
    }

    public class UpdatePersonPreProcessor : IRequestPreProcessor<UpdatePerson>
    {
        private readonly StargateContext _context;
        private readonly ILogger<UpdatePersonPreProcessor> _logger;

        public UpdatePersonPreProcessor(StargateContext context, ILogger<UpdatePersonPreProcessor> logger)
        {
            _context = context;
            _logger = logger;
        }

        public Task Process(UpdatePerson request, CancellationToken cancellationToken)
        {
            var currentNameTrimmed = request.CurrentName.Trim();
            var newNameTrimmed = request.NewName.Trim();

            request.CurrentName = currentNameTrimmed;
            request.NewName = newNameTrimmed;

            if (string.IsNullOrWhiteSpace(currentNameTrimmed) || string.IsNullOrWhiteSpace(newNameTrimmed))
            {
                _logger.LogWarning("Validation failed for UpdatePerson: empty name");
                throw new BadHttpRequestException("Bad Request");
            }

            // Ensure the source person exists
            var person = _context.People
                .AsNoTracking()
                .FirstOrDefault(p => p.Name == currentNameTrimmed);

            if (person is null)
            {
                _logger.LogWarning("Validation failed for UpdatePerson: person not found {Name}", currentNameTrimmed);
                throw new BadHttpRequestException("Bad Request");
            }

            // Ensure the target name is not already in use by someone else
            if (!string.Equals(currentNameTrimmed, newNameTrimmed, StringComparison.Ordinal))
            {
                var existingWithNewName = _context.People
                    .AsNoTracking()
                    .FirstOrDefault(p => p.Name == newNameTrimmed);

                if (existingWithNewName is not null)
                {
                    _logger.LogWarning("Validation failed for UpdatePerson: duplicate target name {Name}", newNameTrimmed);
                    throw new BadHttpRequestException("Bad Request");
                }
            }

            return Task.CompletedTask;
        }
    }

    public class UpdatePersonHandler : IRequestHandler<UpdatePerson, UpdatePersonResult>
    {
        private readonly StargateContext _context;
        private readonly ILogger<UpdatePersonHandler> _logger;

        public UpdatePersonHandler(StargateContext context, ILogger<UpdatePersonHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<UpdatePersonResult> Handle(UpdatePerson request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting UpdatePerson handler for Name: {CurrentName}", request.CurrentName);

            var person = await _context.People
                .FirstOrDefaultAsync(p => p.Name == request.CurrentName, cancellationToken);

            if (person is null)
            {
                _logger.LogWarning("UpdatePerson handler could not find person {Name}", request.CurrentName);
                throw new BadHttpRequestException("Bad Request");
            }

            person.Name = request.NewName;

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Successfully updated person {Id} to new name {NewName}", person.Id, person.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating person with Name {Name}", request.CurrentName);
                throw;
            }

            return new UpdatePersonResult
            {
                Id = person.Id
            };
        }
    }

    public class UpdatePersonResult : BaseResponse
    {
        public int Id { get; set; }
    }
}