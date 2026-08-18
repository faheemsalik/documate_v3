using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Documate.Domain;
using Documate.Models;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Documate.Services
{
    public interface IS3Service
    {
        Task UploadAsync(S3FileModel uploadModel);
        Task<bool> DeleteFile(S3FileModel s3File);
        Task<MemoryStream> DownloadFile(S3FileModel s3File);
        Task<string> GetSignedUrl(S3FileModel s3File);
    }

    public class S3Service : IS3Service
    {
        private readonly ILogger<S3Service> Logger;
        private readonly IAmazonS3 s3Client;

        public S3Service(ILogger<S3Service> logger, IAmazonS3 s3Client)
        {
            Logger = logger;
            this.s3Client = s3Client;
        }

        public async Task UploadAsync(S3FileModel uploadModel)
        {
            var fileTransferUtility = new TransferUtility(s3Client);
            var a = fileTransferUtility.S3Client.Config.RegionEndpoint.ToString();
            //using (var fileToUpload =new FileStream(filePath, FileMode.Open, FileAccess.Read))
            //{
            //    await fileTransferUtility.UploadAsync(fileToUpload,
            //                               bucketName, keyName);
            //}
            var fileTransferUtilityRequest = new TransferUtilityUploadRequest
            {
                BucketName = uploadModel.BucketName,
                InputStream = uploadModel.FileMemoryStream,
                Key = uploadModel.FileName, // Custom filename if want to give
                StorageClass = S3StorageClass.IntelligentTiering
                //FilePath = uploadModel.FilePath // use direct file read instead of memory stream if required
                //PartSize = 6291456, // 6 MB.
                //CannedACL = S3CannedACL.PublicRead  // set file security
            };
            fileTransferUtilityRequest.Metadata.Add("QueueId", uploadModel.QueueId.ToString());
            await fileTransferUtility.UploadAsync(fileTransferUtilityRequest);
            Console.WriteLine("Upload 4 completed");
            return;
        }

        public void ScheduledS3Cleanup()  // Cleanup the bucket after some time and free up the space.
        { }

        public async Task<string> GetSignedUrl(S3FileModel s3File)
        {
            try
            {
                //await CORSConfigTestAsync(s3File.BucketName);
                GetPreSignedUrlRequest request1 = new GetPreSignedUrlRequest()
                {
                    BucketName = s3File.BucketName,
                    Key = s3File.FileName,
                    Expires = DateTime.Now.AddMinutes(30)
                };
                string url = s3Client.GetPreSignedURL(request1);
                return url;
            }
            catch (Exception)
            {
                Logger.LogError("Error getting S3 file URL");
            }
            return "";
        }

        public async Task<bool> DeleteFile(S3FileModel s3File)
        {
            bool returnValue = false;
            try
            {
                var deleteObjectRequest = new DeleteObjectRequest
                {
                    BucketName = s3File.BucketName + s3File.FilePath,
                    Key = s3File.FileName
                };

                Logger.LogDebug("Deleting an object");
                var response = await s3Client.DeleteObjectAsync(deleteObjectRequest);
                if (response.HttpStatusCode == HttpStatusCode.OK)
                    returnValue = true;
                Logger.LogError($"S3 file: {s3File.FileName} deleted");
            }
            catch (Exception)
            {
                Logger.LogError("Error deleting S3 file");
            }
            return returnValue;
        }

        public async Task<MemoryStream> DownloadFile(S3FileModel s3File)
        {
            MemoryStream returnValue = new MemoryStream();
            try
            {
                var getObjectRequest = new GetObjectRequest
                {
                    BucketName = s3File.BucketName + s3File.FilePath,
                    Key = s3File.FileName,
                };

                Logger.LogDebug("Downloading an object");
                var response = await s3Client.GetObjectAsync(getObjectRequest);
                if (response.HttpStatusCode == HttpStatusCode.OK)
                {
                    response.ResponseStream.CopyTo(returnValue);
                    //using (StreamReader reader = new StreamReader(response.ResponseStream))
                    //{
                    //    string contents = reader.ReadToEndAsync().Result;
                    //    returnValue = Encoding.UTF8.GetBytes(contents ?? "");
                    //}
                }
                Logger.LogError($"S3 file: {s3File.FileName} downloaded");
            }
            catch (Exception)
            {
                Logger.LogError("Error deleting S3 file");
            }
            return returnValue;
        }

        private IAmazonS3 GetS3Client()
        {
            RegionEndpoint bucketRegion = RegionEndpoint.USWest2;
            IAmazonS3 s3Client = new AmazonS3Client(bucketRegion);
            return s3Client;
        }

        private async Task CORSConfigTestAsync(string bucketName)
        {
            try
            {
                CORSConfiguration configuration = new CORSConfiguration
                {
                    Rules = new List<CORSRule>
                        {
                          new CORSRule
                          {
                            Id = "GetLocal",
                            AllowedMethods = new List<string> {"GET", "HEAD"}, //"PUT", "POST", "DELETE"
                            AllowedOrigins = new List<string> {"*","http://localhost:4501", "http://localhost:4500", "http://3.11.192.72:8081/"},
                            MaxAgeSeconds = 4000,
                            AllowedHeaders= new List<string> {"*", "Access-Control-Allow-Origin", "Authorization"},
                            ExposeHeaders= new List<string> { "x-amz-server-side-encryption", "x-amz-request-id", "x-amz-id-2" }
                          }
                        }
                };
                await PutCORSConfigurationAsync(configuration, bucketName);
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine("Error encountered on server. Message:'{0}' when writing an object", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
            }

        }
        private async Task PutCORSConfigurationAsync(CORSConfiguration configuration, string bucketName)
        {

            PutCORSConfigurationRequest request = new PutCORSConfigurationRequest
            {
                BucketName = bucketName,
                Configuration = configuration
            };

            var response = await s3Client.PutCORSConfigurationAsync(request);
        }
    }
}

#region file download code
/*
        public async Task<S3FileModel> DownloadFile(S3FileModel s3File)
        {
            GetObjectRequest req = new GetObjectRequest();
            req.Key = s3File.FileName;
            req.BucketName = s3File.BucketName;
            FileInfo fi = new FileInfo(s3File.FileName);
            string ext = fi.Extension.ToLower();
            string mimeType = ReturnmimeType(ext);
            GetObjectResponse res = await s3Client.GetObjectAsync(req);
            s3File.FileMemoryStream = res.ResponseStream;
            Stream response = responseStream;
            return File(response, mimeType, downLoadName);
            return true;
        } 

return new FileStreamResultEx(response, res.ContentLength, mimeType, downloadName);


public class FileStreamResultEx : ActionResult{

     public FileStreamResultEx(
        Stream stream, 
        long contentLength,         
        string mimeType,
        string fileName){
        this.stream = stream;
        this.mimeType = mimeType;
        this.fileName = fileName;
        this.contentLength = contentLength;
     }


     public override void ExecuteResult(
         ControllerContext context)
     {
         var response = context.HttpContext.Response; 
         response.BufferOutput = false;
         response.Headers.Add("Content-Type", mimeType);
         response.Headers.Add("Content-Length", contentLength.ToString());
         response.Headers.Add("Content-Disposition","attachment; filename=" + fileName);

         using(stream) { 
             stream.CopyTo(response.OutputStream);
         }
     }

}
 */
#endregion