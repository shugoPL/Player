using Player.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Player.Services
{
    class PlaylistSaver
    {
        public void Save(ICollection<Track> playlist, string destinationFilename)
        {
            using(var sw = new StreamWriter(destinationFilename))
            {
                string json = JsonConvert.SerializeObject(playlist);
                sw.Write(json);
            }
        }

    }
}
