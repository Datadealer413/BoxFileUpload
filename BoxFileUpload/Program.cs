using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Box.V2;
using Box.V2.Auth;
using Box.V2.Config;
using Box.V2.Exceptions;
using Box.V2.Models;
using Nito.AsyncEx;
using System.IO;

namespace BoxFileUpload
{
    //Make sure you install the NuGet package 'Box Windows SDK V2' 

    class Program
    {
        const string CLIENT_ID = "THIS IS CLIENT_ID";
        const string CLIENT_SECRET = "THIS IS CLIENT_SECRET_TOKEN";
        // const string DEV_ACCESS_TOKEN = "THIS IS DEV_ACCESS_TOKEN";  //log into developers.box.com and get this for your registered app; it will last for 60 minutes
        const string ACCESS_TOKEN = "THIS IS ACCESS KEY";
        const string REFRESH_TOKEN = "THIS_IS_NOT_NECESSARY_FOR_A_DEV_TOKEN_BUT_MUST_BE_HERE";

        //set these to point to whatever file you want to upload; make sure it exists!
        //const string PATH_TO_FILE = "C:\\home\\site\\wwwroot\\app_data\\jobs\\triggered\\DataPackager\\";
        static string PATH_TO_FILE = "C:\\";

        static string FILENAME = "temp.edp7";

        static string BOX_FOLDER = "/asdf/sdfg"; //for this example code, make sure this folder structure exists in Box

        static BoxClient client;

        public static string CLIENT_SECRET1 => CLIENT_SECRET;

        static void Main(string[] args)
        {
            //http://blog.stephencleary.com/2012/02/async-console-programs.html
            if (args.Length < 4)
            {
                return;
            }
            //string DEV_ACCESS_TOKEN = "zSi8XS3NioqblDQsNfEe4h5JNi5wzRgi";
            //DEV_ACCESS_TOKEN = args[0];
            BOX_FOLDER = args[1];
            PATH_TO_FILE = args[2];
            FILENAME = args[3];

            try
            {
                var config = new BoxConfig(CLIENT_ID, CLIENT_SECRET1, new Uri("https://app.box.com/static/sync_redirect.html"));
                var session = new OAuthSession(ACCESS_TOKEN, REFRESH_TOKEN, 3600*24*365, "bearer");
                
                client = new BoxClient(config, session);

                AsyncContext.Run(() => MainAsync());
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
            
        }

        static async Task MainAsync()
        {
            var boxFolderId = await FindBoxFolderId(BOX_FOLDER);

            using (FileStream fs = File.Open(PATH_TO_FILE, FileMode.Open, FileAccess.Read))
            {
                Console.WriteLine("Uploading file...");

                // Create request object with name and parent folder the file should be uploaded to
                BoxFileRequest request = new BoxFileRequest()
                {
                    Name = FILENAME,
                    Parent = new BoxRequestEntity() { Id = boxFolderId }
                };
                BoxFile f = await client.FilesManager.UploadAsync(request, fs);
            }
        }

        static async Task<String> FindBoxFolderId(string path)
        {
            var folderNames = path.Split('/');
            folderNames = folderNames.Where((f) => !String.IsNullOrEmpty(f)).ToArray(); //get rid of leading empty entry in case of leading slash

            var currFolderId = "0"; //the root folder is always "0"
            foreach (string folderName in folderNames)
            {
                Console.WriteLine(folderName);
                var folderInfo = await client.FoldersManager.GetInformationAsync(currFolderId);
                BoxFolder foundFolder;
                try
                {
                    foundFolder = folderInfo.ItemCollection.Entries.OfType<BoxFolder>().First((f) => f.Name == folderName);
                }
                catch(Exception e)
                {
                    var folderParams = new BoxFolderRequest()
                    {
                        Name = folderName,
                        Parent = new BoxRequestEntity()
                        {
                            Id = currFolderId
                        }
                    };
                    await client.FoldersManager.CreateAsync(folderParams);
                    folderInfo = await client.FoldersManager.GetInformationAsync(currFolderId);
                    foundFolder = folderInfo.ItemCollection.Entries.OfType<BoxFolder>().First((f) => f.Name == folderName);
                }
                currFolderId = foundFolder.Id;
            }

            return currFolderId;
        }
    }
}
