using GitClub.Database.Models;
using GitClub.Infrastructure.Errors;
using GitClub.Infrastructure.Errors.Translators;
using GitClub.Infrastructure.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitClub.Tests.Infrastructure.Errors
{
    [TestClass]
    public class ExceptionToApplicationErrorMapperTests
    {
        private ExceptionToApplicationErrorMapper _errorMapper = null!;

        public ExceptionToApplicationErrorMapperTests()
        {

            var options = new ExceptionToApplicationErrorMapperOptions
            {
                IncludeExceptionDetails = true
            };

            _errorMapper = new ExceptionToApplicationErrorMapper(
                logger: new NullLogger<ExceptionToApplicationErrorMapper>(),
                options: Options.Create(options),
                translators:
                [
                    new DefaultExceptionTranslator(new NullLogger<DefaultExceptionTranslator>()),
                    new InvalidModelStateExceptionTranslator(new NullLogger<InvalidModelStateExceptionTranslator>()),
                    new ApplicationErrorExceptionTranslator(new NullLogger<ApplicationErrorExceptionTranslator>()),
                ]);
        }

        [TestMethod]
        public void CreateApplicationErrorResult_CheckAllExceptionsAreTranslatedCorrectly()
        {
            // Arrange
            var testData = new List<(Exception Exception, string ErrorCode)>();

            testData.Add((new Exception { }, ErrorCodes.InternalServerError));

            testData.Add((new AuthenticationFailedException { }, ErrorCodes.AuthenticationFailed));
            testData.Add((new AuthorizationFailedException { }, ErrorCodes.AuthorizationFailed));

            testData.Add((new CannotDeleteOwnUserException { UserId = 1 }, ErrorCodes.CannotDeleteOwnUser));

            testData.Add((new EntityConcurrencyException { EntityId = 1, EntityName = nameof(User) }, ErrorCodes.EntityConcurrencyFailure));
            testData.Add((new EntityNotFoundException { EntityId = 1, EntityName = nameof(User) }, ErrorCodes.EntityNotFound));
            testData.Add((new EntityUnauthorizedAccessException { EntityId = 1, EntityName = nameof(User), UserId = 1 }, ErrorCodes.EntityUnauthorized));

            var modelStateDictionary = new ModelStateDictionary();

            modelStateDictionary.AddModelError("mykey", "My Message");

            testData.Add((new InvalidModelStateException { ModelStateDictionary = modelStateDictionary }, ErrorCodes.ValidationFailed));
            
            testData.Add((new TeamAlreadyAssignedToOrganizationException { OrganizationId = 2, TeamId = 3 }, ErrorCodes.TeamAlreadyAssignedToOrganization));
            testData.Add((new TeamAlreadyAssignedToRepositoryException { RepositoryId = 4, TeamId = 1 }, ErrorCodes.TeamAlreadyAssignedToRepository));
            testData.Add((new TeamNotAssignedToOrganizationException { OrganizationId = 2, TeamId = 3 }, ErrorCodes.TeamNotAssignedToOrganization));
            testData.Add((new TeamNotAssignedToRepositoryException { RepositoryId = 4, TeamId = 1 }, ErrorCodes.TeamNotAssignedToRepository));

            testData.Add((new UserAlreadyAssignedToOrganizationException { OrganizationId = 2, UserId = 3 }, ErrorCodes.UserAlreadyAssignedToOrganization));
            testData.Add((new UserAlreadyAssignedToRepositoryException { RepositoryId = 4, UserId = 1 }, ErrorCodes.UserAlreadyAssignedToRepository));
            testData.Add((new UserAlreadyAssignedToTeamException { TeamId = 4, UserId = 1 }, ErrorCodes.UserAlreadyAssignedToTeam));
            testData.Add((new UserNotAssignedToOrganizationException { OrganizationId = 2, UserId = 3, Role = OrganizationRoleEnum.Member }, ErrorCodes.UserNotAssignedToOrganization));
            testData.Add((new UserNotAssignedToRepositoryException { RepositoryId = 4, UserId = 1 }, ErrorCodes.UserNotAssignedToRepository));
            testData.Add((new UserNotAssignedToTeamException { UserId = 4, TeamId = 1 }, ErrorCodes.UserNotAssignedToTeam));

            // Act
            foreach(var testDataRecord in testData)
            {
                var applicationErrorResult = _errorMapper.CreateApplicationErrorResult(new DefaultHttpContext(), testDataRecord.Exception);

                // Assert
                Assert.AreEqual(testDataRecord.ErrorCode, applicationErrorResult.Error.Code);
            }
        }
    }
}
