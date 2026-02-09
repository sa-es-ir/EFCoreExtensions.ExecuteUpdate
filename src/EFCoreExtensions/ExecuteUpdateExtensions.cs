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

        // Find the SetProperty<TProperty> method that takes (Expression<Func<T, TProperty>>, TProperty)
        // This is the overload where second parameter is the value directly, not an expression
        var setPropertyMethod = setPropertyCalls.GetType()
            .GetMethods()
            .FirstOrDefault(m =>
                m.Name == "SetProperty" &&
                m.IsGenericMethod &&
                m.GetParameters().Length == 2 &&
                m.GetParameters()[0].ParameterType.Name == "Expression`1" &&
                !m.GetParameters()[1].ParameterType.Name.StartsWith("Expression"))
            ?? throw new InvalidOperationException("Could not find SetProperty<TProperty>(Expression<Func<T, TProperty>>, TProperty) method");

        var genericSetPropertyMethod = setPropertyMethod.MakeGenericMethod(propertyInfo.PropertyType);

        // Create lambda: e => e.PropertyName
        var entityParam = Expression.Parameter(entityType, "e");
        var propertyAccess = Expression.Property(entityParam, propertyInfo);
        var propertyLambda = Expression.Lambda(propertyAccess, entityParam);

        // Convert the value to the property type
        var convertedValue = ConvertValueToPropertyType(value, propertyInfo.PropertyType);

        // Invoke: setPropertyCalls.SetProperty(e => e.Property, value)
        // Second parameter is the actual value, not an expression
        genericSetPropertyMethod.Invoke(setPropertyCalls, [propertyLambda, convertedValue]);
    }

    private static object? ConvertValueToPropertyType(object? value, Type targetType)
    {
        if (value == null)
        {
            if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                throw new InvalidOperationException($"Cannot assign null to non-nullable type {targetType.Name}");
            
            return null;
        }

        var valueType = value.GetType();
        
        // If already the correct type, return as-is
        if (valueType == targetType)
            return value;

        // Check if target is nullable value type
        var underlyingType = Nullable.GetUnderlyingType(targetType);
        
        if (underlyingType != null)
        {
            // Target is Nullable<T>
            // Convert to underlying type
            object underlyingValue;
            if (valueType == underlyingType)
            {
                underlyingValue = value;
            }
            else
            {
                try
                {
                    underlyingValue = Convert.ChangeType(value, underlyingType);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Cannot convert value of type '{valueType.Name}' to '{underlyingType.Name}'", ex);
                }
            }
            
            // Create Nullable<T> using reflection to get a proper nullable instance
            // Use the underlying type to create a method that returns Nullable<T>
            var method = typeof(ExecuteUpdateExtensions)
                .GetMethod(nameof(CreateNullableValue), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .MakeGenericMethod(underlyingType);
            
            return method.Invoke(null, [underlyingValue])!;
        }
        else
        {
            // Target is not nullable
            try
            {
                return Convert.ChangeType(value, targetType);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Cannot convert value of type '{valueType.Name}' to '{targetType.Name}'", ex);
            }
        }
    }

    private static T? CreateNullableValue<T>(T value) where T : struct
    {
        return new T?(value);
    }
}
