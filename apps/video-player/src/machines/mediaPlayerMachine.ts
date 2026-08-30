import { assign, enqueueActions, setup } from 'xstate';
import type { RefObject } from 'react';
import type { MediaPlayerError } from '@/utils/apiClient.ts';
import { MediaPlayerSynchronizer } from '@/utils/mediaPlayerUtils.ts';

type Event = { type: string };
type LoadedEvent = Event & { type: 'loaded'; playerRef: RefObject<HTMLVideoElement | null> };
type LoadEvent = Event & { type: 'load'; url: string; thumbnailUrl?: string };
type PauseEvent = Event & { type: 'pause' };
type ResumeEvent = Event & { type: 'resume' };
type RetryEvent = Event & { type: 'retry' };
type SetLoopedEvent = Event & { type: 'setLooped'; looped: boolean };
type SetMutedEvent = Event & { type: 'setMuted'; muted: boolean };
type SetLocalAdActiveEvent = Event & { type: 'setLocalAdActive'; localAdActive: boolean };
type SetDurationEvent = Event & { type: 'setDuration'; duration: number };
type SetVolumeEvent = Event & { type: 'setVolume'; volume: number };
type ErrorEvent = Event & { type: 'error'; error: MediaPlayerError };
type EndedEvent = Event & { type: 'ended' };
type SetPlaybackRateEvent = Event & { type: 'setPlaybackRate'; playbackRate: number };
type SeekToEvent = Event & { type: 'seekTo'; time: number };
type ResetEvent = Event & { type: 'reset' };
type SynchronizeTimeEvent = Event & { type: 'synchronizeTime'; serverTime: number };
type SetAdActiveEvent = Event & { type: 'setAdActive'; adActive: boolean };

export type MachineContext = {
	playing: boolean;
	loading: boolean;
	looped: boolean;
	error?: MediaPlayerError | null;
	volume: number;
	muted: boolean;
	localAdActive: boolean;
	adActive: boolean;
	playbackRate: number;
	duration: number;
	url: string;
	thumbnailUrl: string;
	time: number;
	playerRef?: RefObject<HTMLVideoElement | null>;
};

export type MachineEvents =
	| LoadedEvent
	| SetLoopedEvent
	| SetMutedEvent
	| SetLocalAdActiveEvent
	| SetVolumeEvent
	| PauseEvent
	| SetPlaybackRateEvent
	| SeekToEvent
	| ResumeEvent
	| RetryEvent
	| ErrorEvent
	| ResetEvent
	| SetDurationEvent
	| EndedEvent
	| LoadEvent
	| SynchronizeTimeEvent
	| SetAdActiveEvent;

const resetMediaPlayer = (context: MachineContext): MachineContext => ({
	...context,
	playing: true,
	loading: true,
	looped: false,
	volume: 1.0,
	error: null,
	muted: false,
	localAdActive: true,
	duration: -1,
	time: 0,
	playbackRate: 1,
	url: '',
	thumbnailUrl: '',
});

const resetTrack = (context: MachineContext): MachineContext => ({
	...context,
	loading: true,
	error: null,
	duration: -1,
	time: 0,
	url: '',
	thumbnailUrl: '',
});

const clampTime = (time: number, duration: number) => {
	if (Number.isFinite(duration) && duration > 0) return Math.min(Math.max(time, 0), duration);
	return Math.max(time, 0);
};

