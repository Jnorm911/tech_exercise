using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using Microsoft.Extensions.Logging.Abstractions;
using StargateAPI.Business.Queries;


namespace StrategyAPI.Tests.Astronaut
{
    public class GetAstronautTests
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
        public async Task ReturnsEmptyResult_WhenPersonNotFound()
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            using var context = CreateContext(connection);

            var logger = NullLogger<GetAstronautDutiesByNameHandler>.Instance;
            var handler = new GetAstronautDutiesByNameHandler(context, logger);

            var request = new GetAstronautDutiesByName { Name = "Nobody" };

            var result = await handler.Handle(request, default);

            Assert.Null(result.Person);
            Assert.Empty(result.AstronautDuties);
        }

        [Fact]
        public async Task ReturnsPersonAndDuties_WhenPersonExists()
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            using var context = CreateContext(connection);

            // Arrange a person
            var person = new Person
            {
                Name = "Teal'c",
            };
            context.People.Add(person);
            await context.SaveChangesAsync();

            // Arrange astronaut detail
            var detail = new AstronautDetail
            {
                PersonId = person.Id,
                CurrentRank = "Jaffa Warrior",
                CurrentDutyTitle = "First Prime",
                CareerStartDate = new DateTime(1997, 1, 1)
            };
            context.AstronautDetails.Add(detail);

            // Arrange duties
            context.AstronautDuties.Add(new AstronautDuty
            {
                PersonId = person.Id,
                Rank = "Jaffa Warrior",
                DutyTitle = "First Prime",
                DutyStartDate = new DateTime(1997, 1, 1)
            });

            await context.SaveChangesAsync();

            var logger = NullLogger<GetAstronautDutiesByNameHandler>.Instance;
            var handler = new GetAstronautDutiesByNameHandler(context, logger);

            var request = new GetAstronautDutiesByName { Name = "Teal'c" };

            // Act
            var result = await handler.Handle(request, default);

            // Assert
            Assert.NotNull(result.Person);
            Assert.Equal("Teal'c", result.Person.Name);
            Assert.Single(result.AstronautDuties);
        }
    }
}