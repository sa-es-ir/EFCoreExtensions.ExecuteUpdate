# EF Core 10 Migration Guide

## Summary

EF Core 10 introduced a **breaking change** to the `ExecuteUpdate` API that prevents the original `ExecuteUpdateExtensions.cs` from working. A new file `ExecuteUpdateExtensions10.cs` has been created to support EF Core 10.

## What Changed in EF Core 10?

### The Breaking Change

**EF Core 9 and earlier:**
```csharp
ExecuteUpdate(Expression<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>> setters)
```

**EF Core 10:**
```csharp
ExecuteUpdate(Func<SetPropertyCalls<T>, SetPropertyCalls<T>> setters)
```

The parameter changed from an **expression tree** (`Expression<Func<...>>`) to a **regular delegate** (`Func<...>`).

### Why This Breaks the Old Extension

1. The old extension builds `Expression<Func<...>>` using expression trees
2. It invokes `ExecuteUpdate` via reflection with the expression
3. EF Core 10 now expects a regular `Func<>`, not `Expression<Func<>>`

### The Good News

This change actually **simplifies** dynamic updates! Instead of building complex expression trees, EF Core 10 allows using regular code with if statements inside the lambda:

```csharp
await context.Blogs.ExecuteUpdateAsync(s =>
{
    s.SetProperty(b => b.Views, 8);
    if (nameChanged)
    {
        s.SetProperty(b => b.Name, "foo");
    }
});
```

## New Implementation (`ExecuteUpdateExtensions10.cs`)

The new extension takes advantage of this simpler API:

```csharp
public static Task<int> ExecuteUpdateAsync<TEntity>(
    this IQueryable<TEntity> query, 
    IReadOnlyDictionary<string, object?> fieldValues, 
    CancellationToken cancellationToken = default)
    where TEntity : class
{
    return query.ExecuteUpdateAsync(s =>
    {
        foreach (var pair in fieldValues)
        {
            SetPropertyDynamically(s, typeof(TEntity), pair.Key, pair.Value);
        }
    }, cancellationToken);
}
```

### Key Differences:

1. **No more expression tree building** - We pass a regular lambda to `ExecuteUpdateAsync`
2. **Simpler reflection** - We call `SetProperty` dynamically inside the lambda
3. **SetProperty returns void** in EF Core 10 (previously it returned `SetPropertyCalls<T>` for chaining)

## Project Configuration

The project has been updated to:
- Target `.NET 10.0`
- Use `EF Core 10.0.0`
- Exclude the old `ExecuteUpdateExtensions.cs` from compilation

```xml
<ItemGroup>
  <!-- Exclude old extension that doesn't work with EF Core 10 -->
  <Compile Remove="ExecuteUpdateExtensions.cs" />
</ItemGroup>
```

## Usage (Unchanged)

The public API remains the same:

```csharp
// Single property
await dbContext.Students
    .Where(x => x.Id == 1)
    .ExecuteUpdateAsync("Name", "New Name", cancellationToken);

// Multiple properties
var fieldsToUpdate = new Dictionary<string, object?>
{
    { "Name", "New Name" },
    { "Email", "new@email.com" }
};

await dbContext.Students
    .Where(x => x.Id == 1)
    .ExecuteUpdateAsync(fieldsToUpdate, cancellationToken);
```

## References

- [EF Core 10 Breaking Changes](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/breaking-changes#ExecuteUpdateAsync-lambda)
- [EF Core 10 What's New](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew)
