using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AGMDocstarInterface
{
    public class FormUpload
    {
        private static readonly Encoding encoding = Encoding.UTF8;

        /// <summary>
        /// Non-standard way of uploading and creating a document.
        /// Uses the GeneralUpload.ashx handler via the FILEIMPORT upload type
        /// </summary>
        public static string UploadAndCreate(ServerConnectionInformation sci, string path)
        {
            if (String.IsNullOrWhiteSpace(sci.ServerUrl))
                WebHelper.GetUrlsFromWebServerData(sci);

            var cts = CollectionGets.ContentTypes(sci);
            var url = $"{sci.ServerUrl}GeneralUpload.ashx?uploadType=FILEIMPORT";
            var additionalData = new
            {
                ContentTypeId = cts.First().Value, //Required, can be hardcoded.
                //Title = "Hey this is my document", //Optional
                //InboxId = "10408960-F602-48A9-9D40-8CF38EA11E8C", //Optional
                //WorkflowId = "20408960-F602-48A9-9D40-8CF38EA11E8C", //Optional
                //Keywords = "FindMe", //Optional
                //SecurityClassId = "30408960-F602-48A9-9D40-8CF38EA11E8C", //Optional
                //FolderId = "40408960-F602-48A9-9D40-8CF38EA11E8C", //Optional
                //IsDraft = false, //Optional
                //CustomFieldValues = new[] //Optional
                //{
                //    new
                //    {
                //        CustomFieldMetaId = "40408960-F602-48A9-9D40-8CF38EA11E8C",
                //        CustomFieldName = "InvoiceNumber",
                //        TypeCode = 18, //See CFTypeCode below.
                //        StringValue = "Inv12345", //Depending on type fill in BoolValue, DateTimeValue, DateValue, DecimalValue, IntValue, LongValue
                //    }
                //}
            };
            var paramsDict = new Dictionary<string, object>();
            paramsDict.Add("file", new FormUpload.FileParameter(File.ReadAllBytes(path), Path.GetFileName(path)));
            paramsDict.Add("additionalData", JsonConvert.SerializeObject(additionalData));
            using (var webResponse = FormUpload.MultipartFormDataPost(url, "TheBestAgentOutThere", paramsDict, sci.Token))
            {
                using (StreamReader responseReader = new StreamReader(webResponse.GetResponseStream()))
                {
                    var fullResponse = responseReader.ReadToEnd();
                    string pattern = @"<script id=""postResult"" type=""application\/json"">(.*)<\/script>"; //Pull out the JSON from the HTML
                    RegexOptions options = RegexOptions.Multiline;
                    var match = Regex.Match(fullResponse, pattern, options);
                    if (!match.Success || match.Groups.Count < 2)
                        throw new Exception("No result found");

                    var json = match.Groups[1].Value; //Pull out the (.*) part (IE the part between the script tag).

                    var resp = (JObject)JsonConvert.DeserializeObject(json);
                    var error = resp["Error"];
                    if (error.HasValues)
                        throw new Exception(error["Message"].ToString());

                    return resp["Result"]["Document"]["Id"].ToString();
                }
            }
        }
        private static HttpWebResponse MultipartFormDataPost(string postUrl, string userAgent, Dictionary<string, object> postParameters, string token)
        {
            string formDataBoundary = String.Format("----------{0:N}", Guid.NewGuid());
            string contentType = "multipart/form-data; boundary=" + formDataBoundary;

            byte[] formData = GetMultipartFormData(postParameters, formDataBoundary);

            return PostForm(postUrl, userAgent, contentType, token, formData);
        }
        private static HttpWebResponse PostForm(string postUrl, string userAgent, string contentType, string token, byte[] formData)
        {
            HttpWebRequest request = WebRequest.Create(postUrl) as HttpWebRequest;

            if (request == null)
            {
                throw new NullReferenceException("request is not a http request");
            }

            // Set up the request properties.
            request.Method = "POST";
            request.ContentType = contentType;
            request.UserAgent = userAgent;
            request.CookieContainer = new CookieContainer();
            request.ContentLength = formData.Length;

            if (!String.IsNullOrEmpty(token))
            {
                request.Headers.Add(Constants.TOKENHEADER, token);
            }
            // Send the form data to the request.
            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(formData, 0, formData.Length);
                requestStream.Close();
            }

            return request.GetResponse() as HttpWebResponse;
        }
        private static byte[] GetMultipartFormData(Dictionary<string, object> postParameters, string boundary)
        {
            Stream formDataStream = new System.IO.MemoryStream();
            bool needsCLRF = false;

            foreach (var param in postParameters)
            {
                // Add a CRLF to allow multiple parameters to be added.
                // Skip it on the first parameter, add it to subsequent parameters.
                if (needsCLRF)
                    formDataStream.Write(encoding.GetBytes("\r\n"), 0, encoding.GetByteCount("\r\n"));

                needsCLRF = true;

                if (param.Value is FileParameter)
                {
                    FileParameter fileToUpload = (FileParameter)param.Value;

                    // Add just the first part of this param, since we will write the file data directly to the Stream
                    string header = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\nContent-Type: {3}\r\n\r\n",
                        boundary,
                        param.Key,
                        fileToUpload.FileName ?? param.Key,
                        fileToUpload.ContentType ?? "application/octet-stream");

                    formDataStream.Write(encoding.GetBytes(header), 0, encoding.GetByteCount(header));

                    // Write the file data directly to the Stream, rather than serializing it to a string.
                    formDataStream.Write(fileToUpload.File, 0, fileToUpload.File.Length);
                }
                else
                {
                    string postData = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}",
                        boundary,
                        param.Key,
                        param.Value);
                    formDataStream.Write(encoding.GetBytes(postData), 0, encoding.GetByteCount(postData));
                }
            }

            // Add the end of the request.  Start with a newline
            string footer = "\r\n--" + boundary + "--\r\n";
            formDataStream.Write(encoding.GetBytes(footer), 0, encoding.GetByteCount(footer));

            // Dump the Stream into a byte[]
            formDataStream.Position = 0;
            byte[] formData = new byte[formDataStream.Length];
            formDataStream.Read(formData, 0, formData.Length);
            formDataStream.Close();

            return formData;
        }

        public class FileParameter
        {
            public byte[] File { get; set; }
            public string FileName { get; set; }
            public string ContentType { get; set; }
            public FileParameter(byte[] file) : this(file, null) { }
            public FileParameter(byte[] file, string filename) : this(file, filename, null) { }
            public FileParameter(byte[] file, string filename, string contenttype)
            {
                File = file;
                FileName = filename;
                ContentType = contenttype;
            }
        }
    }
}
