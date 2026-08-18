using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.Runtime.Internal.Util;
using Amazon.Textract;
using Amazon.Textract.Model;

using Microsoft.Extensions.Logging;

namespace Innovoice.Services
{
	public interface ITextractTextAnalysisService
	{
		GetDocumentAnalysisResponse GetJobResults(string jobId);
		bool IsJobComplete(string jobId);
		Task<string> StartDocumentAnalysis(string bucketName, string key, string featureType);
		void WaitForJobCompletion(string jobId, int delay);
		void PrintDebug(GetDocumentAnalysisResponse response);
	}
	
	public class TextractTextAnalysisService : ITextractTextAnalysisService
	{
		private IAmazonTextract textract;
		private ILogger<TextractTextAnalysisService> logger;

		public TextractTextAnalysisService(IAmazonTextract textract, ILogger<TextractTextAnalysisService> logger) {
			this.textract = textract;
		}

		public GetDocumentAnalysisResponse GetJobResults(string jobId) 
		{
			var response = this.textract.GetDocumentAnalysisAsync(new GetDocumentAnalysisRequest {
				JobId = jobId
			});
			response.Wait();
			return response.Result;
		}

		public bool IsJobComplete(string jobId) {
			var response = this.textract.GetDocumentAnalysisAsync(new GetDocumentAnalysisRequest {
				JobId = jobId
			});
			response.Wait();
			return !response.Result.JobStatus.Equals("IN_PROGRESS");
		}

		public async Task<string> StartDocumentAnalysis(string bucketName, string key, string featureType) {
			var request = new StartDocumentAnalysisRequest();
			var s3Object = new S3Object {
				Bucket = bucketName,
				Name = key
			};
			request.DocumentLocation = new DocumentLocation {
				S3Object = s3Object
			};
			request.FeatureTypes = new List<string> { featureType };
			var response = await this.textract.StartDocumentAnalysisAsync(request);
			return response.JobId;
		}

		public void WaitForJobCompletion(string jobId, int delay = 5000) {
			while(!IsJobComplete(jobId)) {
				this.Wait(delay);
			}
		}

		private void Wait(int delay = 5000) {
			Task.Delay(delay).Wait();
			Console.Write(".");
		}
	}
}