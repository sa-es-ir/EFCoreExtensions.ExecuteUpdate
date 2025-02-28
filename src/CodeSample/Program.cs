using CodeSample;
using CodeSample.Entities;
using EFCoreExtensions;

var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

var dbContext = new SampleDbContext();

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

await dbContext
    .Students
    .Where(x => x.Id == 1)
    .ExecuteUpdateAsync(fieldsToUpdate, cancellationToken);
