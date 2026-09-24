import type { RefObject } from 'react';
import { YoutubeAdStatus } from '@hypnonema/generated-types';

// Regular expressions to match various video URLs copied directly from react-player
export const MATCH_URL_YOUTUBE =
	/(?:youtu\.be\/|youtube(?:-nocookie|education)?\.com\/(?:embed\/|v\/|watch\/|watch\?v=|watch\?.+&v=|shorts\/|live\/))((\w|-){11})|youtube\.com\/playlist\?list=|youtube\.com\/user\//;
export const MATCH_URL_TWITCH_VIDEO = /(?:www\.|go\.)?twitch\.tv\/videos\/(\d+)($|\?)/;
const MATCH_URL_WISTIA = /(?:wistia\.(?:com|net)|wi\.st)\/(?:medias|embed)\/(?:iframe\/)?([^?]+)/;
export const AUDIO_EXTENSIONS =
	/\.(m4a|m4b|mp4a|mpga|mp2|mp2a|mp3|m2a|m3a|wav|weba|aac|oga|spx)($|\?)/i;
export const VIDEO_EXTENSIONS = /\.(mp4|og[gv]|webm|mov|m4v)(#t=[,\d+]+)?($|\?)/i;

export type PlayerType = 'youtube' | 'wistia' | 'file' | 'other';

const getPlayerType = (player: HTMLVideoElement): PlayerType => {
	const url = player.src;

	if (MATCH_URL_YOUTUBE.test(url as string)) {
		return 'youtube';
	}
	if (MATCH_URL_WISTIA.test(url as string)) {
		return 'wistia';
	}
	if (AUDIO_EXTENSIONS.test(url as string) || VIDEO_EXTENSIONS.test(url as string)) {
		return 'file';
	}
	return 'other';
};

export interface MediaSyncOptions {
	player: HTMLVideoElement | null;
	serverTime: number;
	snapThreshold?: number;
	minorSnapThreshold?: number;
	minSnapInterval?: number;
	onPlaybackRateChange?: (rate: number) => void;
	onTimeChange?: (time: number) => void;
}

type YoutubeAdStatusReporter = (status: YoutubeAdStatus) => void;
type ClickRequester = (x: number, y: number) => void;

interface YoutubeAdMonitor {
	iframe: HTMLIFrameElement;
	document: Document;
	source: string;
	observer: MutationObserver;
	skipRetryTimer?: ReturnType<typeof setInterval>;
	adActive: boolean;
	contentProgressObserved: boolean;
	onYoutubeAdStatus?: YoutubeAdStatusReporter;
	onRequestClick?: ClickRequester;
	lastClickRequest: number;
}

interface PrepareMediaPlayerOptions {
	onYoutubeAdStatus?: YoutubeAdStatusReporter;
	onRequestClick?: ClickRequester;
}

export class MediaPlayerSynchronizer {
	private static lastSoftSnap = 0;

	static sync({
		player,
		serverTime,
		snapThreshold = 0.5,
		minorSnapThreshold = 0.35,
		minSnapInterval = 5,
		onPlaybackRateChange,
		onTimeChange,
	}: MediaSyncOptions) {
		if (!player) return;

		const playerKind = getPlayerType(player);
		const canChangePlaybackRate =
			playerKind === 'youtube' || playerKind === 'wistia' || playerKind === 'file';

		const currentTime = player.currentTime ?? 0;
		const drift = serverTime - currentTime;

		if (Math.abs(drift) > snapThreshold) {
			onTimeChange?.(serverTime);
			if (canChangePlaybackRate) onPlaybackRateChange?.(1);
			return;
		}

		if (canChangePlaybackRate) {
			const correctionFactor = 0.05;
			const newRate = 1 + Math.max(Math.min(drift * 0.1, correctionFactor), -correctionFactor);
			onPlaybackRateChange?.(newRate);
		} else {
			const now = Date.now() / 1000;
			if (
				Math.abs(drift) > minorSnapThreshold &&
				now - MediaPlayerSynchronizer.lastSoftSnap > minSnapInterval
			) {
				onTimeChange?.(serverTime);
				MediaPlayerSynchronizer.lastSoftSnap = now;
			}
		}
	}
}

const youtubeAdMonitors = new WeakMap<HTMLVideoElement, YoutubeAdMonitor>();
const hiddenYoutubeAdElements = new WeakMap<HTMLElement, string>();

const YOUTUBE_AD_STATE_SELECTOR = '.ytp-ad-player-overlay, .ytp-ad-overlay-container';

const YOUTUBE_AD_UI_SELECTOR = ['.ytp-ad-text', '.ytp-ad-image-overlay'].join(',');

const YOUTUBE_AD_SKIP_SELECTOR = [
	'.ytp-ad-skip-button',
	'.ytp-ad-skip-button-modern',
	'.ytp-skip-ad-button',
	'.ytp-ad-skip-ad-slot button',
	'.ytp-ad-player-overlay-skip-or-preview button',
	'button[class*="skip"]',
	'[role="button"][class*="skip"]',
].join(',');
// A skip control only counts as one when it sits inside the ad UI. The broad `[class*="skip"]`
// fallbacks above match anywhere in the document, and a trusted DUI click landing next to the
// button hits the video surface instead — which toggles YouTube's play/pause and freezes the ad.
const YOUTUBE_AD_CONTAINER_SELECTOR = [
	'.ytp-ad-player-overlay',
	'.ytp-ad-player-overlay-layout',
	'.ytp-ad-skip-ad-slot',
	'.ytp-ad-module',
	'.video-ads',
].join(',');
const YOUTUBE_AD_SKIP_RETRY_INTERVAL_MS = 250;
const YOUTUBE_AD_CLICK_INTERVAL_MS = 1_000;

const getIframeDocument = (iframe: HTMLIFrameElement): Document | null => {
	try {
		return iframe.contentDocument;
	} catch {
		return null;
	}
};

const isVisible = (element: Element) => {
	if (!(element instanceof HTMLElement)) return true;
	if (element.hidden || element.getAttribute('aria-hidden') === 'true') return false;

	const style = element.ownerDocument.defaultView?.getComputedStyle(element);
	return style?.display !== 'none' && style?.visibility !== 'hidden' && style?.opacity !== '0';
};

const isSkipCandidate = (element: HTMLElement) => {
	if (element.getAttribute('aria-disabled') === 'true') return false;
	if ('disabled' in element && element.disabled) return false;

	// Deliberately not gated on visibility: under ytp-autohide the player styles the live control
	// as if it were not interactive while the ad still runs. `getClickPoint` rejecting boxless
	// controls already keeps a display:none control from being aimed at.
	return Boolean(element.closest(YOUTUBE_AD_CONTAINER_SELECTOR));
};

const activate = (element: HTMLElement) => {
	const view = element.ownerDocument.defaultView;
	const rect = element.getBoundingClientRect?.();
	const init: MouseEventInit = {
		bubbles: true,
		cancelable: true,
		composed: true,
		view,
		clientX: rect ? rect.left + rect.width / 2 : 0,
		clientY: rect ? rect.top + rect.height / 2 : 0,
		button: 0,
	};

	for (const type of ['pointerdown', 'mousedown', 'pointerup', 'mouseup'] as const) {
		try {
			const isPointer = type.startsWith('pointer');
			const PointerEventCtor = view?.PointerEvent;
			const event =
				isPointer && PointerEventCtor
					? new PointerEventCtor(type, { ...init, pointerType: 'mouse', isPrimary: true })
					: new MouseEvent(type, init);
			element.dispatchEvent(event);
		} catch {
			// Some event constructors are unavailable in the DUI runtime; the click below still runs.
		}
	}

	element.click();
};

// Where the control sits in the browser's own coordinate space: the rect is relative to the
// iframe's viewport, so the iframe's own offset in the top document has to be added back.
const getClickPoint = (element: HTMLElement, iframe: HTMLIFrameElement) => {
	const rect = element.getBoundingClientRect();
	if (rect.width === 0 || rect.height === 0) return null;

	const iframeRect = iframe.getBoundingClientRect();
	const x = iframeRect.left + rect.left + rect.width / 2;
	const y = iframeRect.top + rect.top + rect.height / 2;

	// A control scrolled or laid out beyond the iframe would send the click somewhere else in the
	// page entirely; the ad is better left alone than clicked at random.
	if (x < iframeRect.left || x > iframeRect.right || y < iframeRect.top || y > iframeRect.bottom)
		return null;

	const scale = window.devicePixelRatio || 1;

	return { x: x * scale, y: y * scale };
};

const attemptYoutubeAdSkip = (monitor: YoutubeAdMonitor) => {
	const controls = Array.from(
		monitor.document.querySelectorAll<HTMLElement>(YOUTUBE_AD_SKIP_SELECTOR),
	).filter(isSkipCandidate);

	controls.forEach(activate);

	const now = Date.now();
	if (monitor.onRequestClick && now - monitor.lastClickRequest >= YOUTUBE_AD_CLICK_INTERVAL_MS) {
		const point = controls.map((control) => getClickPoint(control, monitor.iframe)).find(Boolean);

		if (point) {
			monitor.lastClickRequest = now;
			monitor.onRequestClick(point.x, point.y);
		}
	}

	return { controlPresent: controls.length > 0 };
};

const hideVisibleAdUi = (document: Document) => {
	document.querySelectorAll<HTMLElement>(YOUTUBE_AD_UI_SELECTOR).forEach((element) => {
		if (hiddenYoutubeAdElements.has(element) || !isVisible(element)) return;
		// The skip control carries its own .ytp-ad-text label; blanking it leaves an empty button.
		if (element.closest(YOUTUBE_AD_SKIP_SELECTOR)) return;

		hiddenYoutubeAdElements.set(element, element.style.opacity);
		element.style.opacity = '0';
	});
};

// .ad-showing on the player element is the authoritative signal — YouTube drops it the moment the
// ad is skipped. The overlay fallback covers ad shapes that do not set it, and is not gated on
// visibility: under ytp-autohide the player fades its own overlays out while the ad still runs.
const detectYoutubeAd = (document: Document) =>
	Boolean(document.querySelector('.ad-showing')) ||
	Array.from(document.querySelectorAll(YOUTUBE_AD_STATE_SELECTOR)).some(
		(element) => element.childElementCount > 0,
	);

const stopYoutubeSkipRetries = (monitor: YoutubeAdMonitor) => {
	if (monitor.skipRetryTimer === undefined) return;

	clearInterval(monitor.skipRetryTimer);
	monitor.skipRetryTimer = undefined;
};

const startYoutubeSkipRetries = (monitor: YoutubeAdMonitor) => {
	if (monitor.skipRetryTimer !== undefined) return;

	monitor.skipRetryTimer = setInterval(
		() => evaluateYoutubeAd(monitor),
		YOUTUBE_AD_SKIP_RETRY_INTERVAL_MS,
	);
};

// Runs from the observer and from every retry tick. The tick matters for the end of the ad: the
// skip removes the overlay, but the player carries ytp-hide-controls and barely mutates once
// content is back, so waiting for a mutation to notice can leave the server without its report
// until the quorum times out.
const evaluateYoutubeAd = (monitor: YoutubeAdMonitor) => {
	const adActive = detectYoutubeAd(monitor.document);

	hideVisibleAdUi(monitor.document);
	attemptYoutubeAdSkip(monitor);

	if (adActive) startYoutubeSkipRetries(monitor);
	else stopYoutubeSkipRetries(monitor);

	if (adActive === monitor.adActive) return;

	monitor.adActive = adActive;
	monitor.onYoutubeAdStatus?.(
		adActive
			? monitor.contentProgressObserved
				? YoutubeAdStatus.MidRoll
				: YoutubeAdStatus.PreRoll
			: YoutubeAdStatus.Inactive,
	);
};

const disconnectYoutubeAdMonitor = (player: HTMLVideoElement) => {
	const monitor = youtubeAdMonitors.get(player);
	if (!monitor) return;

	monitor.observer.disconnect();
	stopYoutubeSkipRetries(monitor);
	youtubeAdMonitors.delete(player);
};

const prepareYoutubePlayer = (
	playerRef: RefObject<HTMLVideoElement | null>,
	{ onYoutubeAdStatus, onRequestClick }: PrepareMediaPlayerOptions,
) => {
	const player = playerRef.current;
	const iframe = player?.shadowRoot?.querySelector('iframe') as
		| HTMLIFrameElement
		| null
		| undefined;
	if (!player || !iframe) return;

	const iframeDocument = getIframeDocument(iframe);

	const existingMonitor = youtubeAdMonitors.get(player);
	// The document is part of the identity: the iframe element survives a navigation while its
	// document is replaced, which would leave the observer bound to a detached tree.
	if (
		existingMonitor?.iframe === iframe &&
		existingMonitor.source === player.src &&
		existingMonitor.document === iframeDocument
	) {
		if (!existingMonitor.adActive) existingMonitor.contentProgressObserved = true;
		return;
	}
	disconnectYoutubeAdMonitor(player);

	const observeTarget = iframeDocument?.body ?? iframeDocument?.documentElement;
	if (!iframeDocument || !observeTarget) return;

	const monitor: YoutubeAdMonitor = {
		iframe,
		document: iframeDocument,
		source: player.src,
		observer: new MutationObserver(() => evaluateYoutubeAd(monitor)),
		adActive: false,
		contentProgressObserved: false,
		onYoutubeAdStatus,
		onRequestClick,
		lastClickRequest: 0,
	};

	youtubeAdMonitors.set(player, monitor);
	monitor.observer.observe(observeTarget, {
		attributes: true,
		// The skip countdown ticks as a text mutation; without this the observer stays silent
		// through exactly the window in which the control becomes clickable.
		characterData: true,
		childList: true,
		subtree: true,
	});

	evaluateYoutubeAd(monitor);
};

const prepareTwitchPlayer = (playerRef: RefObject<HTMLVideoElement | null>) => {
	const iframe: HTMLIFrameElement | undefined = playerRef.current?.shadowRoot?.querySelector(
		'iframe',
	) as HTMLIFrameElement;
	if (!iframe) {
		console.warn('Twitch player iframe could not be found.');
		return;
	}
	const button = iframe.contentDocument?.querySelector<HTMLButtonElement>(
		'button[data-a-target="content-classification-gate-overlay-start-watching-button"]',
	);
	if (button) {
		button.click();
		console.log('clicked twitch accept mature audience button.');
	}
};

export const prepareMediaPlayer = (
	playerRef: RefObject<HTMLVideoElement | null>,
	options: PrepareMediaPlayerOptions = {},
) => {
	const url = playerRef.current?.src;
	if (!url) return;

	if (MATCH_URL_YOUTUBE.test(url)) {
		prepareYoutubePlayer(playerRef, options);
		return;
	}

	if (playerRef.current) disconnectYoutubeAdMonitor(playerRef.current);

	if (MATCH_URL_TWITCH_VIDEO.test(url)) {
		prepareTwitchPlayer(playerRef);
	}
};
