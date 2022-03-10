using Ionic.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGMDocstarInterface
{
    static class UploadFile
    {
        public static string[] Execute(ServerConnectionInformation sci, string sourceFile, bool deleteSourceFile)
        {
            if (Path.GetExtension(sourceFile).Equals(".zip", StringComparison.CurrentCultureIgnoreCase))
            {
                var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(dir);
                using (ZipFile zip = ZipFile.Read(sourceFile))
                {
                    zip.ExtractAll(dir, ExtractExistingFileAction.OverwriteSilently);
                }
                var files = Directory.GetFiles(dir);
                for (int i = 0; i < files.Length; i++)
                {
                    files[i] = Upload(sci, files[i]);
                }
                if(deleteSourceFile)
                {
                    File.Delete(sourceFile);
                    Directory.Delete(dir, true);
                }
                return files;
            }
            else
            {
                var fid = Upload(sci, sourceFile);

                if (deleteSourceFile)
                {
                    File.Delete(sourceFile);
                }
                return new[] { fid };
            }

        }
        /// <summary>
        /// This method uploads a file to the eclipse server in chunks as defined by Constants.CHUNKSIZE
        /// The file name on server is controlled by the caller, it should be a unique name, and will be used during a create document call.
        /// Files uploaded are staged in a temporary location until another call is made to move the files to a permanent location (calling create document for example).
        /// </summary>
        public static string Upload(ServerConnectionInformation sci, string localFile)
        {
            var remoteFileName = String.Format("{0}{1}", Guid.NewGuid(), Path.GetExtension(localFile)); //This can be any unique name.

            var url = WebHelper.GetServerUrl(sci, "FileTransfer", "UploadFile", false);
            using (var fileContent = File.OpenRead(localFile))
            {
                var streamLength = fileContent.Length;
                var localHash = streamLength.ToString();
                Byte[] chunk = new byte[Constants.CHUNKSIZE];
                int bytesRead = fileContent.Read(chunk, 0, chunk.Length);
                var append = false;
                while (bytesRead > 0)
                {
                    if (bytesRead < Constants.CHUNKSIZE)
                        Array.Resize(ref chunk, bytesRead);
                    var streamPos = fileContent.Position;
                    var intChunks = chunk.Select(r => (int)r).ToArray(); //Json Endpoint expects the content to be an array of integers.
                    var uploadFilePackage = new { FileName = remoteFileName, Content = intChunks, Last = streamPos >= streamLength, Append = append };
                    var json = JsonConvert.SerializeObject(uploadFilePackage, new JsonSerializerSettings());
                    var respString = WebHelper.ExecutePost(url, json, sci.Token);

                    var resp = (JObject)JsonConvert.DeserializeObject(respString);
                    var error = resp["Error"];
                    if (error.HasValues)
                        throw new Exception(error["Message"].ToString());
                    if (uploadFilePackage.Last && resp["Result"].ToString() != localHash)
                        throw new Exception("Something went wrong, the file size on the server does not match to local file size.");

                    // next chunk
                    bytesRead = fileContent.Read(chunk, 0, chunk.Length); // 0 bytes read means end of stream
                    append = true; //This value should be false for the first chunk of the document sent up and true for all others.
                }
            }

            return remoteFileName;

        }

    }
}
