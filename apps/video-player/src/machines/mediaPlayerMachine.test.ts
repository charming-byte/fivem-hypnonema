import { afterEach, describe, expect, it, vi } from 'vitest';
import { createActor } from 'xstate';
import { MediaPlayerSynchronizer } from '@/utils/mediaPlayerUtils.ts';
import { mediaPlayerMachine } from './mediaPlayerMachine.ts';

const URL = 'http://example.com/video.mp4';

// `error` is ignored while no media is loaded, so most error tests need a url first.
const startLoaded = (machine = mediaPlayerMachine, playerRef: unknown = { current: null }) => {
	const actor = createActor(machine).start();
	actor.send({ type: 'load', url: URL });
	actor.send({ type: 'loaded', playerRef: playerRef as never });
	return actor;
};

afterEach(() => {
	vi.restoreAllMocks();
});

describe('mediaPlayerMachine', () => {
	describe('initial state', () => {
		it('starts in loading with default context', () => {
			const actor = createActor(mediaPlayerMachine).start();
			expect(actor.getSnapshot().value).toBe('loading');
			expect(actor.getSnapshot().context).toEqual({
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
			});
		});
	});

	describe('loading -> playing', () => {
		it('transitions to playing on loaded, storing the playerRef and clearing loading', () => {
			const actor = createActor(mediaPlayerMachine).start();
			const playerRef = { current: null };
			actor.send({ type: 'loaded', playerRef });
			expect(actor.getSnapshot().value).toBe('playing');
			expect(actor.getSnapshot().context.playerRef).toBe(playerRef);
			expect(actor.getSnapshot().context.loading).toBe(false);
			expect(actor.getSnapshot().context.playing).toBe(true);
		});

		it('invokes reportMediaLoaded when loaded is handled', () => {
			const reportMediaLoaded = vi.fn();
			const machine = mediaPlayerMachine.provide({ actions: { reportMediaLoaded } });
			const actor = createActor(machine).start();
			actor.send({ type: 'loaded', playerRef: { current: null } });
			expect(reportMediaLoaded).toHaveBeenCalledTimes(1);
		});

		it('pause/ended events sent while still loading are ignored', () => {
			const actor = createActor(mediaPlayerMachine).start();
			actor.send({ type: 'pause' });
			expect(actor.getSnapshot().value).toBe('loading');
			expect(actor.getSnapshot().context.playing).toBe(true);

			actor.send({ type: 'ended' });
			expect(actor.getSnapshot().value).toBe('loading');
		});
	});

	describe('error handling', () => {
		it('transitions to error on error event, storing the error and clearing loading', () => {
			const actor = createActor(mediaPlayerMachine).start();
			actor.send({ type: 'load', url: URL });
			const error = { code: 1, message: 'Failed to load media' } as MediaError;
			actor.send({ type: 'error', error });
			expect(actor.getSnapshot().value).toBe('error');
			expect(actor.getSnapshot().context.error).toEqual(error);
			expect(actor.getSnapshot().context.loading).toBe(false);
		});

		it('invokes reportMediaError with the stored error as params', () => {
			const reportMediaError = vi.fn();
			const machine = mediaPlayerMachine.provide({ actions: { reportMediaError } });
			const actor = createActor(machine).start();
			actor.send({ type: 'load', url: URL });
			const error = { code: 3, message: 'boom' } as MediaError;
			actor.send({ type: 'error', error });
			expect(reportMediaError).toHaveBeenCalledTimes(1);
			expect(reportMediaError.mock.calls[0]?.[1]).toEqual({ error });
		});

		it.each(['playing', 'paused'] as const)(
			'is handled while in "%s", not just while loading',
			(target) => {
				const reportMediaError = vi.fn();
				const machine = mediaPlayerMachine.provide({ actions: { reportMediaError } });
				const actor = startLoaded(machine);
				if (target === 'paused') actor.send({ type: 'pause' });
				expect(actor.getSnapshot().value).toBe(target);

				const error = { code: 2, message: 'connection lost' } as MediaError;
				actor.send({ type: 'error', error });

				expect(actor.getSnapshot().value).toBe('error');
				expect(actor.getSnapshot().context.error).toEqual(error);
				expect(reportMediaError).toHaveBeenCalledTimes(1);
			},
		);

		it('ignores errors while no url is loaded, so an empty src is not reported', () => {
			const reportMediaError = vi.fn();
			const machine = mediaPlayerMachine.provide({ actions: { reportMediaError } });
			const actor = createActor(machine).start();

			actor.send({
				type: 'error',
				error: { code: 4, message: 'Empty src attribute' } as MediaError,
			});

			expect(actor.getSnapshot().value).toBe('loading');
			expect(actor.getSnapshot().context.error).toBe(null);
			expect(reportMediaError).not.toHaveBeenCalled();
		});

		it('ignores the error that an ended track provokes once its url was cleared', () => {
			const reportMediaError = vi.fn();
			const machine = mediaPlayerMachine.provide({ actions: { reportMediaError } });
			const actor = startLoaded(machine);
			actor.send({ type: 'ended' });
			expect(actor.getSnapshot().context.url).toBe('');

			actor.send({
				type: 'error',
				error: { code: 4, message: 'Empty src attribute' } as MediaError,
			});

			expect(actor.getSnapshot().value).toBe('loading');
			expect(reportMediaError).not.toHaveBeenCalled();
		});

		it('retry sends the machine back to loading, clearing the error and restoring loading', () => {
			const actor = createActor(mediaPlayerMachine).start();
			actor.send({ type: 'load', url: URL });
			actor.send({ type: 'error', error: { code: 2, message: 'Error' } as MediaError });
			actor.send({ type: 'retry' });
			expect(actor.getSnapshot().value).toBe('loading');
			expect(actor.getSnapshot().context.loading).toBe(true);
			expect(actor.getSnapshot().context.error).toBe(null);
		});
	});

	describe('playing <-> paused', () => {
		it('pause moves to paused and sets playing=false; resume moves back and sets playing=true', () => {
			const actor = startLoaded();

			actor.send({ type: 'pause' });
			expect(actor.getSnapshot().value).toBe('paused');
			expect(actor.getSnapshot().context.playing).toBe(false);

			actor.send({ type: 'resume' });
			expect(actor.getSnapshot().value).toBe('playing');
			expect(actor.getSnapshot().context.playing).toBe(true);
		});

		it('playing -> ended', () => {
			const actor = startLoaded();
			actor.send({ type: 'ended' });
			expect(actor.getSnapshot().value).toBe('loading');
		});

		it('handles ended from paused too, since the media element pauses before it ends', () => {
			const reportMediaEnded = vi.fn();
			const machine = mediaPlayerMachine.provide({ actions: { reportMediaEnded } });
			const actor = startLoaded(machine);

			actor.send({ type: 'pause' });
			actor.send({ type: 'ended' });

			expect(reportMediaEnded).toHaveBeenCalledTimes(1);
			expect(actor.getSnapshot().value).toBe('loading');
		});
	});

	describe('ended state', () => {
		it('reports the end once and falls through to loading', () => {
			const reportMediaEnded = vi.fn();
			const machine = mediaPlayerMachine.provide({ actions: { reportMediaEnded } });
			const actor = startLoaded(machine);

			actor.send({ type: 'ended' });

			expect(reportMediaEnded).toHaveBeenCalledTimes(1);
			expect(actor.getSnapshot().value).toBe('loading');
			expect(actor.getSnapshot().context.loading).toBe(true);
		});

		it('clears the track but keeps the settings the client will not re-send', () => {
			const playerRef = { current: { src: URL, currentTime: 5 } };
			const actor = startLoaded(mediaPlayerMachine, playerRef);
			actor.send({ type: 'setVolume', volume: 0.3 });
			actor.send({ type: 'setMuted', muted: true });
			actor.send({ type: 'setPlaybackRate', playbackRate: 1.5 });
			actor.send({ type: 'setDuration', duration: 120 });
			actor.send({ type: 'seekTo', time: 30 });

			actor.send({ type: 'ended' });

			const { context } = actor.getSnapshot();
			// track scoped fields are cleared
			expect(context.url).toBe('');
			expect(context.time).toBe(0);
			expect(context.duration).toBe(-1);
			expect(context.error).toBe(null);
			// settings survive, the client only pushes those on change
			expect(context.volume).toBe(0.3);
			expect(context.muted).toBe(true);
			expect(context.playbackRate).toBe(1.5);
			// the playerRef object itself is preserved
			expect(context.playerRef).toBe(playerRef);
		});

		it('waits in loading for the next track and ignores playback events meanwhile', () => {
			const actor = startLoaded();
			actor.send({ type: 'ended' });

			actor.send({ type: 'pause' });
			expect(actor.getSnapshot().value).toBe('loading');
		});

		it('still responds to globally-registered events like setVolume', () => {
			const actor = startLoaded();
			actor.send({ type: 'ended' });
			actor.send({ type: 'setVolume', volume: 0.7 });
			expect(actor.getSnapshot().value).toBe('loading');
			expect(actor.getSnapshot().context.volume).toBe(0.7);
		});

		it('accepts the next track via load', () => {
			const actor = startLoaded();
			actor.send({ type: 'ended' });
			actor.send({ type: 'load', url: 'http://new/url.mp4' });
			expect(actor.getSnapshot().value).toBe('loading');
			expect(actor.getSnapshot().context.url).toBe('http://new/url.mp4');
		});
	});

	describe('looping', () => {
		// The media element never gets a `loop` attribute (toggling it re-creates the iframe of the
		// provider players), so the machine has to restart the track itself on `ended`.
		const loopingActor = (element: { currentTime: number; play: () => unknown }) => {
			const reportMediaEnded = vi.fn();
			const machine = mediaPlayerMachine.provide({ actions: { reportMediaEnded } });
			const actor = startLoaded(machine, { current: element });
			actor.send({ type: 'setLooped', looped: true });
			return { actor, reportMediaEnded };
		};

		it('restarts the track in place instead of ending it', () => {
			const play = vi.fn();
			const element = { currentTime: 42, play };
			const { actor, reportMediaEnded } = loopingActor(element);

			actor.send({ type: 'ended' });

			expect(actor.getSnapshot().value).toBe('playing');
			expect(element.currentTime).toBe(0);
			expect(play).toHaveBeenCalledTimes(1);
			// the track is kept, so the client never has to re-send the url
			expect(actor.getSnapshot().context.url).toBe(URL);
			expect(actor.getSnapshot().context.time).toBe(0);
			expect(reportMediaEnded).not.toHaveBeenCalled();
		});

		it('keeps looping for as long as looped stays set', () => {
			const play = vi.fn();
			const { actor, reportMediaEnded } = loopingActor({ currentTime: 10, play });

			actor.send({ type: 'ended' });
			actor.send({ type: 'ended' });
			actor.send({ type: 'ended' });

			expect(play).toHaveBeenCalledTimes(3);
			expect(actor.getSnapshot().value).toBe('playing');
			expect(reportMediaEnded).not.toHaveBeenCalled();
		});

		it('resumes a track that ended while paused, since only a running track can end', () => {
			const play = vi.fn();
			const element = { currentTime: 42, play };
			const { actor } = loopingActor(element);

			actor.send({ type: 'pause' });
			actor.send({ type: 'ended' });

			expect(actor.getSnapshot().value).toBe('playing');
			expect(actor.getSnapshot().context.playing).toBe(true);
			expect(element.currentTime).toBe(0);
		});

		it('ends normally again once looping is turned off', () => {
			const play = vi.fn();
			const { actor, reportMediaEnded } = loopingActor({ currentTime: 10, play });

			actor.send({ type: 'setLooped', looped: false });
			actor.send({ type: 'ended' });

			expect(actor.getSnapshot().value).toBe('loading');
			expect(actor.getSnapshot().context.url).toBe('');
			expect(reportMediaEnded).toHaveBeenCalledTimes(1);
			expect(play).not.toHaveBeenCalled();
		});

		it('does not throw when the track ends before a player is attached', () => {
			const actor = createActor(mediaPlayerMachine).start();
			actor.send({ type: 'load', url: URL });
			actor.send({ type: 'loaded', playerRef: { current: null } });
			actor.send({ type: 'setLooped', looped: true });

			expect(() => actor.send({ type: 'ended' })).not.toThrow();
			expect(actor.getSnapshot().value).toBe('playing');
		});

		it('survives a play() that the provider rejects', () => {
			const play = vi.fn(() => Promise.reject(new Error('not allowed')));
			const { actor } = loopingActor({ currentTime: 10, play });

			expect(() => actor.send({ type: 'ended' })).not.toThrow();
			expect(actor.getSnapshot().value).toBe('playing');
		});
	});

	describe('global events (registered on the root, available regardless of current state)', () => {
		it.each([
			['loading', 'loading'],
			['playing', 'playing'],
			['paused', 'paused'],
			['error', 'error'],
			// `ended` is transient and immediately falls through to `loading`
			['ended', 'loading'],
		] as const)(
			'setLooped/setMuted/setLocalAdActive/setVolume/setPlaybackRate/seekTo/setDuration work from "%s"',
			(setup, expected) => {
				const actor = createActor(mediaPlayerMachine).start();
				actor.send({ type: 'load', url: URL });
				if (setup === 'error') {
					actor.send({ type: 'error', error: { code: 1, message: 'x' } as MediaError });
				} else if (setup !== 'loading') {
					actor.send({ type: 'loaded', playerRef: { current: null } });
					if (setup === 'paused') actor.send({ type: 'pause' });
					if (setup === 'ended') actor.send({ type: 'ended' });
				}

				actor.send({ type: 'setLooped', looped: true });
				actor.send({ type: 'setMuted', muted: true });
				actor.send({ type: 'setLocalAdActive', localAdActive: false });
				actor.send({ type: 'setVolume', volume: 0.42 });
				actor.send({ type: 'setPlaybackRate', playbackRate: 2 });
				actor.send({ type: 'seekTo', time: 10 });
				actor.send({ type: 'setDuration', duration: 99 });

				expect(actor.getSnapshot().value).toBe(expected);
				expect(actor.getSnapshot().context.looped).toBe(true);
				expect(actor.getSnapshot().context.muted).toBe(true);
				expect(actor.getSnapshot().context.localAdActive).toBe(false);
				expect(actor.getSnapshot().context.volume).toBe(0.42);
				expect(actor.getSnapshot().context.playbackRate).toBe(2);
				expect(actor.getSnapshot().context.time).toBe(10);
				expect(actor.getSnapshot().context.duration).toBe(99);
			},
		);

		it('invokes reportMediaDuration when setDuration is handled', () => {
			const reportMediaDuration = vi.fn();
			const machine = mediaPlayerMachine.provide({ actions: { reportMediaDuration } });
			const actor = createActor(machine).start();
			actor.send({ type: 'setDuration', duration: 120 });
			expect(reportMediaDuration).toHaveBeenCalledTimes(1);
			expect(actor.getSnapshot().context.duration).toBe(120);
		});
	});

	describe('synchronizeTime', () => {
		it('is ignored outside of playing, so a paused track is not dragged forward', () => {
			const sync = vi.spyOn(MediaPlayerSynchronizer, 'sync');
			const actor = startLoaded(mediaPlayerMachine, { current: { currentTime: 3 } });
			actor.send({ type: 'pause' });

			actor.send({ type: 'synchronizeTime', serverTime: 100 });

			expect(sync).not.toHaveBeenCalled();
			expect(actor.getSnapshot().value).toBe('paused');
			expect(actor.getSnapshot().context.time).toBe(0);
		});

		it('is a no-op when no player is attached', () => {
			const sync = vi.spyOn(MediaPlayerSynchronizer, 'sync');
			const actor = startLoaded();

			expect(() => actor.send({ type: 'synchronizeTime', serverTime: 100 })).not.toThrow();
			expect(sync).not.toHaveBeenCalled();
			expect(actor.getSnapshot().context.time).toBe(0);
		});

		it('applies a hard snap to both the context and the media element', () => {
			vi.spyOn(MediaPlayerSynchronizer, 'sync').mockImplementation(({ onTimeChange }) =>
				onTimeChange?.(50),
			);
			const element = { currentTime: 3 };
			const actor = startLoaded(mediaPlayerMachine, { current: element });

			actor.send({ type: 'synchronizeTime', serverTime: 50 });

			expect(element.currentTime).toBe(50);
			expect(actor.getSnapshot().context.time).toBe(50);
		});

		it('applies a playback rate correction and tracks the current time of the element', () => {
			vi.spyOn(MediaPlayerSynchronizer, 'sync').mockImplementation(({ onPlaybackRateChange }) =>
				onPlaybackRateChange?.(1.05),
			);
			const element = { currentTime: 12 };
			const actor = startLoaded(mediaPlayerMachine, { current: element });

			actor.send({ type: 'synchronizeTime', serverTime: 12.2 });

			expect(actor.getSnapshot().context.playbackRate).toBe(1.05);
			expect(actor.getSnapshot().context.time).toBe(12);
			// no hard snap, the element is left alone
			expect(element.currentTime).toBe(12);
		});

		it('passes the server time through to the synchronizer', () => {
			const sync = vi.spyOn(MediaPlayerSynchronizer, 'sync').mockImplementation(() => {});
			const element = { currentTime: 1 };
			const actor = startLoaded(mediaPlayerMachine, { current: element });

			actor.send({ type: 'synchronizeTime', serverTime: 42 });

			expect(sync).toHaveBeenCalledTimes(1);
			expect(sync.mock.calls[0]?.[0]).toMatchObject({ player: element, serverTime: 42 });
		});
	});

	describe('seekTo', () => {
		it('updates both context.time and the current playerRef element when one is attached', () => {
			const element = { currentTime: 0 };
			const actor = createActor(mediaPlayerMachine).start();
			actor.send({ type: 'loaded', playerRef: { current: element } as never });
			actor.send({ type: 'seekTo', time: 42 });
			expect(actor.getSnapshot().context.time).toBe(42);
			expect(element.currentTime).toBe(42);
		});

		it('updates context.time without throwing when no playerRef is attached', () => {
			const actor = createActor(mediaPlayerMachine).start();
			expect(() => actor.send({ type: 'seekTo', time: 15 })).not.toThrow();
			expect(actor.getSnapshot().context.time).toBe(15);
		});

		it('clamps to the known duration, on the element as well as in the context', () => {
			const element = { currentTime: 0 };
			const actor = createActor(mediaPlayerMachine).start();
			actor.send({ type: 'loaded', playerRef: { current: element } as never });
			actor.send({ type: 'setDuration', duration: 60 });

			actor.send({ type: 'seekTo', time: 120 });
			expect(actor.getSnapshot().context.time).toBe(60);
			expect(element.currentTime).toBe(60);

			actor.send({ type: 'seekTo', time: -10 });
			expect(actor.getSnapshot().context.time).toBe(0);
			expect(element.currentTime).toBe(0);
		});

		it('clamps only the lower bound while the duration is still unknown', () => {
			const actor = createActor(mediaPlayerMachine).start();
			actor.send({ type: 'seekTo', time: -5 });
			expect(actor.getSnapshot().context.time).toBe(0);

			actor.send({ type: 'seekTo', time: 9999 });
			expect(actor.getSnapshot().context.time).toBe(9999);
		});
	});

	describe('input validation', () => {
		it.each([
			[-0.5, 0],
			[1.5, 1],
			[0.42, 0.42],
		])('clamps a volume of %s to %s', (input, expected) => {
			const actor = createActor(mediaPlayerMachine).start();
			actor.send({ type: 'setVolume', volume: input });
			expect(actor.getSnapshot().context.volume).toBe(expected);
		});

		it.each([0, -1, NaN, Infinity])('keeps the previous playback rate for %s', (input) => {
			const actor = createActor(mediaPlayerMachine).start();
			actor.send({ type: 'setPlaybackRate', playbackRate: 1.25 });
			actor.send({ type: 'setPlaybackRate', playbackRate: input });
			expect(actor.getSnapshot().context.playbackRate).toBe(1.25);
		});

		it.each([0, -10, NaN, Infinity])('rejects a duration of %s without reporting it', (input) => {
			const reportMediaDuration = vi.fn();
			const machine = mediaPlayerMachine.provide({ actions: { reportMediaDuration } });
			const actor = createActor(machine).start();

			actor.send({ type: 'setDuration', duration: input });

			expect(actor.getSnapshot().context.duration).toBe(-1);
			expect(reportMediaDuration).not.toHaveBeenCalled();
		});
	});

	describe('load', () => {
		it('updates url and (re-)enters "loading" from any state', () => {
			const actor = createActor(mediaPlayerMachine).start();
			actor.send({ type: 'loaded', playerRef: { current: null } });
			actor.send({ type: 'load', url: URL });
			expect(actor.getSnapshot().value).toBe('loading');
			expect(actor.getSnapshot().context.url).toBe(URL);
		});

		it('keeps loading=true when load is sent while already loading', () => {
			const actor = createActor(mediaPlayerMachine).start();
			expect(actor.getSnapshot().context.loading).toBe(true);
			actor.send({ type: 'load', url: URL });
			expect(actor.getSnapshot().value).toBe('loading');
			expect(actor.getSnapshot().context.loading).toBe(true);
		});

		it('restores loading=true when a new url is loaded mid-playback', () => {
			const actor = createActor(mediaPlayerMachine).start();
			actor.send({ type: 'loaded', playerRef: { current: null } });
			expect(actor.getSnapshot().context.loading).toBe(false);
			actor.send({ type: 'load', url: 'http://example.com/other.mp4' });
			expect(actor.getSnapshot().value).toBe('loading');
			expect(actor.getSnapshot().context.loading).toBe(true);
		});

		it('clears a stale error from the previous track', () => {
			const actor = createActor(mediaPlayerMachine).start();
			actor.send({ type: 'load', url: URL });
			actor.send({ type: 'error', error: { code: 5, message: 'x' } as MediaError });
			expect(actor.getSnapshot().context.error).not.toBe(null);

			actor.send({ type: 'load', url: 'http://example.com/other.mp4' });
			expect(actor.getSnapshot().context.error).toBe(null);
		});

		it('re-arms localAdActive for the next track, since it could be a YouTube ad', () => {
			const actor = createActor(mediaPlayerMachine).start();
			actor.send({ type: 'loaded', playerRef: { current: null } });
			actor.send({ type: 'setLocalAdActive', localAdActive: false });
			expect(actor.getSnapshot().context.localAdActive).toBe(false);

			actor.send({ type: 'load', url: 'http://example.com/other.mp4' });
			expect(actor.getSnapshot().context.localAdActive).toBe(true);
		});
	});

	describe('reset', () => {
		it('restores the full default context and moves to "loading"', () => {
			const actor = startLoaded();
			actor.send({ type: 'setLooped', looped: true });
			actor.send({ type: 'setMuted', muted: true });
			actor.send({ type: 'setVolume', volume: 0.2 });
			actor.send({ type: 'setPlaybackRate', playbackRate: 1.5 });
			actor.send({ type: 'setDuration', duration: 200 });
			actor.send({ type: 'seekTo', time: 30 });

			actor.send({ type: 'reset' });

			expect(actor.getSnapshot().value).toBe('loading');
			expect(actor.getSnapshot().context.looped).toBe(false);
			expect(actor.getSnapshot().context.muted).toBe(false);
			expect(actor.getSnapshot().context.volume).toBe(1.0);
			expect(actor.getSnapshot().context.playbackRate).toBe(1);
			expect(actor.getSnapshot().context.time).toBe(0);
			expect(actor.getSnapshot().context.duration).toBe(-1);
			expect(actor.getSnapshot().context.url).toBe('');
			expect(actor.getSnapshot().context.loading).toBe(true);
			expect(actor.getSnapshot().context.error).toBe(null);
		});

		it('clears a stored error and works from the error state', () => {
			const actor = createActor(mediaPlayerMachine).start();
			actor.send({ type: 'load', url: URL });
			actor.send({ type: 'error', error: { code: 5, message: 'x' } as MediaError });
			actor.send({ type: 'reset' });
			expect(actor.getSnapshot().value).toBe('loading');
			expect(actor.getSnapshot().context.error).toBe(null);
		});

		it('does not touch an already-attached playerRef', () => {
			const playerRef = { current: null };
			const actor = createActor(mediaPlayerMachine).start();
			actor.send({ type: 'loaded', playerRef });
			actor.send({ type: 'reset' });
			expect(actor.getSnapshot().context.playerRef).toBe(playerRef);
		});
	});
});
