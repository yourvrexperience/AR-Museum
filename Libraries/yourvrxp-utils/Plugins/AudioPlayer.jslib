var AudioPlayer = {
    $AudioPlayer__deps: [],
    $AudioPlayer: {
        audioElement: null,

        PlayAudioFromURL: function(url) {
            if (AudioPlayer.audioElement === null) {
                AudioPlayer.audioElement = new Audio();
            }
            AudioPlayer.audioElement.src = UTF8ToString(url);
            AudioPlayer.audioElement.play();
        },

        StopAudio: function() {
            if (AudioPlayer.audioElement !== null) {
                AudioPlayer.audioElement.pause();
                AudioPlayer.audioElement.currentTime = 0;
            }
        }
    },

    PlayJSAudioFromURL: function(url) {
        AudioPlayer.PlayAudioFromURL(url);
    },

    StopJSAudio: function() {
        AudioPlayer.StopAudio();
    }
};

autoAddDeps(AudioPlayer, '$AudioPlayer');
mergeInto(LibraryManager.library, AudioPlayer);
