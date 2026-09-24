using System;
using Hypnonema.Client.Dui;
using Hypnonema.Shared.Media.Audio;

namespace Hypnonema.Client.Media.Audio;

public class AudioSystemBuilder
{
    public static IAudioSystem CreateAudioSystem(Browser browser, MediaPlayer mediaPlayer)
    {
        return mediaPlayer.Audio switch
        {
            DefaultAudioConfiguration { Attenuation: var attenuation } =>
                new AudioAttenuationSystem(browser, mediaPlayer, attenuation),

            _ => throw new ArgumentException(
                $"Audio mode '{mediaPlayer.Audio.Mode}' is not supported.",
                nameof(mediaPlayer.Audio))
        };
    }
}