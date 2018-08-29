using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using NAudioWrapper;
using Player.Models;
using Player.Services;


namespace Player.ViewModels
{
    class MainWindowViewModel : INotifyPropertyChanged
    {
        private string _title;
        private double _currentTrackLength;
        private double _currentTrackPosition;
        private string _playPauseImageSource;
        private float _currentVolume;
        private string _presentTime;

        private ObservableCollection<Track> _playlist;
        private Track _currentlyPlayingTrack;
        private Track _currnetlySelectedTrack;
        private AudioPlayer _audioPlayer;

        public string PresentTime
        {
            get { return _presentTime; }
            set
            {
                if (value.Equals(_presentTime)) return;
                _presentTime = value;
                OnPropertyChanged(nameof(PresentTime));
            }
        }


        public string Title
        {
            get { return _title; }
            set
            {
                if (value == _title) return;
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }

        
        public string PlayPauseImageSource
        {
            get { return _playPauseImageSource; }
            set
            {
                if (value == _playPauseImageSource) return;
                _playPauseImageSource = value;
                OnPropertyChanged(nameof(PlayPauseImageSource));
            }
        }

        public float CurrentVolume
        {
            get { return _currentVolume; }
            set
            {
                if (value.Equals(_currentVolume)) return;
                _currentVolume = value;
                OnPropertyChanged(nameof(CurrentVolume));
            }
        }

        public double CurrentTrackLength
        {
            get { return _currentTrackLength; }
            set
            {
                if (value.Equals(_currentTrackLength)) return;
                _currentTrackLength = value;
                OnPropertyChanged(nameof(CurrentTrackLength));
            }
        }

        public double CurrentTrackPosition
        {
            get { return _currentTrackPosition; }
            set
            {
                if (value.Equals(_currentTrackPosition)) return;
                _currentTrackPosition = value;
                OnPropertyChanged(nameof(CurrentTrackPosition));
            }
        }

        public Track CurrentlySelectedTrack
        {
            get { return _currnetlySelectedTrack; }
            set
            {
                if (Equals(value, _currnetlySelectedTrack)) return;
                _currnetlySelectedTrack = value;
                OnPropertyChanged(nameof(CurrentlySelectedTrack));
            }
        }

        public Track CurrentlyPlayingTrack
        {
            get { return _currentlyPlayingTrack; }
            set
            {
                if (Equals(value, _currentlyPlayingTrack)) return;
                _currentlyPlayingTrack = value;
                OnPropertyChanged(nameof(CurrentlyPlayingTrack));
            }
        }

        public ObservableCollection<Track> Playlist
        {
            get { return _playlist; }
            set
            {
                if (Equals(value, _playlist)) return;
                _playlist = value;
                OnPropertyChanged(nameof(Playlist));
            }
        }
        
    #region Declaration of Commands 

        // Obsługa górnego menu
        public ICommand ExitApplicationCommand { get; set; }
        public ICommand AddFileToPlaylistCommand { get; set; }
        public ICommand AddFolderToPlaylistCommand { get; set; }
        public ICommand SavePlaylistCommand { get; set; }
        public ICommand LoadPlaylistCommand { get; set; }

        //Obsługa przycisków w UI
        public ICommand RewindToStartCommand { get; set; }
        public ICommand StartPlaybackCommand { get; set; }
        public ICommand StopPlaybackCommand { get; set; }
        public ICommand ForwardToEndCommand { get; set; }
        public ICommand ShuffleCommand { get; set; }

        //Obsługa slidera 
        public ICommand TrackControlMouseDownCommand { get; set; }
        public ICommand TrackControlMouseUpCommand { get; set; }
        public ICommand VolumeControlValueChangedCommand { get; set; }

        //Obsługa Context Menu
        public ICommand RemoveItemCommand { get; set; }

    #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        private enum PlaybackState
        {
            Playing, Stopped, Paused
        }
        private PlaybackState _playbackState;


        public MainWindowViewModel()
        {
            Application.Current.MainWindow.Closing += MainWindow_Closing;
            LoadCommands();
            Playlist = new ObservableCollection<Track>();

            _playbackState = PlaybackState.Stopped;

            Title = "Odtwarzacz Audio";
            PlayPauseImageSource = "../Images/play.png";
            CurrentVolume = 1;
            var timer = new System.Timers.Timer
            {
                Interval = 300
            };
            timer.Elapsed += TimerElapsed;
            timer.Start();
        }

        private void LoadCommands()
        {
            //Menu Commands
            ExitApplicationCommand = new RelayCommand(ExitApplication, CanExitApplication => { return true; });
            AddFileToPlaylistCommand = new RelayCommand(AddFileToPlaylist, CanAddFileToPlaylist);
            AddFolderToPlaylistCommand = new RelayCommand(AddFolderToPlaylist, CanAddFolderToPlaylist => { return true; });
            SavePlaylistCommand = new RelayCommand(SavePlaylist, CanSavePlaylist => { return true; }); //Brak osobnej metody do CanSavePlaylist ponieważ nie potrzeba nic dodawać tylko zwraca true
            LoadPlaylistCommand = new RelayCommand(LoadPlaylist, CanLoadPlaylist => { return true; });

            //Player Commands
            RewindToStartCommand = new RelayCommand(RewindToStart, CanRewindToStart);
            StartPlaybackCommand = new RelayCommand(StartPlayback, CanStartPlayack);
            StopPlaybackCommand = new RelayCommand(StopPlayback, CanStopPlayback );
            ForwardToEndCommand = new RelayCommand(ForwardToEnd, CanForwardToEnd);
            ShuffleCommand = new RelayCommand(Shuffle, CanShuffle => { return true; });

            // Event Commands
            TrackControlMouseDownCommand = new RelayCommand(TrackControlMouseDown, CanTrackControlMouseDown);
            TrackControlMouseUpCommand = new RelayCommand(TrackControlMouseUp, CanTrackControlMouseUp);
            VolumeControlValueChangedCommand = new RelayCommand(VolumeControlValueChanged, CanVolumeControlValueChanged => { return true; });

            //Context Menu
            RemoveItemCommand = new RelayCommand(RemoveItem, CanRemoveItem);
        }
        
        
        #region Menu Commands Implementation

        private void ExitApplication(object obj)
        {
            if(_audioPlayer != null)
            {
                _audioPlayer.Dispose();
            }
            Application.Current.Shutdown();
        }
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (_audioPlayer != null) _audioPlayer.Dispose();
        }
        
