using NUnit.Framework;
using Raven.Client.Documents.Indexes;
using Raven.TestDriver;

namespace RavenBetweenReproDotnet8;

public class Model
{
    public string Id { get; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
}

public class ModelIndex : AbstractIndexCreationTask<Model>
{
    public ModelIndex()
    {
        Map = docs => from doc in docs select new { doc.CreatedAt };
    }
}

public class Tests : RavenTestDriver
{
    static readonly DateTimeOffset CreatedAt = new(2025, 01, 21, 09, 00, 00, TimeSpan.Zero);
    public static readonly object[][] DateTimeData =
    [
        [new DateTimeOffset(2025, 01, 21, 00, 00, 00, TimeSpan.Zero)],
        [new DateTimeOffset(2025, 01, 20, 00, 00, 00, TimeSpan.Zero)],
    ];

    [Test]
    [TestCaseSource(nameof(DateTimeData))]
    public async Task DateTimeOffsetTests(DateTimeOffset from)
    {
        // Arrange
        using var store = GetDocumentStore();
        await store.ExecuteIndexAsync(new ModelIndex());
        using (var session = store.OpenAsyncSession())
        {
            await session.StoreAsync(new Model { CreatedAt = CreatedAt });

            await session.SaveChangesAsync();

            WaitForIndexing(store);
        }

        // Act
        List<Model> results = [];
        DateTimeOffset? to = null;
        using (var session = store.OpenAsyncSession())
        {
            var query = session.Advanced.AsyncDocumentQuery<Model, ModelIndex>();
            query.WhereBetween(x => x.CreatedAt, from, to);

            results.AddRange(await query.ToListAsync());
        }

        WaitForUserToContinueTheTest(store);

        // Assert
        Assert.That(results.Count == 1);
    }
}
