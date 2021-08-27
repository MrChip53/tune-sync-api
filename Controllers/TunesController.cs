using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

            var folderFileList = Util.scanDirectory(MusicPath);

            foreach (var file in folderFileList)
            {
                var newFile = file.Replace(MusicPath, "");

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
                    
                    // TODO add support for title and artist here
                    await query.InsertAsync(newFile, strHash);
                }
                else
                {
                    if (newFile.EndsWith(".mp3"))
                    {
                        var tune = tunes.Single(p => p.Path == newFile);
                        if (string.IsNullOrEmpty(tune.Artist) || string.IsNullOrEmpty(tune.Title))
                        {
                            var tagFile = TagLib.File.Create(MusicPath + newFile);
                            await query.UpdateTitleArtistAsync(tune.Id, string.Join(",", tagFile.Tag.AlbumArtists),
                                tagFile.Tag.Title);
                        }
                    }
                }
            }

            return new JsonResult(new ReturnStatus("ok"));
        }

        // GET tunes/grab/{id}
        [HttpGet("grab/{id}")]
        public async Task<IActionResult> GetDownload(int id)
        {
            await Db.Connection.OpenAsync();
            var query = new TunesQuery(Db);

            try
            {
                var dlTune = await query.GetTuneByIdAsync(id);

                if (dlTune == null)
                    return NotFound();

                var stream = new FileStream(MusicPath + dlTune.Path, FileMode.Open, FileAccess.Read);

                var fileSplit = dlTune.Path.Split("/");
                
                var fileName = fileSplit.Length > 1 ? fileSplit[^1] : dlTune.Path;

                return File(stream, "application/octet-stream", fileName); // returns a FileStreamResult
            }
            catch (FormatException)
            {
                return BadRequest("Error with request: Improper ID.");
            }
            catch (IOException)
            {
                return BadRequest("Error with request.");
            }
            
        }
        
        // GET tunes/delete/{id}
        [HttpGet("delete/{id}")]
        public async Task<IActionResult> DeleteTune(int id)
        {
            await Db.Connection.OpenAsync();
            var query = new TunesQuery(Db);

            try
            {
                var dlTune = await query.GetTuneByIdAsync(id);

                if (dlTune == null)
                    return NotFound();

                System.IO.File.Delete(MusicPath + dlTune.Path);

                await query.DeleteTune(id);

                return new OkResult();
            }
            catch (FormatException)
            {
                return BadRequest("Error with request: Improper ID.");
            }
            catch (IOException)
            {
                return BadRequest("Error with request.");
            }
            
        }

        //GET tunes/
        [HttpGet]
        public async Task<JsonResult> Get()
        {
            await Db.Connection.OpenAsync();
            var query = new TunesQuery(Db);

            var listTune = await query.ReadAllTunesAsync();

            var retTunes = new Tunes(listTune);
            return new JsonResult(retTunes);
        }
    }
}