using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TuneSyncAPI
{
    public class TunesQuery
    {

        private AppDB Db { get; }

        public TunesQuery(AppDB appDb)
        {
            Db = appDb;
        }

        public async Task<Tune> GetTuneByIdAsync(int id)
        {
            await using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"SELECT `ID`, `Path`, `Hash`, `artist`, `title` FROM `tunes` WHERE `Id` = @id";
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@id",
                DbType = DbType.Int32,
                Value = id,
            });
            var result = await ReadAllAsync(await cmd.ExecuteReaderAsync());
            return result.Count > 0 ? result[0] : null;
        }
        
        public async Task<Tune> GetTuneByPathAsync(string path)
        {
            await using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"SELECT `ID`, `Path`, `Hash`, `artist`, `title` FROM `tunes` WHERE `Path` = @path";
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@id",
                DbType = DbType.String,
                Value = path,
            });
            var result = await ReadAllAsync(await cmd.ExecuteReaderAsync());
            return result.Count > 0 ? result[0] : null;
        }

        public async Task InsertAsync(string path, string hash)
        {
            await using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"INSERT INTO `tunes` (`Path`, `Hash`) VALUES (@path, @hash);";
            BindParams(cmd, path, hash);
            await cmd.ExecuteNonQueryAsync();
        }
        
        public async Task UpdateTitleArtistAsync(int id, string artist, string title)
        {
            await using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"UPDATE `tunes` SET `artist` = @artist, `title` = @title WHERE id = @id";
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@id",
                DbType = DbType.Int32,
                Value = id,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@artist",
                DbType = DbType.String,
                Value = artist,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@title",
                DbType = DbType.String,
                Value = title,
            });
            await cmd.ExecuteNonQueryAsync();
        }

        private void BindParams(MySqlCommand cmd, string path, string hash)
        {
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@path",
                DbType = DbType.String,
                Value = path,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@hash",
                DbType = DbType.String,
                Value = hash,
            });
        }

        public async Task<List<Tune>> ReadAllTunesAsync()
        {
            await using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"SELECT * FROM `tunes`;";
            return await ReadAllAsync(await cmd.ExecuteReaderAsync());
        }

        private static async Task<List<Tune>> ReadAllAsync(DbDataReader reader)
        {
            var tunes = new List<Tune>();
            await using (reader)
            {
                while (await reader.ReadAsync())
                {
                    var tune = new Tune(reader);
                    
                    tunes.Add(tune);
                }
            }
            return tunes;
        }

    }
}
