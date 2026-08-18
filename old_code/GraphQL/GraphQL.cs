using Documate.Data;
using Documate.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using HotChocolate.Data;
using System.Threading.Tasks;
using Google;
using HotChocolate.Resolvers;
using HotChocolate.Language;
using HotChocolate;
using HotChocolate.Types;

namespace Documate.GraphQL
{
    public interface IQuery
    {
    }
    //================================================================
    public class Query: IQuery
    {
        private readonly DBContext dbContext;
        private readonly ILogger<Query> Logger;

        public Query(
            ILogger<Query> logger,
            DBContext context
            )
        {
            Logger = logger;
            dbContext = context;
        }
        //[UsePaging]
        [UseProjection]    // Automatically applies eager loading for requested navigation properties
        [UseFiltering]  // Optional: enables filtering capabilities
        [UseSorting]    // Optional: enables sorting capabilities
        public IQueryable<Document> GetDocs([Service] DBContext context)
        {
            //return dbContext.Documents;
            return context.Documents;
        }

        [UseProjection]    // Automatically applies eager loading for requested navigation properties
        [UseFiltering]  // Optional: enables filtering capabilities
        [UseSorting]    // Optional: enables sorting capabilities
        public IQueryable<Queue> GetQueue([Service] DBContext context)
        {
            //return dbContext.Documents;
            return context.Queues;
        }

        [UseFiltering]
        [UseSorting]
        public IQueryable<Document> GetDocuments()
        {
            return dbContext.Documents.Include(b => b.QueueObj); ;
        }

        // Returns a single document by ID.
        public async Task<Document> GetDocumentById(int id)
        {
            //var a = dbContext.Documents.Include(b => b.QueueObj).Filter(x=> x. id);
//            return a;

            return await dbContext.Documents.FindAsync(id);
        }

        [UseProjection]    // Automatically applies eager loading for requested navigation properties
        [UseFiltering]
        [UseSorting]
        public IQueryable<Document> GetDocs2(IResolverContext context)
        {
            // Start with the base query.
            var query = dbContext.Documents.AsQueryable();

            // Inspect the GraphQL selection set to see if the "queueObj" field was requested.
            //var selections = context.FieldSelection.SelectionSet.Selections.OfType<HotChocolate.Language.FieldNode>();
            var selections = context.Selection.SyntaxNode.SelectionSet.Selections.OfType<FieldNode>();

            bool includeQueue = selections.Any(s => s.Name.Value.Equals("queueObj", System.StringComparison.OrdinalIgnoreCase));
            bool includeStatus = selections.Any(s => s.Name.Value.Equals("sysDocStatusObj", System.StringComparison.OrdinalIgnoreCase));
            //bool includeDocType = false;
            // Optionally, if queueObj is requested, check if its nested field "sysDocTypeObj" is needed.
            if (includeQueue)
            {
                var queueSelection = selections.FirstOrDefault(s => s.Name.Value.Equals("queueObj", System.StringComparison.OrdinalIgnoreCase));
                //if (queueSelection?.SelectionSet?.Selections.OfType<HotChocolate.Language.FieldNode>()
                //        .Any(f => f.Name.Value.Equals("sysDocTypeObj", System.StringComparison.OrdinalIgnoreCase)) == true)
                //{
                //    includeDocType = true;
                //}
            }

            // Conditionally add Include statements.
            if (includeQueue)
            {
                query = query.Include(d => d.QueueObj);
            }
            if (includeStatus)
            {
                query = query.Include(d => d.SysDocStatusObj);
            }
            //if (includeDocType)
            //{
            //    // Assuming QueueObj has a navigation property sysDocTypeObj.
            //    query = query.Include(d => d. QueueObj.sysDocTypeObj);
            //}

            return query;
        }
    }

    //public class DocumentQuery
    //{
    //    private readonly DBContext dbContext;
    //    private readonly ILogger<Query> Logger;

    //    public DocumentQuery(
    //                ILogger<Query> logger,
    //                DBContext context
    //                )
    //    {
    //        Logger = logger;
    //        dbContext = context;
    //    }

       
    //}
}
