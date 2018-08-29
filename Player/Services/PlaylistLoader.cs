using Player.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Player.Services
{
    class PlaylistLoader
    {
        public ObservableCollection<Track> Load(string filePath)
        {
            using( var sr = new StreamReader(filePath))
            {
                var tracks = new ObservableCollection<Track>();

                tracks = JsonConvert.DeserializeObject<ObservableCollection<Track>>(sr.ReadToEnd());
                return tracks;
            }
        }
        
    }
}
