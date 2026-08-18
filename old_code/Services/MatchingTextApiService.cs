using Documate.Data;
using Documate.Domain;
using Documate.Extensions;
using Documate.Models;
using Microsoft.Extensions.Logging;

using RestSharp;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;

namespace Documate.Services
{
    public interface IMatchingTextApiService
    {
        ResponseModel SymanticMachData(SymanticMatchDataInput list);
    }
    //================================================================

    public class MatchingTextApiService : IMatchingTextApiService
    {
        public ResponseModel SymanticMachData(SymanticMatchDataInput data)
        {
            ResponseModel returnValue = new ResponseModel();
            List<OutputItem> workList = new List<OutputItem>();
            List<OutputItem> returnList = new List<OutputItem>();
            // Do the process here
            foreach (SymanticMatchDataItem sourceItem in data.SourceList)
            {
                double similarity = 0.00;
                foreach (SymanticMatchDataItem targetItem in data.TargetList)
                {
                    similarity = Extensions.Helper.CalculateSimilarity(sourceItem.Text, targetItem.Text);
                    workList.Add(
                        new OutputItem
                        {
                            SourceId = sourceItem.Id,
                            SourceText = sourceItem.Text,
                            TargetId = targetItem.Id,
                            TargetText = targetItem.Text,
                            Score = similarity
                        });
                }
            }
            foreach (var item in data.SourceList)
            {
                OutputItem max = workList.Where(x => x.SourceId == item.Id).OrderByDescending(x => x.Score).FirstOrDefault();
                returnList.Add(max);
            }
            returnValue.IsSucessfull = true;
            returnValue.Result = returnList;
            return returnValue;
        }

    }
    public class OutputItem
    {
        public int SourceId { get; set; }
        public string SourceText { get; set; }
        public int TargetId { get; set; }
        public string TargetText { get; set; }
        public double Score { get; set; }
    }

    public class SymanticMatchDataInput
    {
        public List<SymanticMatchDataItem> SourceList { get; set; }
        public List<SymanticMatchDataItem> TargetList { get; set; }
    }

    public class SymanticMatchDataItem
    {
        public int Id { get; set; }
        public string Text { get; set; }
    }

    //public class SymanticMatchOutput
    //{
    //    public SymanticMatchOutput()
    //    {
    //        SourceList = new List<SymanticMatchDataItem>();
    //        TargetList = new List<SymanticMatchDataItem>();
    //    }
    //    public List<SymanticMatchDataItem> SourceList { get; set; }
    //    public List<SymanticMatchDataItem> TargetList { get; set; }
    //    public double Score { get; set; }
    //}
}
