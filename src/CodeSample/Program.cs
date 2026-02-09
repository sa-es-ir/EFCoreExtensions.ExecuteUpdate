using CodeSample;
using CodeSample.Entities;
using EFCoreExtensions;
using Microsoft.EntityFrameworkCore;

var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

var dbContext = new SampleDbContext();

var student = new Student
{
    Name = "John Doe",
    PhoneNumber = "9876543210",
    Email = "something@example.com",
    BirthDate = DateTime.Now,
    Note = "Initial Note"
};

dbContext
    .Students.Add(student);

await dbContext.SaveChangesAsync(cancellationToken);

// EF Core Built-in ExecuteUpdateAsync
await dbContext
    .Students
    .Where(x => x.Id == student.Id)
    .ExecuteUpdateAsync(x => x.SetProperty(s => s.Name, "New Name"), cancellationToken);

// EFCoreExtensions ExecuteUpdateAsync
await dbContext
    .Students
    .Where(x => x.Id == student.Id)
    .ExecuteUpdateAsync(nameof(Student.Name), "New Name", cancellationToken);

var fieldsToUpdate = new Dictionary<string, object?>
{
    { nameof(Student.BirthDate), DateTime.Now.AddDays(-10) },
    { nameof(Student.Email), "new_email@mail.com" },
    { nameof(Student.Note), $"updated_note_{Guid.NewGuid():N}" },

};

// Test just DateTime update
Console.WriteLine("Testing DateTime update...");
await dbContext
    .Students
    .Where(x => x.Id == student.Id)
    .ExecuteUpdateAsync(fieldsToUpdate, cancellationToken);

Console.WriteLine("DateTime update succeeded!");
