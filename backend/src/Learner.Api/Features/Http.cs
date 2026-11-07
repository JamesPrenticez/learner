namespace Learner.Api.Features;

public static class Http
{
    public static IResult Invalid(string field, string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });

    /// <summary>Re-numbers <paramref name="items"/> to match <paramref name="ids"/>. False unless ids are exactly the items' ids.</summary>
    public static bool ApplyOrder<T>(IEnumerable<T> items, int[] ids) where T : Data.IOrdered
    {
        var list = items.ToList();
        if (!list.Select(i => i.Id).Order().SequenceEqual(ids.Order())) return false;
        foreach (var item in list) item.Order = Array.IndexOf(ids, item.Id);
        return true;
    }
}