        private void AddFileToPlaylist(object obj)
        {
            var odf = new OpenFileDialog
            {
                Filter = "Audio files (*.wav, *.mp3, *.wma, *.ogg, *.flac) | *.wav; *.mp3; *.wma; *.ogg; *.flac",
                Multiselect = true
            };
            var result = odf.ShowDialog();
            if(result == true)
            {
                foreach(var file in odf.FileNames)
                {
                    var friendlyName = Path.GetFileName(file); //file.GetFileName.Length - 4
                    friendlyName = friendlyName.Remove(friendlyName.Length - 4);
                    var track = new Track(file, friendlyName);
                    Playlist.Add(track);
                }
            }
        }

        private bool CanAddFileToPlaylist(object obj)
        {
            if (_playbackState == PlaybackState.Stopped) return true;

            return false;
        }
        private void AddFolderToPlaylist(object obj)
        {
            //Dorobić na podstawie OpenDialog lub biblioteki microsoft
            throw new NotImplementedException();
        }

        private bool CanAddFolderToPlaylist(object obj)
        {
            //Ewentualna implementacja warunków w po dodaniu funkcjonalności AddFolderToPlaylist
            throw new NotImplementedException();
        }

        private void SavePlaylist(object obj)
        {
            var sfd = new SaveFileDialog
            {
                CreatePrompt = false,
                OverwritePrompt = true,
                Filter = "PLAYLIST files (*.json) | *.json"
            };
            if ( sfd.ShowDialog() == true)
            {
                var ps = new PlaylistSaver();
                ps.Save(Playlist, sfd.FileName);
            }
        }
        
