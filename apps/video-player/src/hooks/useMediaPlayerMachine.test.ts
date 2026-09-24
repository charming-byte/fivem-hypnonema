import { describe, expect, it, vi } from 'vitest';
import { act, renderHook } from '@testing-library/react';
import { DuiEvents, YoutubeAdStatus } from '@hypnonema/generated-types';
import type { APIClient } from '@/utils/apiClient.ts';
import { useMediaPlayerMachine } from './useMediaPlayerMachine.ts';

const sendDuiEvent = <T>(type: string, data: T) =>
	window.dispatchEvent(new MessageEvent('message', { data: { type, data } }));

describe('useMediaPlayerMachine', () => {
	const renderPlaying = () => {
		const rendered = renderHook(() => useMediaPlayerMachine());
		act(() => rendered.result.current.videoCallbacks.onStart({ current: null }));
		expect(rendered.result.current.snapshot.value).toBe('playing');
		return rendered;
	};

	it('wires DuiEvents.setPaused to pause and resume', () => {
		const { result } = renderPlaying();

		act(() => {
			sendDuiEvent(DuiEvents.setPaused, true);
		});
		expect(result.current.snapshot.value).toBe('paused');
		expect(result.current.snapshot.context.playing).toBe(false);

		act(() => {
			sendDuiEvent(DuiEvents.setPaused, false);
		});
		expect(result.current.snapshot.value).toBe('playing');
		expect(result.current.snapshot.context.playing).toBe(true);
	});

	it('wires DuiEvents.load, seek, setVolume, setMuted and setLooped', () => {
		const { result } = renderPlaying();

		act(() => {
			sendDuiEvent(DuiEvents.setVolume, 0.25);
			sendDuiEvent(DuiEvents.setMuted, true);
			sendDuiEvent(DuiEvents.setLooped, true);
			sendDuiEvent(DuiEvents.seek, 12);
		});
		expect(result.current.snapshot.context.volume).toBe(0.25);
		expect(result.current.snapshot.context.muted).toBe(true);
		expect(result.current.snapshot.context.looped).toBe(true);
		expect(result.current.snapshot.context.time).toBe(12);

		act(() => {
			sendDuiEvent(DuiEvents.load, {
				url: 'http://example.com/next.mp4',
				thumbnailUrl: 'http://example.com/next.jpg',
			});
		});
		expect(result.current.snapshot.value).toBe('loading');
		expect(result.current.snapshot.context.url).toBe('http://example.com/next.mp4');
		expect(result.current.snapshot.context.thumbnailUrl).toBe('http://example.com/next.jpg');
	});

	it('wires DuiEvents.reset back to the defaults', () => {
		const { result } = renderPlaying();

		act(() => {
			sendDuiEvent(DuiEvents.setVolume, 0.25);
		});
		act(() => {
			sendDuiEvent(DuiEvents.reset, null);
		});

		expect(result.current.snapshot.value).toBe('loading');
		expect(result.current.snapshot.context.volume).toBe(1);
	});

	// Reporting back to the client script is fire-and-forget, and in the dev browser every one of
	// those calls rejects because no NUI endpoint answers. Vitest fails a run on unhandled
	// rejections, so this regresses as soon as a report action loses its `.catch(...)`.
	it('keeps playing when every report to the client script rejects', async () => {
		const rejectReport = () => Promise.reject(new Error('no nui endpoint'));
		const apiClient = {
			sendMediaPlayerLoaded: rejectReport,
			sendMediaPlayerEnded: rejectReport,
			sendMediaPlayerError: rejectReport,
			sendPlayerDuration: rejectReport,
		} as unknown as APIClient;

		const { result } = renderHook(() => useMediaPlayerMachine(apiClient));

		act(() => result.current.videoCallbacks.onStart({ current: null }));
		act(() => result.current.videoCallbacks.onDuration(42));
		expect(result.current.snapshot.context.duration).toBe(42);

		act(() => result.current.videoCallbacks.onEnded());
		expect(result.current.snapshot.value).toBe('loading');

		act(() => {
			sendDuiEvent(DuiEvents.load, { url: 'http://example.com/next.mp4', thumbnailUrl: '' });
		});
		act(() => result.current.videoCallbacks.onStart({ current: null }));
		act(() => result.current.videoCallbacks.onError({ code: 2, message: 'boom' }));
		expect(result.current.snapshot.value).toBe('error');

		// let the rejected reports settle inside the test, not after it has already passed
		await act(async () => {
			await Promise.resolve();
		});
	});

	it('starts localAdActive true and unmutes immediately for non-YouTube sources', () => {
		const { result } = renderHook(() => useMediaPlayerMachine());
		expect(result.current.snapshot.context.localAdActive).toBe(true);

		act(() =>
			result.current.videoCallbacks.onStart({
				current: { src: 'http://example.com/video.mp4' } as HTMLVideoElement,
			}),
		);

		expect(result.current.snapshot.context.localAdActive).toBe(false);
	});

	it('keeps localAdActive true for a YouTube source until the ad detector reports', async () => {
		const mediaPlayerUtils = await import('@/utils/mediaPlayerUtils.ts');
		const spy = vi
			.spyOn(mediaPlayerUtils, 'prepareMediaPlayer')
			.mockImplementation((_playerRef, options) => {
				options?.onYoutubeAdStatus?.(YoutubeAdStatus.PreRoll);
			});

		const { result } = renderHook(() => useMediaPlayerMachine());
		act(() =>
			result.current.videoCallbacks.onStart({
				current: { src: 'https://www.youtube.com/watch?v=abc' } as HTMLVideoElement,
			}),
		);
		expect(result.current.snapshot.context.localAdActive).toBe(true);

		spy.mockRestore();
	});

	it('stops reacting to dui events after unmount', () => {
		const { result, unmount } = renderPlaying();
		unmount();

		act(() => {
			sendDuiEvent(DuiEvents.setPaused, true);
		});
		expect(result.current.snapshot.value).toBe('playing');
	});
});
