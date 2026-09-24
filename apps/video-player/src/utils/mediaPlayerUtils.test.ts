import { afterEach, beforeEach, describe, expect, test, vi } from 'vitest';
import { YoutubeAdStatus } from '@hypnonema/generated-types';
import { MediaPlayerSynchronizer, prepareMediaPlayer } from './mediaPlayerUtils';

// Players whose playback rate can be nudged, so they get the fine adjustment path.
const YOUTUBE_URL = 'https://www.youtube.com/watch?v=dQw4w9WgXcQ';
const WISTIA_URL = 'https://fast.wistia.net/embed/iframe/abc123';
const VIDEO_FILE_URL = 'https://example.com/video.mp4';
const AUDIO_FILE_URL = 'https://example.com/song.mp3';
// Everything else falls back to the throttled soft snap, e.g. twitch or hls.
const TWITCH_URL = 'https://www.twitch.tv/videos/123456';
const HLS_URL = 'https://example.com/stream.m3u8';

const createPlayer = (src: string, currentTime?: number) =>
	({ src, currentTime }) as HTMLVideoElement;

const createPlayerRef = (src: string, iframe?: HTMLIFrameElement) => {
	const player = document.createElement('div') as unknown as HTMLVideoElement;
	Object.defineProperty(player, 'src', { value: src, configurable: true });

	const shadowRoot = (player as unknown as HTMLElement).attachShadow({ mode: 'open' });
	if (iframe) shadowRoot.append(iframe);

	return { current: player };
};

const createYoutubePlayerRef = () => {
	const iframe = document.createElement('iframe');
	document.body.append(iframe);

	const playerRef = createPlayerRef(YOUTUBE_URL, iframe);
	const iframeDocument = iframe.contentDocument;
	if (!iframeDocument) throw new Error('Expected iframe document');

	return { playerRef, iframe, iframeDocument };
};

// jsdom lays nothing out, so every box a click depends on has to be supplied explicitly.
const createRect = (left: number, top: number, width: number, height: number) =>
	({ left, top, width, height, right: left + width, bottom: top + height }) as DOMRect;

