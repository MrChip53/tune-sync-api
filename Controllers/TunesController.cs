using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TuneSyncAPI.Controllers
{
    [Produces("application/json")]
    [Route("[controller]")]
    [ApiController]
    public class TunesController : ControllerBase
    {
        //TODO needs to be set in a webui admin panel
        private const string MusicPath = "/Music/";

        private AppDB Db { get; }

        public TunesController(AppDB db)
        {
            Db = db;
        }

        [HttpGet("scan")]
        public async Task<IActionResult> GetScan()
        {
            await Db.Connection.OpenAsync();
            var query = new TunesQuery(Db);
            var tunes = await query.ReadAllTunesAsync();
            var dbFileList = tunes.Select(tune => tune.Path).ToList();

            List<String> folderFileList = Util.scanDirectory(MusicPath);

            foreach (String file in folderFileList)
            {
                String newFile = file.Replace(MusicPath, "");

                if (!dbFileList.Contains(newFile))
                {
                    //Insert
                    byte[] hash;
                    string strHash;
                    using (var md5 = System.Security.Cryptography.MD5.Create())
                    {
                        await using (var stream = System.IO.File.OpenRead(file))
                        {
                            hash = await md5.ComputeHashAsync(stream);
                            strHash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                        }
                    }

                    await query.InsertAsync(newFile, strHash);
                }
                else
                {
                    if (newFile.EndsWith(".mp3"))
                    {
                        var tune = tunes.Single(p => p.Path == newFile);
                        if (tune.Artist == string.Empty || tune.Title == string.Empty)
                        {
                            var tagFile = TagLib.File.Create(MusicPath + newFile);
                            await query.UpdateTitleArtistAsync(tune.ID, string.Join(",", tagFile.Tag.AlbumArtists),
                                tagFile.Tag.Title);
                        }
                    }
                }
            }

            return new JsonResult(new ReturnStatus("ok"));
        }

        //GET tunes/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDownload(string id)
        {
            await Db.Connection.OpenAsync();
            var query = new TunesQuery(Db);

            Tune dlTune = await query.GetTuneByIdAsync(Int32.Parse(id));

            if (dlTune == null)
                return NotFound();

            FileStream stream = new FileStream(MusicPath + dlTune.Path, FileMode.Open, FileAccess.Read);

            if (stream == null)
                return NotFound(); // returns a NotFoundResult with Status404NotFound response.

            String[] fileSplit = dlTune.Path.Split("/");

            String fileName;

            if (fileSplit.Length > 1)
                fileName = fileSplit[fileSplit.Length - 1];
            else
                fileName = dlTune.Path;

            return File(stream, "application/octet-stream", fileName); // returns a FileStreamResult
        }

        //GET tunes/
        [HttpGet]
        public async Task<JsonResult> Get()
        {
            await Db.Connection.OpenAsync();
            var query = new TunesQuery(Db);

            List<Tune> listTune = await query.ReadAllTunesAsync();

            Tunes retTunes = new Tunes(listTune);
            return new JsonResult(retTunes);
        }
    }
}