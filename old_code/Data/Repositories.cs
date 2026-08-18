using Documate.Domain;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Documate.Data
{
    public interface IAccountRepo : IRepository<Account>
    { }
    public class AccountRepo : GenericRepository<Account>, IAccountRepo
    {
        public AccountRepo(DBContext context) : base(context) { }
    }

    public interface IUserRepo : IRepository<User>
    { }
    public class UserRepo : GenericRepository<User>, IUserRepo
    {
        public UserRepo(DBContext context) : base(context) { }
    }

    public interface IAuthTokenRepo : IRepository<AuthToken>
    { }
    public class AuthTokenRepo : GenericRepository<AuthToken>, IAuthTokenRepo
    {
        public AuthTokenRepo(DBContext context) : base(context) { }
    }

    public interface IQueueRepo : IRepository<Queue>
    { }
    public class QueueRepo : GenericRepository<Queue>, IQueueRepo
    {
        public QueueRepo(DBContext context) : base(context) { }
    }

    public interface IUserQueueRepo : IRepository<UserQueue>
    { }
    public class UserQueueRepo : GenericRepository<UserQueue>, IUserQueueRepo
    {
        public UserQueueRepo(DBContext context) : base(context) { }
    }

    public interface ISysDocStatusRepo : IRepository<SysDocStatus>
    { }
    public class SysDocStatusRepo : GenericRepository<SysDocStatus>, ISysDocStatusRepo
    {
        public SysDocStatusRepo(DBContext context) : base(context) { }
    }

    public interface INanoModelRepo : IRepository<NanoModel>
    { }
    public class NanoModelRepo : GenericRepository<NanoModel>, INanoModelRepo
    {
        public NanoModelRepo(DBContext context) : base(context) { }
    }

    public interface IDocStorageRepo : IRepository<DocStorage>
    { }
    public class DocStorageRepo : GenericRepository<DocStorage>, IDocStorageRepo
    {
        public DocStorageRepo(DBContext context) : base(context) { }
    }
    
    public interface IDocumentRepo : IRepository<Document>
    { }
    public class DocumentRepo : GenericRepository<Document>, IDocumentRepo
    {
        public DocumentRepo(DBContext context) : base(context) { }
    }

    public interface IDocImageRepo : IRepository<DocImage>
    { }
    public class DocImageRepo : GenericRepository<DocImage>, IDocImageRepo
    {
        public DocImageRepo(DBContext context) : base(context) { }
    }

    public interface IStatusHistoryRepo : IRepository<StatusHistory>
    { }
    public class StatusHistoryRepo : GenericRepository<StatusHistory>, IStatusHistoryRepo
    {
        public StatusHistoryRepo(DBContext context) : base(context) { }
    }

    public interface ITemplateRepo : IRepository<Template>
    { }
    public class TemplateRepo : GenericRepository<Template>, ITemplateRepo
    {
        public TemplateRepo(DBContext context) : base(context) { }
    }

    public interface ITemplateQueueRepo : IRepository<TemplateQueue>
    { }
    public class TemplateQueueRepo : GenericRepository<TemplateQueue>, ITemplateQueueRepo
    {
        public TemplateQueueRepo(DBContext context) : base(context) { }
    }

    public interface ITemplateKeywordRepo : IRepository<TemplateKeyword>
    { }
    public class TemplateKeywordRepo : GenericRepository<TemplateKeyword>, ITemplateKeywordRepo
    {
        public TemplateKeywordRepo(DBContext context) : base(context) { }
    }

    public interface IMasterKeywordSetRepo : IRepository<MasterKeywordSet>
    { }
    public class MasterKeywordSetRepo : GenericRepository<MasterKeywordSet>, IMasterKeywordSetRepo
    {
        public MasterKeywordSetRepo(DBContext context) : base(context) { }
    }

    public interface IKeywordSynonymRepo : IRepository<KeywordSynonym>
    { }
    public class KeywordSynonymRepo : GenericRepository<KeywordSynonym>, IKeywordSynonymRepo
    {
        public KeywordSynonymRepo(DBContext context) : base(context) { }
    }

    public interface IIdentifyingElementRepo : IRepository<IdentifyingElement>
    { }
    public class IdentifyingElementRepo : GenericRepository<IdentifyingElement>, IIdentifyingElementRepo
    {
        public IdentifyingElementRepo(DBContext context) : base(context) { }
    }

    public interface IKeywordElementRepo : IRepository<KeywordElement>
    { }
    public class KeywordElementRepo : GenericRepository<KeywordElement>, IKeywordElementRepo
    {
        public KeywordElementRepo(DBContext context) : base(context) { }
    }

    public interface ICreditPurchaseRepo : IRepository<CreditPurchase>
    { }
    public class CreditPurchaseRepo : GenericRepository<CreditPurchase>, ICreditPurchaseRepo
    {
        public CreditPurchaseRepo(DBContext context) : base(context) { }
    }

    public interface ICustomerInvoiceRepo : IRepository<CustomerInvoice>
    { }
    public class CustomerInvoiceRepo : GenericRepository<CustomerInvoice>, ICustomerInvoiceRepo
    {
        public CustomerInvoiceRepo(DBContext context) : base(context) { }
    }

    public interface IInvoiceDocRepo : IRepository<InvoiceDoc>
    { }
    public class InvoiceDocRepo : GenericRepository<InvoiceDoc>, IInvoiceDocRepo
    {
        public InvoiceDocRepo(DBContext context) : base(context) { }
    }


}
