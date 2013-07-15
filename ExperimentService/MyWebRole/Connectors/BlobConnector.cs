using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace MyWebRole.Connectors
{
    public class BlobConnector
    {
        private CloudBlobContainer blobContainer;
        public BlobConnector(string containerName)
        {
            Initialize(containerName);
        }

        public BlobConnector(string containerName, string connectionString)
        {
            Initialize(containerName, connectionString);
        }

        private void Initialize(string containerName)
        {
            string connectionString = CloudConfigurationManager.GetSetting("Microsoft.TableStorage.ConnectionString");
            CloudStorageAccount blobStorageAccount = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient blobClient = blobStorageAccount.CreateCloudBlobClient();
            blobContainer = blobClient.GetContainerReference(containerName);
            blobContainer.CreateIfNotExists();
            blobContainer.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
        }

        private void Initialize(string containerName, string connectionString)
        {
            CloudStorageAccount blobStorageAccount = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient blobClient = blobStorageAccount.CreateCloudBlobClient();
            blobContainer = blobClient.GetContainerReference(containerName);
            blobContainer.CreateIfNotExists();
            blobContainer.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
        }

        public string UploadFile(string file)
        {
            string fileName = file.Split('\\').Last();
            CloudBlockBlob blockBlob = blobContainer.GetBlockBlobReference(fileName);

            using (var fileStream = System.IO.File.OpenRead(file))
            {
                blockBlob.UploadFromStream(fileStream);
            }
            return blockBlob.Uri.ToString();
        }

        public string UploadFile(HttpPostedFileBase file)
        {
            CloudBlockBlob blockBlob = blobContainer.GetBlockBlobReference(file.FileName);
            blockBlob.Properties.ContentType = file.ContentType;
            file.InputStream.Position = 0;
            using (var fileStream = new MemoryStream())
            {
                file.InputStream.CopyTo(fileStream);
                fileStream.Position = 0;
                blockBlob.UploadFromStream(fileStream);
            }
            return blockBlob.Uri.ToString();
        }


        public string DownloadFile(string file)
        {
            CloudBlockBlob blob = blobContainer.GetBlockBlobReference(file);

            using (var fileStream = System.IO.File.OpenWrite(file))
            {
                blob.DownloadToStream(fileStream);
            }
            return file;
        }

        public bool BlobExist(string file)
        {
            string fileName = file + DateTime.UtcNow.ToFileTimeUtc().ToString();
            try
            {


                CloudBlockBlob blob = blobContainer.GetBlockBlobReference(file);
                using (var fileStream = System.IO.File.OpenWrite(fileName))
                {
                    blob.DownloadToStream(fileStream);
                }
                File.Delete(fileName);
            }
            catch (StorageException)
            {
                File.Delete(fileName);
                return false;
            }

            return true;
        }

    }
}