using System;
using System.Threading;
using System.Threading.Tasks;
using Workforce.Modules.People.Application.Contracts;
using Workforce.Modules.People.Domain;
using Workforce.Modules.People.Infrastructure;

namespace Workforce.Modules.People.Application;

public class PeopleHiringContract : IPeopleHiringContract
{
    private readonly PeopleRepository _repository;

    public PeopleHiringContract(PeopleRepository repository)
    {
        _repository = repository;
    }

    public async Task<HirePersonResult> HireAsync(string tenantId, HirePersonCommand command, CancellationToken ct = default)
    {
        // Check for idempotency: Have we already processed a hire for this idempotency key?
        // Since we didn't have an idempotency table yet, we can check if there's a recent employment
        // for this Person/NationalId/Email, OR we can store the IdempotencyKey somewhere.
        // Wait! The user says: "People creates Person/Employment ... Retry exact request -> same PersonId ... no duplicate Person ... one authoritative business result"
        // Let's implement idempotency using the IdempotencyKey.
        var existingResult = await _repository.GetHireIdempotencyAsync(tenantId, command.IdempotencyKey, ct);
        if (existingResult != null)
        {
            return new HirePersonResult
            {
                PersonId = existingResult.PersonId,
                EmploymentId = existingResult.EmploymentId,
                AssignmentId = existingResult.AssignmentId,
                WasIdempotentHit = true
            };
        }

        // If not found, create new domain objects
        var personId = Guid.NewGuid();
        var employmentId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();

        var person = new Person(
            personId,
            new Workforce.SharedKernel.Primitives.TenantId(Guid.Parse(tenantId)),
            command.FirstNameEn,
            command.LastNameEn,
            command.FirstNameAr,
            command.LastNameAr,
            command.DateOfBirth,
            command.Gender,
            command.Nationality,
            command.EncryptedNationalId,
            command.NationalIdHash,
            command.MaskedNationalId,
            command.Email,
            command.PhoneNumber
        );

        var empNumber = string.IsNullOrWhiteSpace(command.EmployeeNumber)
            ? $"EMP-{Random.Shared.Next(100000, 999999)}"
            : command.EmployeeNumber;

        var employment = new Employment(
            employmentId,
            new Workforce.SharedKernel.Primitives.TenantId(Guid.Parse(tenantId)),
            personId,
            new Workforce.SharedKernel.Primitives.LegalEntityId(command.LegalEntityId),
            empNumber,
            command.HireDate,
            null,
            EmploymentStatus.Active
        );

        var assignment = new EmploymentAssignment(
            assignmentId,
            employmentId,
            command.OrganizationUnitId,
            command.TitleEn,
            command.TitleAr,
            command.HireDate,
            null,
            command.PositionId,
            command.LocationId,
            command.HiringManagerId,
            true
        );

        try
        {
            await _repository.CreateEmployeeWithIdempotencyAsync(person, employment, assignment, command.IdempotencyKey, ct);

            return new HirePersonResult
            {
                PersonId = personId,
                EmploymentId = employmentId,
                AssignmentId = assignmentId,
                WasIdempotentHit = false
            };
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Idempotency key already exists"))
        {
            var winner = await _repository.GetHireIdempotencyAsync(tenantId, command.IdempotencyKey, ct);
            if (winner != null)
            {
                return new HirePersonResult
                {
                    PersonId = winner.PersonId,
                    EmploymentId = winner.EmploymentId,
                    AssignmentId = winner.AssignmentId,
                    WasIdempotentHit = true
                };
            }
            throw;
        }
    }
}
