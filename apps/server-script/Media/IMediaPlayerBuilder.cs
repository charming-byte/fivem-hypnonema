using Hypnonema.Shared.Media;

namespace Hypnonema.Server.Media;

public interface IMediaPlayerBuilder
{
    IMediaPlayer Create(ITrack track, Target target);
}