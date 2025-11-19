using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Data;
using Xunit;

namespace StrategyAPI.Tests.Astronaut
{
    public class CreateAstronautDutyPreProcessorTests
    {
        private static StargateContext CreateContext(SqliteConnection connection)
        {
            var options = new DbContextOptionsBuilder<StargateContext>()
                .UseSqlite(connection)
                .Options;

            var context = new StargateContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public async Task Process_ThrowsBadRequest_WhenPersonDoesNotExist()
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            using var context = CreateContext(connection);

            var preProcessor = new CreateAstronautDutyPreProcessor(context);

            var request = new CreateAstronautDuty
            {
                Name = "Nonexistent Person",
                Rank = "Captain",
                DutyTitle = "Commander",
                DutyStartDate = new DateTime(2024, 1, 1)
            };

            await Assert.ThrowsAsync<BadHttpRequestException>(
                () => preProcessor.Process(request, CancellationToken.None));
        }

        [Fact]
        public async Task Process_ThrowsBadRequest_WhenDuplicateDutyExists()
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            using var context = CreateContext(connection);

            var person = new Person
            {
                Name = "Samantha Carter"
            };

            context.People.Add(person);
            await context.SaveChangesAsync();

            var existingDuty = new AstronautDuty
            {
                PersonId = person.Id,
                Rank = "Colonel",
                DutyTitle = "Commander",
                DutyStartDate = new DateTime(2024, 1, 1)
            };

            context.AstronautDuties.Add(existingDuty);
            await context.SaveChangesAsync();

            var preProcessor = new CreateAstronautDutyPreProcessor(context);

            var request = new CreateAstronautDuty
            {
                Name = person.Name,
                Rank = "Colonel",
                DutyTitle = "Commander",
                DutyStartDate = existingDuty.DutyStartDate
            };

            await Assert.ThrowsAsync<BadHttpRequestException>(
                () => preProcessor.Process(request, CancellationToken.None));
        }

        [Fact]
        public async Task Process_Completes_WhenPersonExistsAndNoDuplicateDuty()
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            using var context = CreateContext(connection);

            var person = new Person
            {
                Name = "Daniel Jackson"
            };

            context.People.Add(person);
            await context.SaveChangesAsync();

            var preProcessor = new CreateAstronautDutyPreProcessor(context);

            var request = new CreateAstronautDuty
            {
                Name = person.Name,
                Rank = "Doctor",
                DutyTitle = "Linguist",
                DutyStartDate = new DateTime(2024, 2, 1)
            };

