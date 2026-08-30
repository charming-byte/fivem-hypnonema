import type { VideoCallbacks } from '@/hooks/useMediaPlayerMachine.ts';
import type { mediaPlayerMachine } from '@/machines/mediaPlayerMachine.ts';
import type { SnapshotFrom } from 'xstate';
import ReactPlayer from 'react-player';
import { SyntheticEvent, useRef } from 'react';
import { AdOverlay } from '@/components/AdOverlay';

interface MediaPlayerProps {
	state: SnapshotFrom<typeof mediaPlayerMachine>;
	videoCallbacks: VideoCallbacks;
}

export const MediaPlayer = ({ state, videoCallbacks }: MediaPlayerProps) => {
	const playerRef = useRef<HTMLVideoElement | null>(null);

	const handleDurationChange = () => {
		const player = playerRef.current;
		if (!player || player.duration == 0) return;

		videoCallbacks.onDuration(player.duration);
	};

	const handleError = (_: SyntheticEvent<HTMLVideoElement>) => {
		const player = playerRef.current;
		if (!player) return;

		const message = player.error?.message || 'There was an error while playing video';
		const code = player.error?.code || -1;

		videoCallbacks.onError({
			message,
			code,
		});
	};

	// The server's pause only holds the shared content clock. A local ad has to keep running
	// through it: pausing the player freezes the ad too, so its skip countdown never elapses and
	// the ad never ends — which is exactly what the ad quorum is waiting for.
	const playing = state.context.playing || state.context.localAdActive;

	// @ts-ignore
	return (
		<div style={{ position: 'relative', width: '100%', height: '100%' }}>
			<ReactPlayer
				style={{ aspectRatio: '16/9' }}
				ref={playerRef}
				className="react-player"
				width="100%"
				height="100%"
				controls={false}
				src={state.context.url}
				muted={state.context.muted || state.context.localAdActive}
				volume={state.context.volume}
				playing={playing}
				playbackRate={state.context.playbackRate}
				onEnded={videoCallbacks.onEnded}
				onError={handleError}
				onDurationChange={handleDurationChange}
				onProgress={() => videoCallbacks.onStart(playerRef)}
				config={{
					youtube: {
						referrerpolicy: 'strict-origin-when-cross-origin',
						enablejsapi: 1,
						fs: 0,
						//@ts-expect-error not documented in youtube config but is a valid option
						cc_load_policy: 3,
						rel: 0,
						iv_load_policy: 3,
					},
				}}
			/>
			<AdOverlay active={state.context.adActive} thumbnailUrl={state.context.thumbnailUrl} />
		</div>
	);
};
