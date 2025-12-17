using System.Globalization;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace Berkat.Helper;

public class FilterDto
{
    public required string FieldName { get; set; }
    public required string Operator { get; set; }
    public string? Value { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
}

public class FilterConditionDto
{
    public required string Field { get; set; }

    /// <summary>
    /// Field operator (EQ, NEQ, GT, LT, GTE, LTE, LIKE)
    /// </summary>
    public required string Type { get; set; }

    public required object? Value { get; set; }
}

public class FilterGroupDto
{
    /// <summary>
    /// Logical group operator (AND/OR)
    /// </summary>
    public required string Operator { get; set; }

    /// <summary>
    /// Bisa berupa <see cref="FilterConditionDto"/> atau <see cref="FilterGroupDto"/>
    /// </summary>
    public required List<JsonElement> Groups { get; set; }
}

public static class QueryableExtensions
{
    public static IQueryable<T> ApplyDynamicFilters<T>(
        this IQueryable<T> query,
        List<FilterDto>? filters,
        Dictionary<string, string> fieldMapping
    )
    {
        if (filters == null || !filters.Any()) return query;

        foreach (var filter in filters)
        {
            if (string.IsNullOrWhiteSpace(filter.FieldName)
                || string.IsNullOrWhiteSpace(filter.Operator))
                continue;

            if (!fieldMapping.TryGetValue(filter.FieldName, out var fieldPath))
            {
                continue;
            }

            Type currentType = typeof(T);
            PropertyInfo? propertyInfo = null;

            foreach (var member in fieldPath.Split('.'))
            {
                propertyInfo = currentType.GetProperty(
                    member,
                    BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance
                );
                if (propertyInfo == null) break;
                currentType = propertyInfo.PropertyType;
            }

            if (propertyInfo == null) continue;

            var targetType = Nullable.GetUnderlyingType(propertyInfo.PropertyType)
                             ?? propertyInfo.PropertyType;
            var style = NumberStyles.Any;
            var culture = CultureInfo.InvariantCulture;

            try
            {
                //string filters
                if (targetType == typeof(string))
                {
                    var searchVal = filter.Value ?? "";
                    switch (filter.Operator)
                    {
                        case "equal":
                            query = query.Where($"{fieldPath} == @0", searchVal);
                            break;
                        case "contains":
                            query = query.Where(
                                $"{fieldPath} != null && {fieldPath}.Contains(@0)",
                                searchVal
                            );
                            break;
                    }
                }

                //boolean filters
                else if (targetType == typeof(bool))
                {
                    if (bool.TryParse(filter.Value, out var boolVal))
                    {
                        if (filter.Operator == "equal")
                        {
                            query = query.Where($"{fieldPath} == @0", boolVal);
                        }
                    }
                }

                //date filters
                else if (targetType == typeof(DateTime) || targetType == typeof(DateTimeOffset))
                {
                    if (DateTime.TryParse(
                            filter.Value,
                            culture,
                            DateTimeStyles.None,
                            out var dateVal
                        ))
                    {
                        switch (filter.Operator)
                        {
                            case "equal":
                                query = query.Where($"{fieldPath} == @0", dateVal);
                                break;
                            case "moreThan":
                                query = query.Where($"{fieldPath} > @0", dateVal);
                                break;
                            case "lessThan":
                                query = query.Where($"{fieldPath} < @0", dateVal);
                                break;
                        }
                    }

                    if (filter.Operator == "valueRange"
                        && DateTime.TryParse(
                            filter.From,
                            culture,
                            DateTimeStyles.None,
                            out var dateFrom
                        )
                        && DateTime.TryParse(
                            filter.To,
                            culture,
                            DateTimeStyles.None,
                            out var dateTo
                        ))
                    {
                        query = query.Where(
                            $"{fieldPath} >= @0 && {fieldPath} <= @1",
                            dateFrom,
                            dateTo
                        );
                    }
                }

                else
                {
                    //numeric filters
                    switch (filter.Operator)
                    {
                        case "valueRange":
                            if (decimal.TryParse(filter.From, style, culture, out var f)
                                && decimal.TryParse(filter.To, style, culture, out var t))
                                query = query.Where(
                                    $"{fieldPath} >= @0 && {fieldPath} <= @1",
                                    f,
                                    t
                                );
                            break;

                        case "equal":
                            if (decimal.TryParse(filter.Value, style, culture, out var eq))
                                query = query.Where($"{fieldPath} == @0", eq);
                            break;

                        case "moreThan":
                            if (decimal.TryParse(filter.Value, style, culture, out var mt))
                                query = query.Where($"{fieldPath} > @0", mt);
                            break;

                        case "lessThan":
                            if (decimal.TryParse(filter.Value, style, culture, out var lt))
                                query = query.Where($"{fieldPath} < @0", lt);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DynamicFilter Error] Field: {fieldPath}, Msg: {ex.Message}");
            }
        }

        return query;
    }

    public static IQueryable<T> ApplySorting<T>(
        this IQueryable<T> query,
        string? fieldName,
        string? sortDir = "asc"
    )
    {
        if (fieldName == null) return query;

        var parameter = Expression.Parameter(typeof(T), "p");

        var fieldMember = GetMemberExpression(parameter, fieldName);
        if (fieldMember == null) return query;

        var orderByMethodName = sortDir == "asc"
            ? nameof(Queryable.OrderBy)
            : nameof(Queryable.OrderByDescending);
        var orderByExpression = Expression.Lambda(fieldMember, parameter);

        // Method<T, TKey>(IQueryable<T> source, Expression<Func<T, TKey>> keySelector)
        var method = typeof(Queryable).GetMethods()
            .Where(m =>
                m.Name == orderByMethodName
                && m.IsGenericMethodDefinition
                && m.GetParameters().Length == 2
            )
            .Single(m =>
                m.GetParameters()[1].ParameterType.GetGenericTypeDefinition()
                == typeof(Expression<>)
            );
        var genericMethod = method.MakeGenericMethod(typeof(T), fieldMember.Type);

        return (IQueryable<T>)genericMethod.Invoke(null, [query, orderByExpression])!;
    }

    private static readonly JsonSerializerOptions Options
        = new() { PropertyNameCaseInsensitive = true };

    public static IQueryable<T> ApplyDynamicFiltersNew<T>(
        this IQueryable<T> query,
        string? filtersJson
    )
    {
        if (string.IsNullOrWhiteSpace(filtersJson)) return query;
        try
        {
            var rootGroup = JsonSerializer.Deserialize<FilterGroupDto>(filtersJson, Options);
            if (rootGroup == null || rootGroup.Groups.Count == 0)
            {
                return query;
            }

            var parameter = Expression.Parameter(typeof(T), "p");

            var whereExpression = BuildLinqExpression<T>(rootGroup, parameter);
            if (whereExpression == null)
            {
                return query;
            }

            var fullExpression = Expression.Lambda<Func<T, bool>>(whereExpression, parameter);
            return query.Where(fullExpression);
        }
        catch (Exception err)
        {
            throw new Exception($"Error parsing FiltersJson: {err.Message}");
        }
    }

    private static Expression? BuildLinqExpression<T>(
        FilterGroupDto group,
        ParameterExpression parameter
    )
    {
        Expression? combinedExpression = null;
        Func<Expression, Expression, Expression> combineLogic
            = group.Operator.Equals("OR", StringComparison.CurrentCultureIgnoreCase)
                ? Expression.OrElse
                : Expression.AndAlso;

        foreach (var node in group.Groups)
        {
            Expression? nodeExpression = null;

            if (node.TryGetProperty("groups", out _))
            {
                var parsedNestedGroup = node.Deserialize<FilterGroupDto>(Options);
                if (parsedNestedGroup == null)
                    throw new Exception($"Error parsing nested group {node}: it's null");

                // handle group rekursif
                nodeExpression = BuildLinqExpression<T>(parsedNestedGroup, parameter);
            }
            else if (node.TryGetProperty("field", out _))
            {
                var parsedCondition = node.Deserialize<FilterConditionDto>(Options);
                if (parsedCondition == null)
                    throw new Exception($"Error parsing condition {node}: it's null");

                // handle condition
                nodeExpression = BuildConditionLinqExpression(parsedCondition, parameter);
            }

            if (nodeExpression != null)
            {
                // apply atau combine expression
                combinedExpression = combinedExpression == null
                    ? nodeExpression
                    : combineLogic(combinedExpression, nodeExpression);
            }
        }

        return combinedExpression;
    }

    private static Expression BuildConditionLinqExpression(
        FilterConditionDto condition,
        ParameterExpression parameter
    )
    {
        var fieldMember = GetMemberExpression(parameter, condition.Field);
        if (fieldMember == null) return Expression.Constant(true); // condition ignored

        var targetType = fieldMember.Type;

        var value = condition.Value;
        if (value is JsonElement jsonElement)
            value = GetValueFromElement(jsonElement, targetType);

        // handle null comparison
        if (value == null)
        {
            var nullConstant = Expression.Constant(null, targetType);
            if (condition.Type.Equals("EQ", StringComparison.CurrentCultureIgnoreCase))
                return Expression.Equal(fieldMember, nullConstant);
            if (condition.Type.Equals("NEQ", StringComparison.CurrentCultureIgnoreCase))
                return Expression.NotEqual(fieldMember, nullConstant);

            return Expression.Constant(false);
        }

        var constant = Expression.Constant(value, targetType);

        return condition.Type.ToUpper() switch
        {
            "EQ" => Expression.Equal(fieldMember, constant),
            "NEQ" => Expression.NotEqual(fieldMember, constant),
            "GT" => Expression.GreaterThan(fieldMember, constant),
            "LT" => Expression.LessThan(fieldMember, constant),
            "GTE" => Expression.GreaterThanOrEqual(fieldMember, constant),
            "LTE" => Expression.LessThanOrEqual(fieldMember, constant),
            "LIKE" => BuildStringContainsExpression(fieldMember, constant),
            _ => Expression.Constant(true)
        };
    }

    private static MemberExpression? GetMemberExpression(Expression expression, string path)
    {
        var parts = path.Split('.');

        var currentExpression = expression;
        foreach (var part in parts)
        {
            var prop = currentExpression.Type.GetProperty(
                part,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance
            );

            if (prop != null) currentExpression = Expression.Property(currentExpression, prop);
            else return null;
        }

        return currentExpression as MemberExpression;
    }

    private static Expression BuildStringContainsExpression(
        MemberExpression member,
        ConstantExpression constant
    )
    {
        if (member.Type != typeof(string)) return Expression.Constant(false);

        var containsMethod = typeof(string).GetMethod("Contains", [typeof(string)]);
        return Expression.Call(member, containsMethod!, constant);
    }

    private static object? GetValueFromElement(JsonElement element, Type targetType)
    {
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlyingType == typeof(bool))
        {
            if (element.ValueKind == JsonValueKind.String)
            {
                return bool.TryParse(element.GetString(), out var boolean) && boolean;
            }

            return element.GetBoolean();
        }

        if (underlyingType == typeof(DateOnly)
            && element.ValueKind == JsonValueKind.String
            && DateOnly.TryParse(element.GetString(), out var date))
        {
            return date;
        }

        if (underlyingType == typeof(DateTime)
            && element.ValueKind == JsonValueKind.String
            && DateTime.TryParse(element.GetString(), out var datetime))
        {
            return datetime;
        }

        if (underlyingType == typeof(decimal))
        {
            return element.GetDecimal();
        }

        if (underlyingType == typeof(double))
        {
            return element.GetDouble();
        }

        if (underlyingType == typeof(float))
        {
            return element.GetSingle();
        }

        if (underlyingType == typeof(int))
        {
            return element.GetInt32();
        }

        if (underlyingType == typeof(string))
        {
            return element.GetString();
        }

        return null; // fallback default
    }
}