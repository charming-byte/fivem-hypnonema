import { DuiCallbacks, YoutubeAdStatus } from '@hypnonema/generated-types';

// Only the fields that are actually reported to the server. Deliberately not `MediaError` itself:
// callers build this from a media element's error, they never own a real `MediaError` instance.
export type MediaPlayerError = Pick<MediaError, 'code' | 'message'>;

// Reporting to the client script is fire-and-forget: `invokeNuiCallback` already logs a failure
// before rethrowing (the rethrow only exists so a caller *can* await one), and a dropped report is
// never worth failing playback over. Absorb the rejection so it does not surface as an unhandled
// one — routine in the dev browser, where no NUI endpoint answers at all.
export const ignoreReportingFailure = () => {};

export class APIClient {
	private readonly baseURL: string;

	public constructor(browserId: string, resourceName: string) {
		this.baseURL = `https://${resourceName}/browser_${browserId}`;
	}

	public async sendPlayerDuration(duration: number) {
		await this.invokeNuiCallback(DuiCallbacks.onPlayerDuration, Math.round(duration));
	}

	public async sendDocumentReady() {
		await this.invokeNuiCallback(DuiCallbacks.onDocumentReady, null);
	}

	public async sendMediaPlayerError(error: MediaPlayerError) {
		await this.invokeNuiCallback(DuiCallbacks.onPlayerError, error);
	}

	public async sendMediaPlayerEnded() {
		await this.invokeNuiCallback(DuiCallbacks.onPlayerEnd, null);
	}

	public async sendMediaPlayerLoaded() {
		await this.invokeNuiCallback(DuiCallbacks.onPlayerLoaded, null);
	}

	// Asks the client script to click a point through the DUI mouse natives, because a click the
	// page dispatches on itself is untrusted and YouTube ignores those on its ad controls.
	public sendClickRequest(x: number, y: number) {
		void this.invokeNuiCallback(
			DuiCallbacks.onRequestClick,
			{ x: Math.round(x), y: Math.round(y) },
			{ logErrors: false },
		).catch(ignoreReportingFailure);
	}

	public sendYoutubeAdStatus(status: YoutubeAdStatus) {
		void this.invokeNuiCallback(DuiCallbacks.onYoutubeAdStatus, status, { logErrors: false }).catch(
			ignoreReportingFailure,
		);
	}

	private async invokeNuiCallback(
		callback: string,
		body: unknown,
		options: { logErrors?: boolean } = {},
	) {
		const url = `${this.baseURL}_${callback}`;

		try {
			const response = await fetch(url, {
				method: 'POST',
				headers: {
					'Content-Type': 'application/json;charset=UTF-8',
				},
				body: JSON.stringify(body),
			});

			if (!response.ok) {
				throw new Error(`HTTP error! status: ${response.status}`);
			}
		} catch (error) {
			if (options.logErrors !== false)
				console.error(`Error invoking NUI callback ${callback}:`, error);
			throw error;
		}
	}
}
