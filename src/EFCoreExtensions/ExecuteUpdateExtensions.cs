using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace EFCoreExtensions;

/// <summary>
/// EF Core 10+ compatible extension for dynamic ExecuteUpdate operations.
/// EF Core 10 changed ExecuteUpdate to accept Func instead of Expression<Func>, enabling simpler dynamic updates.
/// For EF Core <=9, see ExecuteUpdateExtensionsUpToEFCore9.cs which uses reflection to build the necessary expression trees for the older API.
/// Based on: https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/breaking-changes#ExecuteUpdateAsync-lambda
/// </summary>
public static class ExecuteUpdateExtensions
{
    public static int ExecuteUpdate<TEntity>(this IQueryable<TEntity> query, string fieldName, object? fieldValue)
        where TEntity : class
    {
        return query.ExecuteUpdate(s =>
        {
            SetPropertyDynamically(s, typeof(TEntity), fieldName, fieldValue);
        });
    }

    public static Task<int> ExecuteUpdateAsync<TEntity>(this IQueryable<TEntity> query, string fieldName, object? fieldValue, CancellationToken cancellationToken = default)
        where TEntity : class
    {
        return query.ExecuteUpdateAsync(s =>
        {
            SetPropertyDynamically(s, typeof(TEntity), fieldName, fieldValue);
        }, cancellationToken);
    }

    public static int ExecuteUpdate<TEntity>(this IQueryable<TEntity> query, IReadOnlyDictionary<string, object?> fieldValues)
        where TEntity : class
    {
        return query.ExecuteUpdate(s =>
        {
            foreach (var pair in fieldValues)
            {
                SetPropertyDynamically(s, typeof(TEntity), pair.Key, pair.Value);
            }
        });
    }

    public static Task<int> ExecuteUpdateAsync<TEntity>(this IQueryable<TEntity> query, IReadOnlyDictionary<string, object?> fieldValues, CancellationToken cancellationToken = default)
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

    private static void SetPropertyDynamically(object setPropertyCalls, Type entityType, string propertyName, object? value)
    {
        var propertyInfo = entityType.GetProperty(propertyName)
            ?? throw new InvalidOperationException($"Property '{propertyName}' not found on type '{entityType.Name}'");

        // Find the SetProperty<TProperty> method  
        var setPropertyMethod = setPropertyCalls.GetType()
            .GetMethods()
            .First(m => m.Name == "SetProperty" && m.IsGenericMethod && m.GetParameters().Length == 2);

        var genericSetPropertyMethod = setPropertyMethod.MakeGenericMethod(propertyInfo.PropertyType);

        // Create lambda: e => e.PropertyName
        var entityParam = Expression.Parameter(entityType, "e");
        var propertyAccess = Expression.Property(entityParam, propertyInfo);
        var propertyLambda = Expression.Lambda(propertyAccess, entityParam);

        // Convert value to target type if needed
        var convertedValue = ConvertValue(propertyInfo.PropertyType, value);

        // Create lambda for the value: e => convertedValue
        var valueLambda = Expression.Lambda(
            Expression.Constant(convertedValue, propertyInfo.PropertyType),
            entityParam);

        // Invoke: setPropertyCalls.SetProperty(e => e.Property, e => value)
        // Note: In EF Core 10, SetProperty returns void (not the setPropertyCalls object)
        genericSetPropertyMethod.Invoke(setPropertyCalls, [propertyLambda, valueLambda]);
    }

    private static object? ConvertValue(Type targetType, object? value)
    {
        if (value == null)
            return targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null
                ? throw new InvalidOperationException($"Cannot assign null to non-nullable type {targetType.Name}")
                : null;

        if (value.GetType() == targetType)
            return value;

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        return Convert.ChangeType(value, underlyingType);
    }
}
