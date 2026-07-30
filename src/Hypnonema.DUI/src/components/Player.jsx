import React, { useEffect, useRef, useState } from 'react';
import ReactPlayer from 'react-player';
import useNuiMessage from '../hooks/useNuiMessage';

const Player = () => {
    const player = useRef(null);
    const currentUrl = useRef('');
    const captionTimers = useRef([]);

    const [isVisible] = useState(true);
    const [url, setUrl] = useState('');
    const [resourceName, setResourceName] = useState('hypnonema');
    const [posterUrl, setPosterUrl] = useState('https://i.imgur.com/dPaIjEW.jpg');
    const [screenName, setScreenName] = useState('');
    const [playing, setPlaying] = useState(false);
    const [volume, setVolume] = useState(0.0);
    const [playerReady, setPlayerReady] = useState(false);
    const [muted] = useState(false);
    const [repeat, setRepeat] = useState(false);

    const clearCaptionTimers = () => {
        captionTimers.current.forEach(timer => clearTimeout(timer));
        captionTimers.current = [];
    };

    const disableYoutubeCaptions = () => {
        if (
            !player.current ||
            typeof player.current.getInternalPlayer !== 'function'
        ) {
            return;
        }

        const internalPlayer = player.current.getInternalPlayer();

        if (!internalPlayer) {
            return;
        }

        try {
            if (typeof internalPlayer.setOption === 'function') {
                internalPlayer.setOption('captions', 'track', {});
            }
        } catch (error) {
            console.log('Could not clear YouTube caption track', error);
        }

        try {
            if (typeof internalPlayer.unloadModule === 'function') {
                internalPlayer.unloadModule('captions');
            }
        } catch (error) {
            console.log('Could not unload YouTube captions', error);
        }
    };

    const scheduleDisableYoutubeCaptions = () => {
        clearCaptionTimers();

        [0, 250, 500, 1000, 2000, 4000, 8000].forEach(delay => {
            const timer = setTimeout(() => {
                disableYoutubeCaptions();
            }, delay);

            captionTimers.current.push(timer);
        });
    };

    useEffect(() => {
        return () => {
            clearCaptionTimers();
        };
    }, []);

    const onPlayMessage = ({ payload }) => {
        if (currentUrl.current !== payload) {
            currentUrl.current = payload;
            setPlayerReady(false);
        }

        setUrl(payload);
        setPlaying(true);

        setTimeout(() => {
            scheduleDisableYoutubeCaptions();
        }, 0);
    };

    const onStop = () => {
        currentUrl.current = '';
        setPlayerReady(false);
        setPlaying(false);
        setUrl('');
    };

    const onInit = ({ payload }) => {
        const {
            resourceName: initializedResourceName,
            screenName: initializedScreenName,
            posterUrl: initializedPosterUrl
        } = payload;

        setScreenName(initializedScreenName);
        setResourceName(initializedResourceName);
        setPosterUrl(initializedPosterUrl);
    };

    const onPause = () => {
        setPlaying(false);
    };

    const onResume = () => {
        setPlaying(true);

        setTimeout(() => {
            scheduleDisableYoutubeCaptions();
        }, 0);
    };

    const onSeek = ({ payload }) => {
        if (player.current) {
            player.current.seekTo(payload, 'seconds');
        }

        scheduleDisableYoutubeCaptions();
    };

    const onRepeat = ({ payload }) => {
        setRepeat(payload);
    };

    const onVolume = ({ payload }) => {
        const vol = parseFloat(payload);

        setVolume(vol);
    };

    const onSynchronizeState = ({ payload }) => {
        const {
            url: synchronizedUrl,
            paused,
            currentTime,
            repeat: synchronizedRepeat
        } = payload;

        if (currentUrl.current !== synchronizedUrl) {
            currentUrl.current = synchronizedUrl;
            setPlayerReady(false);
        }

        setUrl(synchronizedUrl);
        setPlaying(!paused);
        setRepeat(synchronizedRepeat);

        setTimeout(() => {
            if (player.current) {
                player.current.seekTo(currentTime, 'seconds');
            }

            scheduleDisableYoutubeCaptions();
        }, 0);
    };

    const onReady = () => {
        setPlayerReady(true);
        scheduleDisableYoutubeCaptions();

        try {
            if (
                !player.current ||
                typeof player.current.getInternalPlayer !== 'function'
            ) {
                return;
            }

            const internalPlayer = player.current.getInternalPlayer();

            if (
                internalPlayer &&
                internalPlayer._iframe &&
                internalPlayer._iframe.contentWindow
            ) {
                const button =
                    internalPlayer._iframe.contentWindow.document.querySelector(
                        'button[data-a-target="player-overlay-mature-accept"]'
                    );

                if (button) {
                    button.click();
                }
            }
        } catch (error) {
            console.log(
                'Could not handle Twitch mature-content overlay',
                error
            );
        }
    };

    const onStart = () => {
        scheduleDisableYoutubeCaptions();

        sendDuiResponse('playbackStart', {
            screenName,
            date: new Date().toISOString()
        }).then(() => { });
    };

    const onPlayerPlay = () => {
        scheduleDisableYoutubeCaptions();
    };

    const onEnded = () => {
        clearCaptionTimers();

        setTimeout(() => {
            sendDuiResponse('playbackEnded', { screenName }).then(() => { });
        }, 2500);
    };

    const onDuration = duration => {
        sendDuiResponse('updateStateDuration', {
            screenName,
            duration
        }).then(() => { });
    };

    useNuiMessage('synchronizeState', onSynchronizeState);
    useNuiMessage('volume', onVolume);
    useNuiMessage('repeat', onRepeat);
    useNuiMessage('seek', onSeek);
    useNuiMessage('resume', onResume);
    useNuiMessage('play', onPlayMessage);
    useNuiMessage('stop', onStop);
    useNuiMessage('init', onInit);
    useNuiMessage('pause', onPause);

    const sendDuiResponse = (nuiCallback, body) => {
        const responseUrl = new URL(
            nuiCallback,
            `${window.location.protocol}${resourceName}`
        ).toString();

        return fetch(responseUrl, {
            headers: {
                'content-type': 'application/json; charset=UTF-8'
            },
            method: 'POST',
            body: JSON.stringify(body)
        }).catch(error => console.log(error));
    };

    if (!isVisible) {
        return null;
    }

    return (
        <div style={{ width: '100%', height: '100%' }}>
            {!playing &&
                <div id='posterImg'>
                    <img src={posterUrl} alt='' />
                </div>
            }

            <div className='player-wrapper'>
                <ReactPlayer
                    ref={player}
                    className='react-player'
                    url={url}
                    pip={false}
                    playing={playerReady && playing}
                    controls={false}
                    loop={repeat}
                    playbackRate={1.0}
                    volume={playerReady ? volume : null}
                    muted={muted}
                    onDuration={onDuration}
                    onReady={onReady}
                    onPlay={onPlayerPlay}
                    onEnded={onEnded}
                    onStart={onStart}
                    onError={error =>
                        console.log('onError', JSON.stringify(error))
                    }
                    config={{
                        youtube: {
                            playerVars: {
                                controls: 0,
                                disablekb: 1,
                                fs: 0,
                                iv_load_policy: 3,
                                playsinline: 1,
                                rel: 0,
                                cc_load_policy: 0
                            }
                        },
                        file: {
                            attributes: {
                                controlsList: 'nodownload noplaybackrate',
                                disablePictureInPicture: true
                            }
                        }
                    }}
                    width='100%'
                    height='100%'
                />
            </div>
        </div>
    );
};

export default Player;