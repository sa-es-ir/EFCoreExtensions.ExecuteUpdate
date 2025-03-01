using CodeSample;
using CodeSample.Entities;
using EFCoreExtensions;
using Microsoft.EntityFrameworkCore;

var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(100)).Token;

var dbContext = new SampleDbContext();

// EF Core Built-in ExecuteUpdateAsync
await dbContext
    .Students
    .Where(x => x.Id == 1)
    .ExecuteUpdateAsync(x => x.SetProperty(s => s.Name, "New Name"), cancellationToken);

// EFCoreExtensions ExecuteUpdateAsync
await dbContext
    .Students
    .Where(x => x.Id == 1)
    .ExecuteUpdateAsync(nameof(Student.Name), "New Name", cancellationToken);

var fieldsToUpdate = new Dictionary<string, object?>
{
    { nameof(Student.Name), "New Name" },
    { nameof(Student.PhoneNumber), "1234567890" },
    { nameof(Student.Email), "my@email.com" },
    { nameof(Student.Note), "Student Note" }
};

// EFCoreExtensions ExecuteUpdateAsync
await dbContext
    .Students
    .Where(x => x.Id == 1)
    .ExecuteUpdateAsync(fieldsToUpdate, cancellationToken);