export const mediaPlayerMachine = setup({
	types: {
		context: {} as MachineContext,
		events: {} as MachineEvents,
	},
	actions: {
		resetMediaPlayer: assign(({ context }) => resetMediaPlayer(context)),
		resetTrack: assign(({ context }) => resetTrack(context)),
		restartTrack: enqueueActions(({ context, enqueue }) => {
			enqueue.assign({ time: 0 });
			enqueue(() => {
				const player = context.playerRef?.current;
				if (!player) return;

				player.currentTime = 0;

				void player.play()?.catch(() => {});
			});
		}),
		reportMediaLoaded: () => {},
		reportMediaDuration: () => {},
		reportMediaEnded: () => {},
		reportMediaError: () => {},
	},
	guards: {
		isLooped: ({ context }) => context.looped,
	},
}).createMachine({
	id: 'videoPlayer',
	initial: 'loading',
	context: {
		playing: true,
		loading: true,
		looped: false,
		duration: -1,
		volume: 1.0,
		error: null,
		muted: false,
		localAdActive: true,
		adActive: false,
		playbackRate: 1,
		time: 0,
		playerRef: undefined,
		url: '',
		thumbnailUrl: '',
	},
	on: {
		load: {
			// Next track could open with a YouTube ad, so the flag is re-armed until the detector
			// clears it again.
			actions: [
				assign({
					url: ({ event }) => event.url,
					thumbnailUrl: ({ event }) => event.thumbnailUrl ?? '',
					localAdActive: () => true,
				}),
			],
			target: '.loading',
		},
		reset: {
			actions: ['resetMediaPlayer'],
			target: '.loading',
		},
		error: {
			guard: ({ context }) => context.url !== '',
			actions: [
				assign({ error: ({ event }) => event.error }),
				{
					type: 'reportMediaError',
					params: ({ context }: { context: MachineContext }) => ({ error: context.error }),
				},
			],
			target: '.error',
		},
		setLooped: { actions: [assign({ looped: ({ event }) => event.looped })] },
		setMuted: { actions: [assign({ muted: ({ event }) => event.muted })] },
		setLocalAdActive: { actions: [assign({ localAdActive: ({ event }) => event.localAdActive })] },
		setAdActive: { actions: [assign({ adActive: ({ event }) => event.adActive })] },
		setVolume: {
			actions: [assign({ volume: ({ event }) => Math.min(Math.max(event.volume, 0), 1) })],
		},
		setPlaybackRate: {
			actions: [
				assign({
					playbackRate: ({ event, context }) =>
						Number.isFinite(event.playbackRate) && event.playbackRate > 0
							? event.playbackRate
							: context.playbackRate,
				}),
			],
		},
		setDuration: {
			guard: ({ event }) => Number.isFinite(event.duration) && event.duration > 0,
			actions: [assign({ duration: ({ event }) => event.duration }), 'reportMediaDuration'],
		},
		seekTo: {
			actions: [
				assign({ time: ({ event, context }) => clampTime(event.time, context.duration) }),
				// reads the already clamped `context.time`, not the raw event time
				({ context }) => {
					if (context.playerRef?.current) context.playerRef.current.currentTime = context.time;
				},
			],
		},
	},
	states: {
		loading: {
			entry: [assign({ loading: () => true, error: () => null })],
			exit: [assign({ loading: () => false })],
			on: {
				loaded: {
					actions: ['reportMediaLoaded', assign({ playerRef: ({ event }) => event.playerRef })],
					target: 'playing',
				},
			},
		},
		playing: {
			entry: [assign({ playing: () => true })],
			on: {
				ended: [{ guard: 'isLooped', actions: ['restartTrack'] }, { target: 'ended' }],
				pause: 'paused',
				synchronizeTime: {
					actions: [
						enqueueActions(({ context, event, enqueue }) => {
							const player = context.playerRef?.current;
							if (!player) return;

							let snappedTime: number | undefined;
							let nextPlaybackRate: number | undefined;

							MediaPlayerSynchronizer.sync({
								player,
								serverTime: event.serverTime,
								onPlaybackRateChange: (playbackRate) => {
									nextPlaybackRate = playbackRate;
								},
								onTimeChange: (time) => {
									snappedTime = time;
								},
							});

							if (snappedTime !== undefined) {
								const time = snappedTime;
								enqueue(() => {
									player.currentTime = time;
								});
							}

							enqueue.assign({
								playbackRate: nextPlaybackRate ?? context.playbackRate,
								time: snappedTime ?? player.currentTime,
							});
						}),
					],
				},
			},
		},
		paused: {
			entry: [assign({ playing: () => false })],
			on: {
				resume: 'playing',
				ended: [
					{ guard: 'isLooped', actions: ['restartTrack'], target: 'playing' },
					{ target: 'ended' },
				],
			},
		},
		ended: {
			entry: ['reportMediaEnded', 'resetTrack'],
			always: 'loading',
		},
		error: {
			on: {
				retry: 'loading',
			},
		},
	},
});
