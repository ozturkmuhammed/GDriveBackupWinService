using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Newtonsoft.Json;
using OAuthConsoleApp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI;
using static Google.Apis.Drive.v3.DriveService;


namespace LoopService
{
    public class GoogleDriveSync : BackgroundService
    {
        const string FilePathSecret = "C:\\GDrive\\Secret.json";
        const string FilePathAuth = "C:\\GDrive\\Config.json";
        protected override void Execute()
        {
            __GoogleDriveConfigInfoSecret = JsonConvert.DeserializeObject<GoogleDriveTokenConfigInfo>(PathHelper.TxtReader(FilePathSecret));
            __GoogleDriveConfigInfoAuth = JsonConvert.DeserializeObject<GoogleDriveSecretConfigInfo>(PathHelper.TxtReader(FilePathAuth));

            if (__GoogleDriveConfigInfoSecret.ACCESSTOKEN == "" || __GoogleDriveConfigInfoSecret.REFRESHTOKEN == "")
            {
                return;
            }

            GetTokenAsync();
            string folderName = "EduNette Sunucu Yedekleri";
            string sunucuyedekleriid = "";
            try
            {

                sunucuyedekleriid = GetFolderId(folderName, "");
                if (sunucuyedekleriid == "")
                {
                    sunucuyedekleriid = CreateFolder(folderName, "");
                }


            }
            catch (Exception ex)
            {
                __GoogleDriveConfigInfoSecret.ACCESSTOKEN = null;
                __GoogleDriveConfigInfoSecret.REFRESHTOKEN = null;
                PathHelper.DosyayaYazUtf8(FilePathAuth, PathHelper.ToJson(FilePathSecret), true);
                throw ex;
            }
            List<string> directoryFiles = GetBackupFolderFiles();
            if (directoryFiles.Count > 0)
            {

                UploadFiles(sunucuyedekleriid);
                GecmisYedekleriSil(folderName);

            }


        }

        private void GecmisYedekleriSil(string folderName)
        {
            var allFiles = GoogleDriveFileListDirectoryStructure.ListAll(service, new GoogleDriveFileListDirectoryStructure.FilesListOptionalParms { Q = "('root' in parents)", PageSize = 1000 });
            allFiles.Files = allFiles.Files.ToList().Where(x => x.Name == folderName && x.Trashed == false).ToList();
            var allFilesList = GoogleDriveFileListDirectoryStructure.PrettyPrint(service, allFiles, "");
            allFilesList.RemoveAll(x => x.Trashed == true);

            for (int i = 31; i < 62; i++)
            {
                string dayfolderDelete = DateTime.Now.AddDays(i * -1).ToString("yyyyMMdd");
                List<Google.Apis.Drive.v3.Data.File> driveContainsFile = allFilesList.Where(x => x.Name.Contains(dayfolderDelete)).ToList();

                foreach (var item in driveContainsFile)
                {
                    DeleteFile(item.Id);
                }
            }
        }

        private static readonly string[] Scopes = new string[] { DriveService.Scope.Drive, DriveService.Scope.DriveFile, DriveService.Scope.DriveReadonly };
        private static DriveService service;
        private static GoogleDriveTokenConfigInfo __GoogleDriveConfigInfoSecret;
        private static GoogleDriveSecretConfigInfo __GoogleDriveConfigInfoAuth;