        private void LoadPlaylist(object obj)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "PLAYLIST files (*.json) | *.json"
            };
            if(ofd.ShowDialog() == true)
            {
                Playlist = new PlaylistLoader().Load(ofd.FileName);
            }
        }

        private void RemoveItem(object obj)
        {
            Playlist.Remove(CurrentlySelectedTrack);
        }

        private bool CanRemoveItem(object obj)
        {
            //Sprawdzenie czy nie usuwamy utworu który jest obecnie odtwarzany
            if (CurrentlyPlayingTrack != CurrentlySelectedTrack) return true;
            return false;
        }
        
        #endregion
        
        #region Playback Commands Implementation
        private void RewindToStart(object obj)
        {
            _audioPlayer.SetPosition(0);
        }

        private bool CanRewindToStart(object obj)
        {
            if (_playbackState == PlaybackState.Playing)
            {
                return true;
            }
            return false;
        }

        private void StartPlayback(object obj)
        {
            if( CurrentlySelectedTrack != null)
            {
                if( _playbackState == PlaybackState.Stopped)
                {
                    _audioPlayer = new AudioPlayer(CurrentlySelectedTrack.Filepath, CurrentVolume)
                    {
                        PlaybackStopType = AudioPlayer.PlaybackStopTypes.PlaybackStoppedReachingEndOfFile
                    };
                    _audioPlayer.PlaybackPaused += _audioPlayer_PlaybackPaused;
                    _audioPlayer.PlaybackResumed += _audioPlayer_PlaybackResumed;
                    _audioPlayer.PlaybackStopped += _audioPlayer_PlaybackStopped;
                    CurrentTrackLength = _audioPlayer.GetLenghtInSeconds();
                    CurrentlyPlayingTrack = CurrentlySelectedTrack;
                    PresentTime = ConvertTime(CurrentTrackLength);
                }
                if( CurrentlySelectedTrack == CurrentlyPlayingTrack)
                {
                    _audioPlayer.TogglePlayPause(CurrentVolume);
                }

            }
        }

        private bool CanStartPlayack(object obj)
        {
            if (CurrentlySelectedTrack != null) return true;
            return false;
        }

        private void StopPlayback(object obj)
        {
            if( _audioPlayer != null)
            {
                _audioPlayer.PlaybackStopType = AudioPlayer.PlaybackStopTypes.PlaybackStoppedByUser;
                _audioPlayer.Stop();
            }
        }
        private bool CanStopPlayback(object obj)
        {
            if(_playbackState == PlaybackState.Playing || _playbackState == PlaybackState.Paused)
            {
                return true;
            }
            return false;
        }

        private void ForwardToEnd(object obj)
        {
            if( _audioPlayer != null)
            {
                _audioPlayer.SetPosition(_audioPlayer.GetLenghtInSeconds() - 0.001);
                _audioPlayer.PlaybackStopType = AudioPlayer.PlaybackStopTypes.PlaybackStoppedReachingEndOfFile;
                
            }
        }
        private bool CanForwardToEnd(object obj)
        {
            if (_playbackState == PlaybackState.Playing) return true;
            return false; //else
        }

        private void Shuffle(object obj)
        {
            Playlist = Playlist.Shuffle();
        }

        private bool CanShuffle(object obj)
        {
            if (_playbackState == PlaybackState.Stopped) return true;
            return false; //else
        }

        #endregion

        #region Sliders Events Commands Implementation
        //Co robiby w przypadku naciśnęcia myszą na element w seekbar
        private void TrackControlMouseDown(object obj)
        {
            if( _audioPlayer != null)
            {
                _audioPlayer.Pause();
            }
        }
        private bool CanTrackControlMouseDown(object obj)
        {
            if(_playbackState == PlaybackState.Playing)
            {
                return true;
            }
            return false;
        }
        //Co robimy po puszczeniu klawisza myszy
        private void TrackControlMouseUp(object obj)
        {
            if(_audioPlayer != null)
            {
                _audioPlayer.SetPosition(CurrentTrackPosition);
                _audioPlayer.Play(NAudio.Wave.PlaybackState.Paused, CurrentVolume);
            }
        }

        private bool CanTrackControlMouseUp(object obj)
        {
            if (_playbackState == PlaybackState.Paused) return true;
            return false; //else
        }

        private void VolumeControlValueChanged(object obj)
        {
            if(_audioPlayer != null)
            {
                _audioPlayer.SetVolume(CurrentVolume);
            }
        }
        
        #endregion


        //Events for AudioPlayer
        private void _audioPlayer_PlaybackStopped()
        {
            _playbackState = PlaybackState.Stopped;
            PlayPauseImageSource = "../Images/play.png";
            CommandManager.InvalidateRequerySuggested();
            CurrentTrackPosition = 0;

            if(_audioPlayer.PlaybackStopType == AudioPlayer.PlaybackStopTypes.PlaybackStoppedReachingEndOfFile)
            {
                CurrentlySelectedTrack = Playlist.NextItem(CurrentlyPlayingTrack);
                StartPlayback(null);
            }
        }

        private void _audioPlayer_PlaybackResumed()
        {
            _playbackState = PlaybackState.Playing;
            PlayPauseImageSource = "../Images/pause.png";
        }

        private void _audioPlayer_PlaybackPaused()
        {
            _playbackState = PlaybackState.Paused;
            PlayPauseImageSource = "../Images/play.png";
        }

        // Timer methods
        private void UpdateSeekbar()
        {
            if(_playbackState == PlaybackState.Playing)
            {
                CurrentTrackPosition = _audioPlayer.GetPositionInSeconds();
            }
        }

        private void TimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            UpdateSeekbar();
        }

        public string ConvertTime(double time)
        {
            TimeSpan t = TimeSpan.FromSeconds(time);
            string presentFormat = string.Format("{0:D2}h:{1:D2}m:{2:D2}s", t.Hours, t.Minutes, t.Seconds);
            return presentFormat;
        }

        
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
