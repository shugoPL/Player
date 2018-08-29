using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NAudioWrapper
{
    //Klasa abstrakcji pomiędzy ViewModel a biblioteką NAudio
    public class AudioPlayer
    {
        // Enumeracja pozwalająca zidentyfikować czy zatrzumujemy utwór przez osiągnięcie końca pliku czy robi to użytkownik
        public enum PlaybackStopTypes
        {
            PlaybackStoppedByUser, PlaybackStoppedReachingEndOfFile
        }
        public PlaybackStopTypes PlaybackStopType { get; set; }

        private AudioFileReader _audioFileReader; //Handler do odczytywanego utworu
        private DirectSoundOut _output; //Urządzenie wyjściowe
        private string _filePath; //Ścieżka do pliku muzycznego

        // Events Action do sprawdzenia i uzupełnienia wiedza na ten temat
        public event Action PlaybackResumed;
        public event Action PlaybackStopped;
        public event Action PlaybackPaused;

        public AudioPlayer(string filePath, float volume)
        {
            PlaybackStopType = PlaybackStopTypes.PlaybackStoppedReachingEndOfFile;
            
            _audioFileReader = new AudioFileReader(filePath) { Volume = volume };

            _output = new DirectSoundOut(200);
            _output.PlaybackStopped += _output_PlaybackStopped;

            var wc = new WaveChannel32(_audioFileReader);
            wc.PadWithZeroes = false;

            _output.Init(wc);
        }

        public void Play(PlaybackState playbackState, double currentVolumeLevel)
        {
            if (playbackState == PlaybackState.Stopped || playbackState == PlaybackState.Paused)
            {
                _output.Play();
            }
            _audioFileReader.Volume = (float)currentVolumeLevel;

            if (PlaybackResumed != null)
            {
                PlaybackResumed();
            }
        }

        private void _output_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            Dispose();
            PlaybackStopped?.Invoke(); //Ułatwione wywołanie delegata
        }

        public void Stop()
        {
            if (_output != null) _output.Stop();
        }

        public void Pause()
        {
            if (_output != null)
            {
                _output.Pause();
                PlaybackPaused?.Invoke(); //Ułatwonie wywołanie delegata
            }
        }

        public void TogglePlayPause(double currentVolumeLevel)
        {
            if (_output != null)
            {
                if (_output.PlaybackState == PlaybackState.Playing)
                {
                    Pause();
                }
                else
                {
                    Play(_output.PlaybackState, currentVolumeLevel);
                }
            }
            else
            {
                Play(_output.PlaybackState, currentVolumeLevel);
            }
        }

        public void Dispose()
        {
            //Jeżeli wyjście jest zajęte trzeba zwolnić zasoby
            if (_output != null)
            {
                if (_output.PlaybackState == PlaybackState.Playing)
                {
                    _output.Stop();
                }
                _output.Dispose();
                _output = null;
            }
            if (_audioFileReader != null) //Jeżeli handler odczytu nie jest null zwalniamy zasoby i ustawiamy go na null
            {
                _audioFileReader.Dispose();
                _audioFileReader = null;
            }
        }
        //Zwracanie w sekundach długości utworu
        public double GetLenghtInSeconds()
        {
            if (_audioFileReader != null)
                return _audioFileReader.TotalTime.TotalSeconds;
            else return 0;
        }
        //Zwracanie aktualnej pozycji w utworznie w sekundach
        public double GetPositionInSeconds()
        {
            return _audioFileReader != null ? _audioFileReader.CurrentTime.TotalSeconds : 0;
        }
        
        //Pobieranie ustawionej głośności pliku
        public float GetVolume()
        {
            return _audioFileReader != null ? _audioFileReader.Volume : 1;
        }
        //Ustawianie pozycji w pliku na podstawie wartości w sekundach
        public void SetPosition(double value)
        {
            if (_audioFileReader != null) _audioFileReader.CurrentTime = TimeSpan.FromSeconds(value);
        }
        //Ustawianie głośności
        public void SetVolume(float value)
        {
            if(_output != null) _audioFileReader.Volume = value;
        }
    }
}