describe('MediaPlayerSynchronizer', () => {
	const onPlaybackRateChange = vi.fn();
	const onTimeChange = vi.fn();

	const sync = (options: Partial<Parameters<typeof MediaPlayerSynchronizer.sync>[0]>) =>
		MediaPlayerSynchronizer.sync({
			player: createPlayer(VIDEO_FILE_URL, 0),
			serverTime: 0,
			onPlaybackRateChange,
			onTimeChange,
			...options,
		});

	beforeEach(() => {
		vi.resetAllMocks();
		vi.useFakeTimers();
		// a real epoch, so the very first soft snap is not blocked by the interval
		vi.setSystemTime(new Date('2026-01-01T00:00:00Z'));
		// @ts-expect-error - resetting the private throttle between tests
		MediaPlayerSynchronizer.lastSoftSnap = 0;
	});

	afterEach(() => {
		vi.useRealTimers();
	});

	test('does nothing when no player is attached', () => {
		sync({ player: null, serverTime: 10 });

		expect(onTimeChange).not.toHaveBeenCalled();
		expect(onPlaybackRateChange).not.toHaveBeenCalled();
	});

	test('treats a player without a currentTime as being at 0', () => {
		sync({ player: createPlayer(VIDEO_FILE_URL, undefined), serverTime: 10 });

		expect(onTimeChange).toHaveBeenCalledWith(10);
	});

	describe('hard snap', () => {
		test('snaps forward and resets the rate when the player lags behind', () => {
			sync({ player: createPlayer(VIDEO_FILE_URL, 5), serverTime: 10, snapThreshold: 1 });

			expect(onTimeChange).toHaveBeenCalledWith(10);
			expect(onPlaybackRateChange).toHaveBeenCalledWith(1);
		});

		test('snaps backward when the player runs ahead', () => {
			sync({ player: createPlayer(VIDEO_FILE_URL, 15), serverTime: 10, snapThreshold: 1 });

			expect(onTimeChange).toHaveBeenCalledWith(10);
			expect(onPlaybackRateChange).toHaveBeenCalledWith(1);
		});

		test('uses a default threshold of 0.5 seconds', () => {
			sync({ player: createPlayer(VIDEO_FILE_URL, 9.4), serverTime: 10 });
			expect(onTimeChange).toHaveBeenCalledWith(10);

			vi.resetAllMocks();

			sync({ player: createPlayer(VIDEO_FILE_URL, 9.6), serverTime: 10 });
			expect(onTimeChange).not.toHaveBeenCalled();
		});

		test('leaves the rate alone for players that cannot change it', () => {
			sync({ player: createPlayer(TWITCH_URL, 5), serverTime: 10, snapThreshold: 1 });

			expect(onTimeChange).toHaveBeenCalledWith(10);
			expect(onPlaybackRateChange).not.toHaveBeenCalled();
		});

		test('is not throttled, unlike the soft snap', () => {
			sync({ player: createPlayer(TWITCH_URL, 5), serverTime: 10, snapThreshold: 1 });
			sync({ player: createPlayer(TWITCH_URL, 15), serverTime: 20, snapThreshold: 1 });

			expect(onTimeChange).toHaveBeenNthCalledWith(1, 10);
			expect(onTimeChange).toHaveBeenNthCalledWith(2, 20);
		});
	});

	describe('fine adjustment for players that support a playback rate', () => {
		test.each([
			['youtube', YOUTUBE_URL],
			['wistia', WISTIA_URL],
			['video file', VIDEO_FILE_URL],
			['audio file', AUDIO_FILE_URL],
		])('adjusts the rate for a %s player', (_kind, url) => {
			sync({ player: createPlayer(url, 9.8), serverTime: 10, snapThreshold: 1 });

			expect(onPlaybackRateChange).toHaveBeenCalledTimes(1);
			expect(onTimeChange).not.toHaveBeenCalled();
		});

		test('speeds up when the player lags behind', () => {
			sync({ player: createPlayer(VIDEO_FILE_URL, 9.8), serverTime: 10, snapThreshold: 1 });

			expect(onPlaybackRateChange.mock.calls[0]?.[0]).toBeCloseTo(1.02);
		});

		test('slows down when the player runs ahead', () => {
			sync({ player: createPlayer(VIDEO_FILE_URL, 10.2), serverTime: 10, snapThreshold: 1 });

			expect(onPlaybackRateChange.mock.calls[0]?.[0]).toBeCloseTo(0.98);
		});

		test('resets the rate to 1 when there is no drift', () => {
			sync({ player: createPlayer(VIDEO_FILE_URL, 10), serverTime: 10, snapThreshold: 1 });

			expect(onPlaybackRateChange).toHaveBeenCalledWith(1);
		});

		test.each([
			['speeding up', 8, 1.05],
			['slowing down', 12, 0.95],
		])('caps the correction at 5%% when %s', (_direction, currentTime, expected) => {
			sync({ player: createPlayer(VIDEO_FILE_URL, currentTime), serverTime: 10, snapThreshold: 5 });

			expect(onPlaybackRateChange.mock.calls[0]?.[0]).toBeCloseTo(expected);
		});
	});

	describe('soft snap for players that cannot change their playback rate', () => {
		test.each([
			['twitch', TWITCH_URL],
			['hls', HLS_URL],
		])('snaps a %s player once the drift exceeds the minor threshold', (_kind, url) => {
			sync({
				player: createPlayer(url, 9.8),
				serverTime: 10,
				snapThreshold: 1,
				minorSnapThreshold: 0.1,
			});

			expect(onTimeChange).toHaveBeenCalledWith(10);
			expect(onPlaybackRateChange).not.toHaveBeenCalled();
		});

		test('stays put while the drift is below the minor threshold', () => {
			sync({
				player: createPlayer(TWITCH_URL, 9.95),
				serverTime: 10,
				snapThreshold: 1,
				minorSnapThreshold: 0.1,
			});

			expect(onTimeChange).not.toHaveBeenCalled();
			expect(onPlaybackRateChange).not.toHaveBeenCalled();
		});

		test('respects the minimum interval between soft snaps', () => {
			const options = { snapThreshold: 1, minorSnapThreshold: 0.1, minSnapInterval: 10 };

			sync({ player: createPlayer(TWITCH_URL, 9.8), serverTime: 10, ...options });
			expect(onTimeChange).toHaveBeenCalledTimes(1);

			// second call before the interval elapsed
			onTimeChange.mockClear();
			sync({ player: createPlayer(TWITCH_URL, 19.8), serverTime: 20, ...options });
			expect(onTimeChange).not.toHaveBeenCalled();

			// third call once the interval elapsed
			vi.advanceTimersByTime(11_000);
			sync({ player: createPlayer(TWITCH_URL, 29.8), serverTime: 30, ...options });
			expect(onTimeChange).toHaveBeenCalledWith(30);
			expect(onPlaybackRateChange).not.toHaveBeenCalled();
		});

		test('uses a default minor threshold of 0.35 seconds', () => {
			// a drift of 0.4 sits between the default minor threshold and the 0.5 hard snap
			sync({ player: createPlayer(TWITCH_URL, 9.6), serverTime: 10 });
			expect(onTimeChange).toHaveBeenCalledWith(10);

			// @ts-expect-error - resetting the private throttle so the interval is not what blocks
			MediaPlayerSynchronizer.lastSoftSnap = 0;
			onTimeChange.mockClear();

			sync({ player: createPlayer(TWITCH_URL, 9.8), serverTime: 10 });
			expect(onTimeChange).not.toHaveBeenCalled();
		});

		test('uses a default interval of 5 seconds', () => {
			const options = { snapThreshold: 1, minorSnapThreshold: 0.1 };

			sync({ player: createPlayer(TWITCH_URL, 9.8), serverTime: 10, ...options });
			expect(onTimeChange).toHaveBeenCalledTimes(1);

			onTimeChange.mockClear();
			vi.advanceTimersByTime(4_000);
			sync({ player: createPlayer(TWITCH_URL, 19.8), serverTime: 20, ...options });
			expect(onTimeChange).not.toHaveBeenCalled();

			vi.advanceTimersByTime(2_000);
			sync({ player: createPlayer(TWITCH_URL, 29.8), serverTime: 30, ...options });
			expect(onTimeChange).toHaveBeenCalledWith(30);
		});
	});
});

