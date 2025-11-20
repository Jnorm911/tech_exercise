using MediatR;
using MediatR.Pipeline;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Controllers;

namespace StargateAPI.Business.Commands
{
    public class CreateAstronautDuty : IRequest<CreateAstronautDutyResult>
    {
        public required string Name { get; set; }

        public required string Rank { get; set; }

        public required string DutyTitle { get; set; }

        public DateTime DutyStartDate { get; set; }
    }

    public class CreateAstronautDutyPreProcessor : IRequestPreProcessor<CreateAstronautDuty>
    {
        private readonly StargateContext _context;
        private readonly ILogger<CreateAstronautDutyPreProcessor> _logger;

        public CreateAstronautDutyPreProcessor(StargateContext context, ILogger<CreateAstronautDutyPreProcessor> logger)
        {
            _context = context;
            _logger = logger;
        }

        public Task Process(CreateAstronautDuty request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Validating duty for {Name}", request.Name);

            var person = _context.People.AsNoTracking().FirstOrDefault(z => z.Name == request.Name);

            if (person is null)
            {
                _logger.LogWarning("Validation failed: person not found {Name}", request.Name);
                throw new BadHttpRequestException("Bad Request");
            }

            var verifyNoPreviousDuty = _context.AstronautDuties.FirstOrDefault(z =>
                z.PersonId == person.Id &&
                z.DutyTitle == request.DutyTitle &&
                z.DutyStartDate == request.DutyStartDate);

            if (verifyNoPreviousDuty is not null)
            {
                _logger.LogWarning("Validation failed: duplicate duty for {Name}", request.Name);
                throw new BadHttpRequestException("Bad Request");
            }

            return Task.CompletedTask;
        }
    }

    public class CreateAstronautDutyHandler : IRequestHandler<CreateAstronautDuty, CreateAstronautDutyResult>
    {
        private readonly StargateContext _context;
        private readonly ILogger<CreateAstronautDutyHandler> _logger;

        public CreateAstronautDutyHandler(StargateContext context, ILogger<CreateAstronautDutyHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CreateAstronautDutyResult> Handle(CreateAstronautDuty request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling CreateAstronautDuty for {Name}", request.Name);

            var person = await _context.People.FirstOrDefaultAsync(p => p.Name == request.Name, cancellationToken);

            if (person is null)
            {
                _logger.LogWarning("Handler failed: person not found {Name}", request.Name);
                throw new BadHttpRequestException("Bad Request");
            }

            var astronautDetail = await _context.AstronautDetails.FirstOrDefaultAsync(a => a.PersonId == person.Id, cancellationToken);

            if (astronautDetail == null)
            {
                astronautDetail = new AstronautDetail
                {
                    PersonId = person.Id,
                    CurrentDutyTitle = request.DutyTitle,
                    CurrentRank = request.Rank,
                    CareerStartDate = request.DutyStartDate.Date
                };

                if (request.DutyTitle == "RETIRED")
                {
                    astronautDetail.CareerEndDate = request.DutyStartDate.AddDays(-1).Date;
                }

                await _context.AstronautDetails.AddAsync(astronautDetail);
            }
            else
            {
                astronautDetail.CurrentDutyTitle = request.DutyTitle;
                astronautDetail.CurrentRank = request.Rank;

                if (request.DutyTitle == "RETIRED")
                {
                    astronautDetail.CareerEndDate = request.DutyStartDate.AddDays(-1).Date;
                }

                _context.AstronautDetails.Update(astronautDetail);
            }

            var astronautDuty = await _context.AstronautDuties
                .Where(d => d.PersonId == person.Id)
                .OrderByDescending(d => d.DutyStartDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (astronautDuty != null)
            {
                astronautDuty.DutyEndDate = request.DutyStartDate.AddDays(-1).Date;
                _context.AstronautDuties.Update(astronautDuty);
            }

            var newAstronautDuty = new AstronautDuty
            {
                PersonId = person.Id,
                Rank = request.Rank,
                DutyTitle = request.DutyTitle,
                DutyStartDate = request.DutyStartDate.Date,
                DutyEndDate = null
            };

            await _context.AstronautDuties.AddAsync(newAstronautDuty);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed saving astronaut duty for {Name}", request.Name);
                throw;
            }

            _logger.LogInformation("CreateAstronautDuty succeeded for {Name}", request.Name);

            return new CreateAstronautDutyResult
            {
                Id = newAstronautDuty.Id
            };
        }
    }

    public class CreateAstronautDutyResult : BaseResponse
    {
        public int? Id { get; set; }
    }
}