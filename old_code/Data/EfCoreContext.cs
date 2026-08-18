using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Documate.Domain;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;


namespace Documate.Data
{

    public class DBContext : DbContext
    {
        public DBContext(DbContextOptions<DBContext> options) : base(options)
        {
        }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<AuthToken> AuthTokens { get; set; }
        
        public DbSet<DocStorage> DocStorages { get; set; }
        public DbSet<NanoModel> NanoModels { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocImage> DocImages { get; set; }
        public DbSet<SysDocStatus> SysDocStatuses { get; set; }
        public DbSet<StatusHistory> StatusHistories { get; set; }

        public DbSet<Queue> Queues { get; set; }
        public DbSet<UserQueue> UserQueues { get; set; }
        //public DbSet<SysDocType> SysDocTypes { get; set; }
        //public DbSet<SchemaSection> SchemaSections { get; set; }
        //public DbSet<SchemaDataPoint> SchemaDataPoints { get; set; }
        //public DbSet<SchemaNodeCategory> SchemaNodeCategories { get; set; }

        public DbSet<Template> Templates { get; set; }
        public DbSet<TemplateQueue> TemplateQueues { get; set; }
        public DbSet<TemplateKeyword> TemplateKeywords { get; set; }
        public DbSet<MasterKeywordSet> MasterKeywordSets { get; set; }
        public DbSet<KeywordSynonym> KeywordSynonyms { get; set; }
        
        //public DbSet<SchemaKeyword> QueueKeywords { get; set; }

        public DbSet<IdentifyingElement> IdentifyingElements { get; set; }
        public DbSet<KeywordElement> KeywordElements { get; set; }

        public DbSet<CreditPurchase> CreditPurchase { get; set; }
        public DbSet<CustomerInvoice> CustomerInvoice { get; set; }
        public DbSet<InvoiceDoc> InvoiceDoc { get; set; }
        public DbSet<SysSetting> SysSetting { get; set; }
        



        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //optionsBuilder.UseSqlServer(@"Server=(local)\\sqlexpress;Database=Innovoice;MultipleActiveResultSets=true;User ID=sa;Password=a;");
        }
        //Use this code if we want to use difference tables filename for an entity set (DbSet)
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                // equivalent of modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
                modelBuilder.Entity(entityType.ClrType).ToTable(entityType.ClrType.Name);

                // equivalent of modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
                // and modelBuilder.Conventions.Remove<ManyToManyCascadeDeleteConvention>();
                entityType.GetForeignKeys()
                    .Where(fk => !fk.IsOwnership && fk.DeleteBehavior == DeleteBehavior.Cascade)
                    .ToList()
                    .ForEach(fk => fk.DeleteBehavior = DeleteBehavior.Restrict);
            }
            //modelBuilder.Entity<Tenant>().ToTable("Tenant");
        }
    }
}


