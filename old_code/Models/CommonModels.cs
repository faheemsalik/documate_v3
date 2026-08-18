using System;
using System.Collections.Generic;
using System.IO;

namespace Documate.Common.Models
{
    /// <summary>
    /// Response object for all the methods. Provide <typeparamref filename="T"/> from following list:
    ///     <para><c> InnovoiceDocListInfo</c> : while fetching documents list</para>
    ///     <para><c> InnovoiceDocument</c> : while fetching single documents</para>
    ///     InnovoiceQueueUpdate : Gives you response object of Queue update
    /// </summary>
    /// <typeparam filename="T">The element type of the array</typeparam>
    ///  <example>
    /// This sample shows how to call the <see cref="GetZero"/> method.
    /// <code>
    /// InnovoiceResponse<InnovoiceDocListInfo> docList;
    /// </code>
    /// </example>
    public class DocumateResponse<T>
    {
        public bool IsSucessfull { get; set; }
        public string Message { get; set; }
        public T Result { get; set; }
        public T[] Results { get; set; }
    }

    public class DocumateResponse : DocumateResponse<DocumateResponse>
    {
    }
    public class DocumateDocsListResponse : DocumateResponse<DocumateDocListInfo>
    {
    }

    public class DocumateDocumentResponse : DocumateResponse<DocumateDocument>
    {
    }

    public class DocumateDocListInfo
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public DocumateDocStatus Status { get; set; }
        public int QueueId { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime? UpdatedOnUtc { get; set; }
        public int page { get; set; }
        public string ProcessingRemarks { get; set; }
    }

    public class DocumateDocument
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string Description { get; set; }
        public string ProcessingRemarks { get; set; }
        public string FailedException { get; set; }
        public DocumateDocStatus Status { get; set; }
        public int QueueId { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime? UpdatedOnUtc { get; set; }
        public string ProcessedDataJSON { get; set; }
        public string UserMetaData { get; set; }

        //public int QueueId { 
        //    get { return UserQueueId; }
        //    set { QueueId = value; } 
    }

    public class UploadDocReponse : DocumateResponse<DocumateDocument>
    {
        public UploadDocReponse()
        {
            Result = new DocumateDocument();
        }
    }

    public enum DocumateDocStatus
    {
        //NOT_SET = 0,
        IMPORTING = 1, //Document is being processed by the AI Core Engine for data extraction; initial state of the document.
        //FAILED_IMPORT = 2,  //Import failed e.g.due to a malformed document file.
        TO_REVIEW = 3, //Initial extraction step is done and the document is waiting for user validation.
        //REVIEWING = 4, //Document is undergoing validation in the user interface.
        EXPORTING = 5, //Document is validated and is now awaiting the completion of connector save call.See connector extension for more information on this status.
        EXPORTED = 6, //Document is validated and successfully passed all hooks; this is the typical terminal state of a document.
        FAILED_EXPORT = 7, // When the connector returned an error.
        DELETED = 8, //When the document was deleted by the user.
    }

    public class UploadDocModel
    {
        public string FileName { get; set; }
        public int QueueId { get; set; }
        public byte[] FileBytes { get; set; }
        public string FileBase64 { get; set; }
        public string UserMetaData { get; set; }
    }


}
