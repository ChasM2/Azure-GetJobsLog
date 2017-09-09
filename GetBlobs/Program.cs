using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetBlobs
{
    class Program
    {
        static string SetOptions(string message, string defaultValue)
        {
            Console.WriteLine(message);
            var test = Console.ReadLine();
            return string.IsNullOrEmpty(test) ? defaultValue : test;
        }

        static void Main(string[] args)
        {
            String accountName = "testcloudaccount", endPointSuffix = "core.windows.net",
                accountKey = "", 
                containerName = "azure-webjobs-hosts",
                subdir = "output-logs", 
                dateString = "08/15/2017", 
                startTime = "04:30:00",
                hoursCount = "2";
            DateTime startDate, endDate;

            accountName = SetOptions("Set account name (testcloudaccount): ", accountName);
            endPointSuffix = SetOptions("Set endpoint suffix (core.windows.net): ", endPointSuffix);
            accountKey = SetOptions("Set account Key: ", accountKey);
            containerName = SetOptions("Set container name (azure-webjobs-hosts): ", containerName);
            subdir = SetOptions("Set container name (output-logs): ", subdir);
            dateString = SetOptions("Set Date (08/15/2017): ", dateString);
            startTime = SetOptions("Set start Time (04:30:00): ", startTime);
            hoursCount = SetOptions("Set hours count (2): ", hoursCount);

            try
            {
                startDate = DateTime.ParseExact(dateString + " " + startTime, "MM/dd/yyyy hh:mm:ss", CultureInfo.InvariantCulture);
                double result;
                double.TryParse(hoursCount, out result);
                endDate = startDate.AddHours(result);
            }
            catch (Exception exc)
            {
                Console.WriteLine("Date time parse exception: {0}", exc.ToString());
                return;
            }

            var account = new CloudStorageAccount(new StorageCredentials(accountName, accountKey), accountName, endPointSuffix, true);
            var blobClient = account.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(containerName);
            var blobs = container.ListBlobs(prefix: subdir, useFlatBlobListing: true);
            var fileName = String.Format("{0}.zip", startDate.ToString("MM-dd-yyyy"));

            using (var zipToOpen = new FileStream(fileName, FileMode.OpenOrCreate))
            {
                using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                {
                    ZipArchiveEntry readmeEntry = archive.CreateEntry(
                        String.Format("Readme-{0}.txt", DateTime.Now.ToString("yyyyMMddHHmm")));
                    Console.CursorVisible = false;
                    int index = 0;

                    using (var stream = readmeEntry.Open())
                    {
                        stream.Seek(0, SeekOrigin.End);
                        using (var binaryWriter = new StreamWriter(stream))
                        {
                            binaryWriter.WriteLine(string.Format("Account name: {0}", accountName));
                            binaryWriter.WriteLine(string.Format("End Point Suffix: {0}", endPointSuffix));
                            binaryWriter.WriteLine(string.Format("Container name: {0}", containerName));
                            binaryWriter.WriteLine(string.Format("Sub dir: {0}", subdir));
                            binaryWriter.WriteLine(string.Format("Date range: {0} - {1}\n\n", startDate.ToString(), endDate.ToString()));
                        }
                    }

                    foreach (var blob in blobs)
                    {
                        Console.Write(".");

                        var blockBlob = (CloudBlockBlob)blob;
                        var dateBlob = blockBlob.Properties.LastModified.GetValueOrDefault().UtcDateTime;
                        if (dateBlob >= startDate && dateBlob <= endDate)
                        {
                            using (var stream = readmeEntry.Open())
                            {
                                stream.Seek(0, SeekOrigin.End);
                                container.GetBlobReference(blockBlob.Name).DownloadToStream(stream);

                                Console.Write(index++);
                            }
                        }
                    }

                    using (var stream = readmeEntry.Open())
                    {
                        stream.Seek(0, SeekOrigin.End);
                        using (var binaryWriter = new StreamWriter(stream))
                        {
                            binaryWriter.WriteLine(string.Format("\n\ncount - {0}", index.ToString()));
                        }
                    }
                }
            }

            Console.Clear();
            Console.CursorVisible = true;
            Console.WriteLine("Finished");
            Console.ReadLine();
        }
    }
}