        public static void GetTokenAsync()
        {
            var tokenResponse = new TokenResponse
            {
                AccessToken = __GoogleDriveConfigInfoSecret.ACCESSTOKEN,
                RefreshToken = __GoogleDriveConfigInfoSecret.REFRESHTOKEN,
            };
            string clientId = __GoogleDriveConfigInfoAuth.CLIENTID;
            string clientSecret = __GoogleDriveConfigInfoAuth.CLIENTSECRET;

            var applicationName = "Drive Api";
            var username = __GoogleDriveConfigInfoAuth.EMAIL;


            var apiCodeFlow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret,
                },
                Scopes = Scopes,
                DataStore = new FileDataStore(applicationName)
            });


            var credential = new UserCredential(apiCodeFlow, username, tokenResponse);


            service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = applicationName
            });


        }
        public static List<Google.Apis.Drive.v3.Data.File> GetAllFiles(string folderName)
        {
            var allFiles = GoogleDriveFileListDirectoryStructure.ListAll(service, new GoogleDriveFileListDirectoryStructure.FilesListOptionalParms { Q = "('root' in parents)", PageSize = 1000 });
            allFiles.Files = allFiles.Files.ToList().Where(x => x.Name == folderName && x.Trashed == false).ToList();

            return allFiles.Files.ToList();
        }
        public static string CreateFolder(string folderName, string parentid)
        {
            var newFile = new Google.Apis.Drive.v3.Data.File { Name = folderName, MimeType = "application/vnd.google-apps.folder" };

            string folderid = GetFolderId(folderName, parentid);
            if (folderid != "")
            {
                return folderid;
            }
            if (parentid!="")
            {
                newFile.Parents = new string[] { parentid };
            }


            var result = service.Files.Create(newFile).Execute();

            return result.Id;
        }
        public static string GetFolderId(string folderName, string parentid)
        {
            var request = service.Files.List();
            request.Q = $"mimeType = 'application/vnd.google-apps.folder' and name = '{folderName}'";
            request.Fields = "files(*)";
            var response = request.Execute();

            // List files.
            IList<Google.Apis.Drive.v3.Data.File> files = request.Execute().Files;
            files = files.OrderBy(x => x.CreatedTime).ToList();
            if (parentid == "")
            {
                if (files.Count == 1)
                {
                    return files[0].Id;
                }
            }

            if (parentid=="" && files.Count > 0)
            {
                return files[0].Id;
            }
            Google.Apis.Drive.v3.Data.File folderDetail = files.Where(x => x.Trashed == false && x.Parents.Contains(parentid)).FirstOrDefault();
            if (folderDetail != null)
            {
                return folderDetail.Id;
            }
            else
            {
                return "";
            }
            return "";
        }

        private string GetMimeType(string file)
        {
            string mimeType = "application/unknown";
            string ext = System.IO.Path.GetExtension(file).ToLower();

            Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext);

            if (regKey != null && regKey.GetValue("Content Type") != null)
            {
                mimeType = regKey.GetValue("Content Type").ToString();
            }

            return mimeType;
        }

        public bool UploadFile(string path, string folderId)
        {


            try
            {

                var FileMetaData = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = Path.GetFileName(path),
                    MimeType = GetMimeType(path),
                    //id of parent folder 
                    Parents = new List<string>
                {
                    folderId
                }
                };

                using (var stream = new System.IO.FileStream(path, System.IO.FileMode.Open))
                {
                    var request = service.Files.Create(FileMetaData, stream, FileMetaData.MimeType);
                    request.Fields = "id";

                    var response = request.Upload();
                    if (response.Status != Google.Apis.Upload.UploadStatus.Completed)
                        throw response.Exception;
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }


        }
        public void DeleteFile(string fileId)
        {

            var command = service.Files.Delete(fileId);
            var result = command.Execute();
        }

        private void UploadFiles(string sunucuyedekleriid)
        {
            List<string> directoryFiles = GetBackupFolderFiles();
            foreach (var item in directoryFiles)
            {

                string filename = Path.GetFileName(item);
                if (filename.ToLower().EndsWith(".tmp") || filename.ToLower().EndsWith(".bak"))
                {
                    continue;
                }
                string dayfolderid = "";
                string dayfolder = DateTime.Now.ToString("yyyy.MM.dd");
                if (filename.ToLower().Contains("bak.zip"))
                {
                    string firmaismi = filename.Split('_')[0];
                    string firmafolderid = CreateFolder(firmaismi, sunucuyedekleriid);
                    dayfolderid = CreateFolder(dayfolder, firmafolderid);
                }
                else
                {

                    dayfolderid = CreateFolder(dayfolder, sunucuyedekleriid);
                }
                if (UploadFile(item, dayfolderid))
                {
                    System.IO.File.Delete(item);
                }
            }
        }
        public List<string> GetBackupFolderFiles()
        {
            string path = @"C:\TaskBackup";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            List<string> directoryFiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).ToList();
            return directoryFiles;

        }
        protected override int GetIntervalSeconds()
        {
            return 60;
        }


    }
    internal class GoogleDriveFileListDirectoryStructure
    {


        public class FilesListOptionalParms
        {
            /// The source of files to list.
            public string Corpus { get; set; }

            /// A comma-separated list of sort keys. Valid keys are 'createdTime', 'folder', 'modifiedByMeTime', 'modifiedTime', 'name', 'quotaBytesUsed', 'recency', 'sharedWithMeTime', 'starred', and 'viewedByMeTime'. Each key sorts ascending by default, but may be reversed with the 'desc' modifier. Example usage: ?orderBy=folder,modifiedTime desc,name. Please note that there is a current limitation for users with approximately one million files in which the requested sort order is ignored.
            public string OrderBy { get; set; }

            /// The maximum number of files to return per page.
            public int PageSize { get; set; }

            /// The token for continuing a previous list request on the next page. This should be set to the value of 'nextPageToken' from the previous response.
            public string PageToken { get; set; }

            /// A query for filtering the file results. See the "Search for Files" guide for supported syntax.
            public string Q { get; set; }

            /// A comma-separated list of spaces to query within the corpus. Supported values are 'drive', 'appDataFolder' and 'photos'.
            public string Spaces { get; set; }
        }

        /// <summary>
        /// Lists or searches files.
        /// Documentation https://developers.google.com/drive/v3/reference/files/list
        /// Generation Note: This does not always build correctly.  Google needs to standardize things I need to figure out which ones are wrong.
        /// </summary>
        /// <param name="service">Authenticated Drive service. </param>
        /// <param name="optional">The optional parameters. </param>
        /// <returns>FileListResponse</returns>
        public static Google.Apis.Drive.v3.Data.FileList ListAll(DriveService service, FilesListOptionalParms optional = null)
        {
            try
            {
                // Initial validation.
                if (service == null)
                    throw new ArgumentNullException("service");

                // Building the initial request.
                var request = service.Files.List();

                // Applying optional parameters to the request.
                request = (FilesResource.ListRequest)ApplyOptionalParms(request, optional);
                request.Fields = "files(*)";
                var pageStreamer = new Google.Apis.Requests.PageStreamer<Google.Apis.Drive.v3.Data.File, FilesResource.ListRequest, Google.Apis.Drive.v3.Data.FileList, string>(
                                                   (req, token) => request.PageToken = token,
                                                   response => response.NextPageToken,
                                                   response => response.Files);

                var allFiles = new Google.Apis.Drive.v3.Data.FileList();
                allFiles.Files = new List<Google.Apis.Drive.v3.Data.File>();

                foreach (var result in pageStreamer.Fetch(request))
                {
                    allFiles.Files.Add(result);
                }

                return allFiles;
            }
            catch (Exception Ex)
            {
                throw new Exception("Request Files.List failed.", Ex);
            }
        }

        public static List<Google.Apis.Drive.v3.Data.File> PrettyPrint(DriveService service, Google.Apis.Drive.v3.Data.FileList list, string indent)
        {
            List<Google.Apis.Drive.v3.Data.File> fileList = new List<Google.Apis.Drive.v3.Data.File>();
            foreach (var item in list.Files.OrderBy(a => a.Name))
            {
                if (item.MimeType == "application/vnd.google-apps.folder")
                {
                    var ChildrenFiles = ListAll(service, new FilesListOptionalParms { Q = string.Format("('{0}' in parents)", item.Id), PageSize = 1000 });


                    fileList.AddRange(PrettyPrint(service, ChildrenFiles, indent + "  "));
                }
                else
                {
                    fileList.Add(item);
                }
            }
            return fileList;
        }
        public static object ApplyOptionalParms(object request, object optional)
        {
            if (optional == null)
                return request;

            System.Reflection.PropertyInfo[] optionalProperties = (optional.GetType()).GetProperties();

            foreach (System.Reflection.PropertyInfo property in optionalProperties)
            {
                // Copy value from optional parms to the request.  They should have the same names and datatypes.
                System.Reflection.PropertyInfo piShared = (request.GetType()).GetProperty(property.Name);
                piShared.SetValue(request, property.GetValue(optional, null), null);
            }

            return request;
        }
    }
}
