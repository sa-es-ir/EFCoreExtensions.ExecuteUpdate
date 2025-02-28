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

    public static int CustomExecuteUpdate(this IQueryable query, string fieldName, object? fieldValue)
    {
        var updateBody = BuildUpdateBody(query.ElementType,
            new Dictionary<string, object?> { { fieldName, fieldValue } });

        return (int)UpdateMethodInfo.MakeGenericMethod(query.ElementType).Invoke(null, [query, updateBody])!;
    }

    public static Task<int> CustomExecuteUpdateAsync(this IQueryable query, string fieldName, object? fieldValue, CancellationToken cancellationToken = default)
    {
        var updateBody = BuildUpdateBody(query.ElementType,
            new Dictionary<string, object?> { { fieldName, fieldValue } });

        return (Task<int>)UpdateAsyncMethodInfo.MakeGenericMethod(query.ElementType).Invoke(null, [query, updateBody, cancellationToken])!;
    }

    public static int CustomExecuteUpdate(this IQueryable query, IReadOnlyDictionary<string, object?> fieldValues)
    {
        var updateBody = BuildUpdateBody(query.ElementType, fieldValues);

        return (int)UpdateMethodInfo.MakeGenericMethod(query.ElementType).Invoke(null, [query, updateBody])!;
    }

    public static Task<int> CustomExecuteUpdateAsync(this IQueryable query, IReadOnlyDictionary<string, object?> fieldValues, CancellationToken cancellationToken = default)
    {
        var updateBody = BuildUpdateBody(query.ElementType, fieldValues);

        return (Task<int>)UpdateAsyncMethodInfo.MakeGenericMethod(query.ElementType).Invoke(null, [query, updateBody, cancellationToken])!;
    }

    static LambdaExpression BuildUpdateBody(Type entityType, IReadOnlyDictionary<string, object?> fieldValues)
    {
        var setParam = Expression.Parameter(typeof(SetPropertyCalls<>).MakeGenericType(entityType), "s");
        var objParam = Expression.Parameter(entityType, "e");

        Expression setBody = setParam;

        foreach (var pair in fieldValues)
        {
            var propExpression = Expression.PropertyOrField(objParam, pair.Key);
            var valueExpression = ValueForType(propExpression.Type, pair.Value);

            setBody = Expression.Call(setBody, nameof(SetPropertyCalls<object>.SetProperty),
                [propExpression.Type], Expression.Lambda(propExpression, objParam), valueExpression);
        }

        var updateBody = Expression.Lambda(setBody, setParam);

        return updateBody;
    }

    static Expression ValueForType(Type desiredType, object? value)
    {
        if (value == null)
        {
            return Expression.Default(desiredType);
        }

        if (value.GetType() != desiredType)
        {
            return Expression.Convert(Expression.Constant(value), desiredType);
        }

        return Expression.Constant(value);
    }
}
