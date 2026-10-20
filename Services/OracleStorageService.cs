using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GroupProject.Services
{
    public class OracleStorageService : IStorageService
    {
        //load the oracle config from the storage service - TL
        private readonly IConfiguration _config;

        public OracleStorageService(IConfiguration config)
        {
            _config = config;
        }

        // creates new S3 Client to handle cloud operations
        private IAmazonS3 GetS3Client()
        {
            //sets credentials for the object from the storage service
            var accessKey = _config["OracleStorage:AccessKey"];
            var secretKey = _config["OracleStorage:SecretKey"];
            var region = _config["OracleStorage:Region"];
            var namespaceName = _config["OracleStorage:Namespace"];

            //setting up url - TL
            var s3Config = new AmazonS3Config
            {
                ServiceURL = $"https://{namespaceName}.compat.objectstorage.{region}.oraclecloud.com",
                ForcePathStyle = true,
                //Fixes to ensure that image uploads properly, as we are using oracle object storage instead of amazon - TL
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED 
            };

            return new AmazonS3Client(accessKey, secretKey, s3Config);
        }

        //function to upload the image to object storage - TL
        public async Task<string> UploadFileAsync(IFormFile file)
        {
            //if file is null or empty - TL
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is empty or null.");
            }

            //create new variable using functino above - TL
            using var client = GetS3Client();

            //file name for the file, this can be changed later and is just temporary - TL
            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            //getting appropriate values from config - TL
            var bucketName = _config["OracleStorage:BucketName"];
            var namespaceName = _config["OracleStorage:Namespace"];
            var region = _config["OracleStorage:Region"];

            //creates new MemoryStream to load the iamge
            using var memoryStream = new MemoryStream();
            //copy file to MemoryStream - TL
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            //creates new request object to send to oracle - TK
            var request = new PutObjectRequest
            {
                InputStream = memoryStream,
                Key = uniqueFileName,
                BucketName = bucketName,
                CannedACL = S3CannedACL.PublicRead,
                UseChunkEncoding = false,
                DisablePayloadSigning = true
            };

            //upload image - TL
            await client.PutObjectAsync(request);

            //returns user to a page with the image displayed - TL
            return $"https://objectstorage.{region}.oraclecloud.com/n/{namespaceName}/b/{bucketName}/o/{uniqueFileName}";
        }
    }
}