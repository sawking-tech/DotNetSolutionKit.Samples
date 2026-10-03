using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using ST.DotNetSolutionKit.Samples.Common.Domain.Specifications;
using LinqSpecs;

namespace ST.DotNetSolutionKit.Samples.Common.Tests.Stubs;

/// <summary>
/// In-memory stub for ICaseInsensitiveSearch.
/// Replicates PostgresCaseInsensitiveSearch behavior: PreparePattern + ILIKE-to-Regex conversion.
/// </summary>
public sealed class InMemoryCaseInsensitiveSearch : ICaseInsensitiveSearch
{
    private static readonly MethodInfo IsMatchMethod =
        typeof(Regex).GetMethod(
            nameof(Regex.IsMatch),
            BindingFlags.Static | BindingFlags.Public,
            null,
            [typeof(string), typeof(string), typeof(RegexOptions)],
            null)!;

    public Specification<T> GetSpecification<T>(Expression<Func<T, string>> propertyExpression, string? pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return new AdHocSpecification<T>(_ => true);

        var regexPattern = ILikeToRegex(PreparePattern(pattern));
        var parameter = propertyExpression.Parameters[0];

        var call = Expression.Call(IsMatchMethod,
            propertyExpression.Body,
            Expression.Constant(regexPattern),
            Expression.Constant(RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));

        return new AdHocSpecification<T>(Expression.Lambda<Func<T, bool>>(call, parameter));
    }

    public Specification<T> GetArraySpecification<T>(Expression<Func<T, string[]>> arrayPropertyExpression, string? pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return new AdHocSpecification<T>(_ => true);

        var regexPattern = ILikeToRegex(PreparePattern(pattern));
        var parameter = arrayPropertyExpression.Parameters[0];

        var itemParam = Expression.Parameter(typeof(string), "s");
        var itemCall = Expression.Call(IsMatchMethod,
            itemParam,
            Expression.Constant(regexPattern),
            Expression.Constant(RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));
        var itemLambda = Expression.Lambda<Func<string, bool>>(itemCall, itemParam);

        var anyMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.Any) && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(string));

        var anyCall = Expression.Call(null, anyMethod, arrayPropertyExpression.Body, itemLambda);
        return new AdHocSpecification<T>(Expression.Lambda<Func<T, bool>>(anyCall, parameter));
    }

    /// <summary>
    /// Mirrors PostgresCaseInsensitiveSearch.EscapeAndWrapPattern:
    /// escapes '/', '%', '_' using '/' as escape char, wraps with %.
    /// </summary>
    private static string PreparePattern(string term)
        => $"%{term.Trim().Replace("/", "//").Replace("%", "/%").Replace("_", "/_")}%";

    /// <summary>
    /// Converts a PostgreSQL ILIKE pattern (with '/' as escape char) to a .NET Regex pattern.
    /// '/' followed by next char → literal char (Regex.Escaped)
    /// '%' → .*
    /// '_' → .
    /// other → Regex.Escape(char)
    /// </summary>
    private static string ILikeToRegex(string iLikePattern)
    {
        var sb = new StringBuilder("^");
        var i = 0;
        while (i < iLikePattern.Length)
        {
            var c = iLikePattern[i];
            if (c == '/' && i + 1 < iLikePattern.Length)
            {
                sb.Append(Regex.Escape(iLikePattern[i + 1].ToString()));
                i += 2;
            }
            else if (c == '%')
            {
                sb.Append(".*");
                i++;
            }
            else if (c == '_')
            {
                sb.Append('.');
                i++;
            }
            else
            {
                sb.Append(Regex.Escape(c.ToString()));
                i++;
            }
        }
        sb.Append('$');
        return sb.ToString();
    }
}