            await preProcessor.Process(request, CancellationToken.None);
        }
    }

    public class CreateAstronautDutyHandlerTests
    {
        private static StargateContext CreateContext(SqliteConnection connection)
        {
            var options = new DbContextOptionsBuilder<StargateContext>()
                .UseSqlite(connection)
                .Options;

            var context = new StargateContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        private static async Task<(StargateContext context, CreateAstronautDutyHandler handler, Person person)> BuildHandlerWithPersonAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var context = CreateContext(connection);

            var person = new Person
            {
                Name = "Jack O'Neill"
            };

            context.People.Add(person);
            await context.SaveChangesAsync();

            var handler = new CreateAstronautDutyHandler(context);
            return (context, handler, person);
        }

        [Fact]
        public async Task Handle_ThrowsBadRequest_WhenPersonCannotBeFoundByName()
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            using var context = CreateContext(connection);

            var handler = new CreateAstronautDutyHandler(context);

            var request = new CreateAstronautDuty
            {
                Name = "Unknown Person",
                Rank = "Major",
                DutyTitle = "Pilot",
                DutyStartDate = new DateTime(2024, 1, 1)
            };

            await Assert.ThrowsAsync<BadHttpRequestException>(
                () => handler.Handle(request, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_CreatesAstronautDetailAndDuty_WhenNoExistingDetailOrDuty()
        {
            var (context, handler, person) = await BuildHandlerWithPersonAsync();

            using (context)
            {
                var startDate = new DateTime(2024, 3, 1);

                var request = new CreateAstronautDuty
                {
                    Name = person.Name,
                    Rank = "Colonel",
                    DutyTitle = "Commander",
                    DutyStartDate = startDate
                };

                var result = await handler.Handle(request, CancellationToken.None);

                Assert.NotNull(result);
                Assert.True(result.Id.HasValue);

                var detail = await context.AstronautDetails.SingleAsync(d => d.PersonId == person.Id);
                Assert.Equal(request.DutyTitle, detail.CurrentDutyTitle);
                Assert.Equal(request.Rank, detail.CurrentRank);
                Assert.Equal(startDate.Date, detail.CareerStartDate);
                Assert.Null(detail.CareerEndDate);

                var duty = await context.AstronautDuties.SingleAsync(d => d.Id == result.Id.Value);
                Assert.Equal(person.Id, duty.PersonId);
                Assert.Equal(request.Rank, duty.Rank);
                Assert.Equal(request.DutyTitle, duty.DutyTitle);
                Assert.Equal(startDate.Date, duty.DutyStartDate);
                Assert.Null(duty.DutyEndDate);
            }
        }

        [Fact]
        public async Task Handle_UpdatesAstronautDetailAndClosesPreviousDuty_WhenExistingDetailAndDuty()
        {
            var (context, handler, person) = await BuildHandlerWithPersonAsync();

            using (context)
            {
                var initialStartDate = new DateTime(2024, 1, 1);

                var detail = new AstronautDetail
                {
                    PersonId = person.Id,
                    CurrentDutyTitle = "Pilot",
                    CurrentRank = "Major",
                    CareerStartDate = initialStartDate
                };

                context.AstronautDetails.Add(detail);

                var previousDuty = new AstronautDuty
                {
                    PersonId = person.Id,
                    Rank = "Major",
                    DutyTitle = "Pilot",
                    DutyStartDate = initialStartDate
                };

                context.AstronautDuties.Add(previousDuty);
                await context.SaveChangesAsync();

                var newStartDate = new DateTime(2024, 6, 1);

                var request = new CreateAstronautDuty
                {
                    Name = person.Name,
                    Rank = "Colonel",
                    DutyTitle = "Commander",
                    DutyStartDate = newStartDate
                };

                var result = await handler.Handle(request, CancellationToken.None);

                Assert.NotNull(result);
                Assert.True(result.Id.HasValue);

                var updatedDetail = await context.AstronautDetails.SingleAsync(d => d.PersonId == person.Id);
                Assert.Equal(request.DutyTitle, updatedDetail.CurrentDutyTitle);
                Assert.Equal(request.Rank, updatedDetail.CurrentRank);
                Assert.Equal(initialStartDate.Date, updatedDetail.CareerStartDate);
                Assert.Null(updatedDetail.CareerEndDate);

                var closedPreviousDuty = await context.AstronautDuties.SingleAsync(d => d.Id == previousDuty.Id);
                Assert.Equal(newStartDate.AddDays(-1).Date, closedPreviousDuty.DutyEndDate);
            }
        }

        [Fact]
        public async Task Handle_SetsCareerEndDateOnRetirement()
        {
            var (context, handler, person) = await BuildHandlerWithPersonAsync();

            using (context)
            {
                var initialStartDate = new DateTime(2024, 1, 1);

                var detail = new AstronautDetail
                {
                    PersonId = person.Id,
                    CurrentDutyTitle = "Commander",
                    CurrentRank = "Colonel",
                    CareerStartDate = initialStartDate
                };

                context.AstronautDetails.Add(detail);

                var previousDuty = new AstronautDuty
                {
                    PersonId = person.Id,
                    Rank = "Colonel",
                    DutyTitle = "Commander",
                    DutyStartDate = initialStartDate
                };

                context.AstronautDuties.Add(previousDuty);
                await context.SaveChangesAsync();

                var retirementStartDate = new DateTime(2030, 1, 1);

                var request = new CreateAstronautDuty
                {
                    Name = person.Name,
                    Rank = "Colonel",
                    DutyTitle = "RETIRED",
                    DutyStartDate = retirementStartDate
                };

                var result = await handler.Handle(request, CancellationToken.None);

                Assert.NotNull(result);
                Assert.True(result.Id.HasValue);

                var updatedDetail = await context.AstronautDetails.SingleAsync(d => d.PersonId == person.Id);
                Assert.Equal("RETIRED", updatedDetail.CurrentDutyTitle);
                Assert.Equal("Colonel", updatedDetail.CurrentRank);
                Assert.Equal(initialStartDate.Date, updatedDetail.CareerStartDate);
                Assert.Equal(retirementStartDate.AddDays(-1).Date, updatedDetail.CareerEndDate);

                var closedPreviousDuty = await context.AstronautDuties.SingleAsync(d => d.Id == previousDuty.Id);
                Assert.Equal(retirementStartDate.AddDays(-1).Date, closedPreviousDuty.DutyEndDate);
            }
        }
    }
}
