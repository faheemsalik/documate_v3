using Documate.Data;
using Documate.Domain;
using Documate.Extensions;
using Documate.Models;
using Documate.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Documate.Services
{
    public interface INanoModelService
    {
        List<NanoModel> GetModel(Expression<Func<NanoModel, bool>> where = null);
        Task<NanoModel> GetEntityById(int id);
    }
    //================================================================

    public class NanoModelService : INanoModelService
    {
        private readonly DBContext dbContext;
        private readonly ILogger<AccountService> Logger;
        private readonly INanoModelRepo nanoModelRepo;

        public NanoModelService(
            ILogger<AccountService> logger,
            DBContext context,
            INanoModelRepo nanoModelRepo
            )
        {
            Logger = logger;
            dbContext = context;
            this.nanoModelRepo = nanoModelRepo;
        }


        public List<NanoModel> GetModel(Expression<Func<NanoModel, bool>> where = null)
        {
            var query = from nanoModel in nanoModelRepo.Table
                        select new NanoModel
                        {
                            Id = nanoModel.Id,
                            NanoModelId = nanoModel.NanoModelId,
                            ModelKey = nanoModel.ModelKey
                        };
            query = query.Where(where);
            var a = query.ToList();
            return a;
        }

        public async Task<NanoModel> GetEntityById(int id)
        {
            NanoModel returnValue = await nanoModelRepo.GetEntityById(id);
            return returnValue;
        }
    }

}
