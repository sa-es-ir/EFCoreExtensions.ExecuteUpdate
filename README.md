# An EF Core extension to have `ExecuteUpdate` dynamically

### EF Core built-in
```csharp
await dbContext
    .Students
    .Where(x => x.Id == 1)
    .ExecuteUpdateAsync(x => x.SetProperty(s => s.Name, "New Name"), cancellationToken);
```

### With Extension
```csharp
await dbContext
    .Students
    .Where(x => x.Id == 1)
    .ExecuteUpdateAsync(nameof(Student.Name), "New Name", cancellationToken);
```

### ExecuteUpdate dynamically
```csharp
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
```