// Mirrors the real player: .video-ads.ytp-ad-module is a permanent layer that merely goes empty
// between ads, while the overlay inside it is what actually comes and goes with one.
const createAd = (iframeDocument: Document) => {
	const adLayer = iframeDocument.createElement('div');
	adLayer.className = 'video-ads ytp-ad-module';
	const overlay = iframeDocument.createElement('div');
	overlay.className = 'ytp-ad-player-overlay';
	overlay.append(iframeDocument.createElement('div'));

	adLayer.append(overlay);
	iframeDocument.body.append(adLayer);

	return { adLayer, overlay };
};

describe('prepareMediaPlayer YouTube ad handling', () => {
	const onYoutubeAdStatus = vi.fn();

	beforeEach(() => {
		vi.resetAllMocks();
	});

	afterEach(() => {
		document.body.replaceChildren();
	});

	test('clicks the skip control, leaves inert ones alone and reports active then ended', async () => {
		const { playerRef, iframeDocument } = createYoutubePlayerRef();
		const { adLayer, overlay } = createAd(iframeDocument);
		const disabledSkipButton = iframeDocument.createElement('button');
		disabledSkipButton.className = 'ytp-ad-skip-button';
		disabledSkipButton.disabled = true;
		const disabledClick = vi.spyOn(disabledSkipButton, 'click');
		// The player sits in ytp-autohide inside the DUI, so the live control is styled as if it
		// were not interactive. It still has to be clicked.
		const overlayHiddenSkipButton = iframeDocument.createElement('button');
		overlayHiddenSkipButton.className = 'ytp-ad-skip-button-modern';
		overlayHiddenSkipButton.style.pointerEvents = 'none';
		overlayHiddenSkipButton.style.opacity = '0.5';
		const click = vi.spyOn(overlayHiddenSkipButton, 'click');

		prepareMediaPlayer(playerRef, { onYoutubeAdStatus });

		// The skip control's containers must stay in layout, otherwise it cannot be clicked.
		expect(adLayer.style.display).not.toBe('none');
		expect(overlay.style.display).not.toBe('none');

		overlay.append(disabledSkipButton, overlayHiddenSkipButton);
		await vi.waitFor(() => expect(click).toHaveBeenCalled());
		expect(disabledClick).not.toHaveBeenCalled();
		expect(onYoutubeAdStatus).toHaveBeenCalledWith(YoutubeAdStatus.PreRoll);

		onYoutubeAdStatus.mockClear();
		await new Promise((resolve) => setTimeout(resolve, 0));
		expect(onYoutubeAdStatus).not.toHaveBeenCalled();

		// The skip tears the overlay down but leaves the permanent ad layer behind.
		overlay.remove();
		await vi.waitFor(() =>
			expect(onYoutubeAdStatus).toHaveBeenCalledWith(YoutubeAdStatus.Inactive),
		);
		expect(adLayer.isConnected).toBe(true);
	});

	test('reports the ad as ended once the overlay is emptied in place', async () => {
		const { playerRef, iframeDocument } = createYoutubePlayerRef();
		const { adLayer, overlay } = createAd(iframeDocument);

		prepareMediaPlayer(playerRef, { onYoutubeAdStatus });
		expect(onYoutubeAdStatus).toHaveBeenCalledWith(YoutubeAdStatus.PreRoll);

		overlay.replaceChildren();

		await vi.waitFor(() =>
			expect(onYoutubeAdStatus).toHaveBeenCalledWith(YoutubeAdStatus.Inactive),
		);
		expect(adLayer.isConnected).toBe(true);
	});

	test.each([
		['pre-roll', YoutubeAdStatus.PreRoll, false],
		['mid-roll', YoutubeAdStatus.MidRoll, true],
	])(
		'keeps retrying an available skip control during a %s ad',
		async (_phase, expectedStatus, contentProgressObserved) => {
			vi.useFakeTimers();
			try {
				const { playerRef, iframeDocument } = createYoutubePlayerRef();
				const skipButton = iframeDocument.createElement('button');
				skipButton.className = 'ytp-skip-ad-button';
				const click = vi.spyOn(skipButton, 'click');

				if (contentProgressObserved) {
					prepareMediaPlayer(playerRef, { onYoutubeAdStatus });
					prepareMediaPlayer(playerRef, { onYoutubeAdStatus });
				}
				const { overlay } = createAd(iframeDocument);
				overlay.append(skipButton);
				if (!contentProgressObserved) prepareMediaPlayer(playerRef, { onYoutubeAdStatus });
				await vi.advanceTimersByTimeAsync(0);
				expect(onYoutubeAdStatus).toHaveBeenCalledWith(expectedStatus);
				click.mockClear();

				await vi.advanceTimersByTimeAsync(1_000);

				expect(click).toHaveBeenCalled();
				expect(onYoutubeAdStatus).not.toHaveBeenCalledWith(YoutubeAdStatus.Inactive);

				overlay.remove();
				await vi.advanceTimersByTimeAsync(0);
				expect(onYoutubeAdStatus).toHaveBeenCalledWith(YoutubeAdStatus.Inactive);
				click.mockClear();

				await vi.advanceTimersByTimeAsync(1_000);
				expect(click).not.toHaveBeenCalled();
			} finally {
				vi.useRealTimers();
			}
		},
	);

	test('hides ad chrome without taking it out of layout and reports the ad active', () => {
		const { playerRef, iframeDocument } = createYoutubePlayerRef();
		const { overlay } = createAd(iframeDocument);
		const adText = iframeDocument.createElement('div');
		adText.className = 'ytp-ad-text';
		// The skip control carries its own label of the same class, which must stay readable.
		const skipButton = iframeDocument.createElement('button');
		skipButton.className = 'ytp-ad-skip-button-modern';
		const skipLabel = iframeDocument.createElement('div');
		skipLabel.className = 'ytp-ad-text ytp-ad-skip-button-text';
		skipButton.append(skipLabel);
		overlay.append(adText, skipButton);

		expect(() => prepareMediaPlayer(playerRef, { onYoutubeAdStatus })).not.toThrow();
		expect(adText.style.opacity).toBe('0');
		expect(adText.style.display).not.toBe('none');
		expect(skipLabel.style.opacity).not.toBe('0');
		expect(onYoutubeAdStatus).toHaveBeenCalledWith(YoutubeAdStatus.PreRoll);
	});

	test('asks the client script to click the skip control through the DUI natives', async () => {
		const onRequestClick = vi.fn();
		const { playerRef, iframe, iframeDocument } = createYoutubePlayerRef();
		const { overlay } = createAd(iframeDocument);
		// jsdom lays nothing out, so both boxes are supplied explicitly.
		iframe.getBoundingClientRect = () => createRect(0, 0, 1280, 720);
		const skipButton = iframeDocument.createElement('button');
		skipButton.className = 'ytp-ad-skip-button-modern';
		skipButton.getBoundingClientRect = () => createRect(100, 40, 80, 20);
		overlay.append(skipButton);

		prepareMediaPlayer(playerRef, { onYoutubeAdStatus, onRequestClick });

		await vi.waitFor(() => expect(onRequestClick).toHaveBeenCalled());
		expect(onRequestClick).toHaveBeenCalledWith(140, 50);
	});

	// A trusted DUI click that misses the button lands on the video surface, which toggles
	// YouTube's play/pause and leaves the ad frozen for good.
	test('never clicks a skip-like control that sits outside the ad UI', async () => {
		const onRequestClick = vi.fn();
		const { playerRef, iframe, iframeDocument } = createYoutubePlayerRef();
		createAd(iframeDocument);
		iframe.getBoundingClientRect = () => createRect(0, 0, 1280, 720);
		const strayButton = iframeDocument.createElement('button');
		strayButton.className = 'skip-navigation';
		strayButton.getBoundingClientRect = () => createRect(100, 40, 80, 20);
		const click = vi.spyOn(strayButton, 'click');
		iframeDocument.body.append(strayButton);

		prepareMediaPlayer(playerRef, { onYoutubeAdStatus, onRequestClick });

		await vi.waitFor(() => expect(onYoutubeAdStatus).toHaveBeenCalled());
		expect(click).not.toHaveBeenCalled();
		expect(onRequestClick).not.toHaveBeenCalled();
	});

	test('does not ask for a click aimed outside the browser surface', async () => {
		const onRequestClick = vi.fn();
		const { playerRef, iframe, iframeDocument } = createYoutubePlayerRef();
		const { overlay } = createAd(iframeDocument);
		iframe.getBoundingClientRect = () => createRect(0, 0, 320, 180);
		const skipButton = iframeDocument.createElement('button');
		skipButton.className = 'ytp-ad-skip-button-modern';
		skipButton.getBoundingClientRect = () => createRect(600, 40, 80, 20);
		const click = vi.spyOn(skipButton, 'click');
		overlay.append(skipButton);

		prepareMediaPlayer(playerRef, { onYoutubeAdStatus, onRequestClick });

		await vi.waitFor(() => expect(click).toHaveBeenCalled());
		expect(onRequestClick).not.toHaveBeenCalled();
	});

	test('does not ask for a click when the skip control has no box to aim at', async () => {
		const onRequestClick = vi.fn();
		const { playerRef, iframeDocument } = createYoutubePlayerRef();
		const { overlay } = createAd(iframeDocument);
		const skipButton = iframeDocument.createElement('button');
		skipButton.className = 'ytp-ad-skip-button-modern';
		const click = vi.spyOn(skipButton, 'click');
		overlay.append(skipButton);

		prepareMediaPlayer(playerRef, { onYoutubeAdStatus, onRequestClick });

		// An injected click is a real click; without a box it would land somewhere arbitrary.
		await vi.waitFor(() => expect(click).toHaveBeenCalled());
		expect(onRequestClick).not.toHaveBeenCalled();
	});

	test('retries a skip control that only appears once the countdown ends', async () => {
		vi.useFakeTimers();
		try {
			const { playerRef, iframeDocument } = createYoutubePlayerRef();
			const { overlay } = createAd(iframeDocument);

			prepareMediaPlayer(playerRef, { onYoutubeAdStatus });
			expect(onYoutubeAdStatus).toHaveBeenCalledWith(YoutubeAdStatus.PreRoll);

			// The control shows up without any observable mutation reaching us.
			const skipButton = iframeDocument.createElement('button');
			skipButton.className = 'ytp-skip-ad-button';
			const click = vi.spyOn(skipButton, 'click');
			overlay.appendChild(skipButton);
			click.mockClear();

			await vi.advanceTimersByTimeAsync(1_000);

			expect(click).toHaveBeenCalled();
		} finally {
			vi.useRealTimers();
		}
	});

	test('reports an ad appearing after content progress as mid-roll', async () => {
		const { playerRef, iframeDocument } = createYoutubePlayerRef();

		prepareMediaPlayer(playerRef, { onYoutubeAdStatus });
		prepareMediaPlayer(playerRef, { onYoutubeAdStatus });

		createAd(iframeDocument);

		await vi.waitFor(() => expect(onYoutubeAdStatus).toHaveBeenCalledWith(YoutubeAdStatus.MidRoll));
	});

	test('treats an ad on a new source as pre-roll when the iframe is reused', () => {
		const { playerRef, iframeDocument } = createYoutubePlayerRef();

		prepareMediaPlayer(playerRef, { onYoutubeAdStatus });
		prepareMediaPlayer(playerRef, { onYoutubeAdStatus });
		Object.defineProperty(playerRef.current, 'src', {
			value: 'https://www.youtube.com/watch?v=aaaaaaaaaaa',
			configurable: true,
		});
		createAd(iframeDocument);

		prepareMediaPlayer(playerRef, { onYoutubeAdStatus });

		expect(onYoutubeAdStatus).toHaveBeenCalledWith(YoutubeAdStatus.PreRoll);
	});

	test('ignores the permanent ad layer when it holds no ad', async () => {
		const { playerRef, iframeDocument } = createYoutubePlayerRef();
		const adLayer = iframeDocument.createElement('div');
		adLayer.className = 'video-ads ytp-ad-module';
		iframeDocument.body.append(adLayer);

		prepareMediaPlayer(playerRef, { onYoutubeAdStatus });
		await new Promise((resolve) => setTimeout(resolve, 0));

		expect(onYoutubeAdStatus).not.toHaveBeenCalled();
	});

	test('does not report when no ad elements are present', async () => {
		const { playerRef, iframeDocument } = createYoutubePlayerRef();

		prepareMediaPlayer(playerRef, { onYoutubeAdStatus });
		iframeDocument.body.append(iframeDocument.createElement('main'));
		await new Promise((resolve) => setTimeout(resolve, 0));

		expect(onYoutubeAdStatus).not.toHaveBeenCalled();
	});

	test('does not throw when iframe document access is unavailable', () => {
		const iframe = document.createElement('iframe');
		Object.defineProperty(iframe, 'contentDocument', {
			get: () => {
				throw new DOMException('Blocked', 'SecurityError');
			},
		});
		const playerRef = createPlayerRef(YOUTUBE_URL, iframe);

		expect(() => prepareMediaPlayer(playerRef, { onYoutubeAdStatus })).not.toThrow();
		expect(onYoutubeAdStatus).not.toHaveBeenCalled();
	});

	test('leaves non-YouTube media behavior alone and does not report ad status', () => {
		const iframe = document.createElement('iframe');
		document.body.append(iframe);
		const playerRef = createPlayerRef(VIDEO_FILE_URL, iframe);

		prepareMediaPlayer(playerRef, { onYoutubeAdStatus });

		expect(onYoutubeAdStatus).not.toHaveBeenCalled();
	});

	test('preserves existing Twitch media preparation behavior', () => {
		const iframe = document.createElement('iframe');
		document.body.append(iframe);
		const playerRef = createPlayerRef(TWITCH_URL, iframe);
		const button = iframe.contentDocument?.createElement('button');
		if (!button) throw new Error('Expected iframe document');
		button.setAttribute(
			'data-a-target',
			'content-classification-gate-overlay-start-watching-button',
		);
		const click = vi.spyOn(button, 'click');
		iframe.contentDocument?.body.append(button);

		prepareMediaPlayer(playerRef, { onYoutubeAdStatus });

		expect(click).toHaveBeenCalledTimes(1);
		expect(onYoutubeAdStatus).not.toHaveBeenCalled();
	});
});
