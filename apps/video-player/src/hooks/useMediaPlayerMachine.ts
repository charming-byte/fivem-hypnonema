import { useMachine } from '@xstate/react';
import type { RefObject } from 'react';
import { DuiEvents, YoutubeAdStatus } from '@hypnonema/generated-types';
import {
	type APIClient,
	type MediaPlayerError,
	ignoreReportingFailure,
} from '@/utils/apiClient.ts';
import { mediaPlayerMachine } from '@/machines/mediaPlayerMachine.ts';
import { MATCH_URL_YOUTUBE, prepareMediaPlayer } from '@/utils/mediaPlayerUtils.ts';
import { useDuiEvent } from './useDuiEvent.ts';

export type VideoCallbacks = {
	onStart: (playerRef: RefObject<HTMLVideoElement | null>) => void;
	onError: (error: MediaPlayerError) => void;
	onEnded: () => void;
	onDuration: (duration: number) => void;
};

export const useMediaPlayerMachine = (apiClient?: APIClient) => {
	const [snapshot, send] = useMachine(
		mediaPlayerMachine.provide({
			actions: {
				reportMediaError: ({ context }) => {
					const error = context.error || { message: 'Unknown error', code: -1 };

					void apiClient?.sendMediaPlayerError(error).catch(ignoreReportingFailure);
				},
				reportMediaLoaded: () =>
					void apiClient?.sendMediaPlayerLoaded().catch(ignoreReportingFailure),
				reportMediaDuration: ({ context }) =>
					void apiClient?.sendPlayerDuration(context.duration).catch(ignoreReportingFailure),
				reportMediaEnded: () =>
					void apiClient?.sendMediaPlayerEnded().catch(ignoreReportingFailure),
			},
		}),
	);

	useDuiEvent<boolean>(DuiEvents.setLooped, (looped) => send({ type: 'setLooped', looped }));
	useDuiEvent<boolean>(DuiEvents.setMuted, (muted) => send({ type: 'setMuted', muted }));
	useDuiEvent<boolean>(DuiEvents.setPaused, (paused) =>
		send({ type: paused ? 'pause' : 'resume' }),
	);
	useDuiEvent<boolean>(DuiEvents.setAdActive, (adActive) =>
		send({ type: 'setAdActive', adActive }),
	);
	useDuiEvent<number>(DuiEvents.setVolume, (volume) => send({ type: 'setVolume', volume }));
	useDuiEvent(DuiEvents.reset, () => send({ type: 'reset' }));
	useDuiEvent<{ url: string; thumbnailUrl: string }>(DuiEvents.load, (payload) =>
		send({ type: 'load', url: payload.url, thumbnailUrl: payload.thumbnailUrl }),
	);
	useDuiEvent<number>(DuiEvents.seek, (time) => send({ type: 'seekTo', time }));
	useDuiEvent<number>(DuiEvents.synchronizeTime, (serverTime) =>
		send({ type: 'synchronizeTime', serverTime }),
	);

	const videoCallbacks: VideoCallbacks = {
		onError: (error: MediaPlayerError) => send({ type: 'error', error }),
		onEnded: () => send({ type: 'ended' }),
		onDuration: (duration: number) => send({ type: 'setDuration', duration }),
		onStart: (playerRef: RefObject<HTMLVideoElement | null>) => {
			// Non-YouTube sources never report `onYoutubeAdStatus`, so the default
			// `localAdActive: true` would otherwise leave them muted and unpausable forever.
			if (!MATCH_URL_YOUTUBE.test(playerRef.current?.src ?? '')) {
				send({ type: 'setLocalAdActive', localAdActive: false });
			}

			prepareMediaPlayer(playerRef, {
				onYoutubeAdStatus: (status) => {
					apiClient?.sendYoutubeAdStatus(status);
					send({ type: 'setLocalAdActive', localAdActive: status !== YoutubeAdStatus.Inactive });
				},
				onRequestClick: (x, y) => apiClient?.sendClickRequest(x, y),
			});
			send({ type: 'loaded', playerRef });
		},
	};

	return { snapshot, videoCallbacks };
};
