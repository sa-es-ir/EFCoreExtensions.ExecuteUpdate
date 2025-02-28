using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;
using System.Reflection;

namespace EFCoreExtensions;

public static class ExecutUpdateExtensions
{
    static MethodInfo UpdateMethodInfo =
        typeof(EntityFrameworkQueryableExtensions).GetMethod(nameof(EntityFrameworkQueryableExtensions.ExecuteUpdate))!;

    static MethodInfo UpdateAsyncMethodInfo =
        typeof(EntityFrameworkQueryableExtensions).GetMethod(nameof(EntityFrameworkQueryableExtensions.ExecuteUpdateAsync))!;

    public static int ExecuteUpdate(this IQueryable query, string fieldName, object? fieldValue)
    {
        var updateBody = SetupUpdateBody(query.ElementType,
            new Dictionary<string, object?> { { fieldName, fieldValue } });

        return (int)UpdateMethodInfo.MakeGenericMethod(query.ElementType).Invoke(null, [query, updateBody])!;
    }

    public static Task<int> ExecuteUpdateAsync(this IQueryable query, string fieldName, object? fieldValue, CancellationToken cancellationToken = default)
    {
        var updateBody = SetupUpdateBody(query.ElementType,
            new Dictionary<string, object?> { { fieldName, fieldValue } });

        return (Task<int>)UpdateAsyncMethodInfo.MakeGenericMethod(query.ElementType).Invoke(null, [query, updateBody, cancellationToken])!;
    }

    public static int ExecuteUpdate(this IQueryable query, IReadOnlyDictionary<string, object?> fieldValues)
    {
        var updateBody = SetupUpdateBody(query.ElementType, fieldValues);

        return (int)UpdateMethodInfo.MakeGenericMethod(query.ElementType).Invoke(null, [query, updateBody])!;
    }

    public static Task<int> ExecuteUpdateAsync(this IQueryable query, IReadOnlyDictionary<string, object?> fieldValues, CancellationToken cancellationToken = default)
    {
        var updateBody = SetupUpdateBody(query.ElementType, fieldValues);

        return (Task<int>)UpdateAsyncMethodInfo.MakeGenericMethod(query.ElementType).Invoke(null, [query, updateBody, cancellationToken])!;
    }

    private static LambdaExpression SetupUpdateBody(Type entityType, IReadOnlyDictionary<string, object?> fieldValues)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        ArgumentNullException.ThrowIfNull(fieldValues);

        var setParameter = Expression.Parameter(typeof(SetPropertyCalls<>).MakeGenericType(entityType), "s");
        var entityParameter = Expression.Parameter(entityType, "e");

        Expression setBody = setParameter;

        foreach (var pair in fieldValues)
        {
            var propertyExpression = Expression.PropertyOrField(entityParameter, pair.Key);
            var valueExpression = ConvertToExpression(propertyExpression.Type, pair.Value);

            setBody = Expression.Call(setBody, nameof(SetPropertyCalls<object>.SetProperty),
                [propertyExpression.Type], Expression.Lambda(propertyExpression, entityParameter), valueExpression);
        }

        var updateBody = Expression.Lambda(setBody, setParameter);

        return updateBody;
    }

    private static Expression ConvertToExpression(Type desiredType, object? value)
    {
        ArgumentNullException.ThrowIfNull(desiredType);

        return value switch
        {
            null => Expression.Default(desiredType),
            _ when value.GetType() != desiredType => Expression.Convert(Expression.Constant(value), desiredType),
            _ => Expression.Constant(value)
        };
    }
}
