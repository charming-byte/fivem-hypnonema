import { afterEach, beforeEach, describe, expect, test, vi } from 'vitest';
import { APIClient } from './apiClient';
import { DuiCallbacks, YoutubeAdStatus } from '@hypnonema/generated-types';

const fetchMock = vi.fn<typeof fetch>();
global.fetch = fetchMock;

describe('APIClient', () => {
	let apiClient: APIClient;
	const mockBrowserId = '1';
	const mockResourceName = 'test-resource';
	const mockBaseURL = `https://${mockResourceName}/browser_${mockBrowserId}`;

	beforeEach(() => {
		fetchMock.mockReset();
		fetchMock.mockResolvedValue({
			ok: true,
			json: async () => ({}),
		} as Response);

		apiClient = new APIClient(mockBrowserId, mockResourceName);
	});

	afterEach(() => {
		vi.clearAllMocks();
	});

	test('Constructor creates correct baseURL', () => {
		// @ts-expect-error - accessing private property on purpose for testing
		expect(apiClient.baseURL).toBe(mockBaseURL);
	});

	test('sendPlayerDuration calls invokeNuiCallback with correct parameters', async () => {
		const mockDuration = 120.5;
		const expectedUrl = `${mockBaseURL}_${DuiCallbacks.onPlayerDuration}`;

		await apiClient.sendPlayerDuration(mockDuration);

		expect(global.fetch).toHaveBeenCalledTimes(1);
		expect(global.fetch).toHaveBeenCalledWith(
			expectedUrl,
			expect.objectContaining({
				method: 'POST',
				headers: {
					'Content-Type': 'application/json;charset=UTF-8',
				},
				body: JSON.stringify(Math.round(mockDuration)),
			}),
		);
	});

	test('sendDocumentReady calls invokeNuiCallback with correct parameters', async () => {
		const expectedUrl = `${mockBaseURL}_${DuiCallbacks.onDocumentReady}`;

		await apiClient.sendDocumentReady();

		expect(global.fetch).toHaveBeenCalledTimes(1);
		expect(global.fetch).toHaveBeenCalledWith(
			expectedUrl,
			expect.objectContaining({
				method: 'POST',
				body: JSON.stringify(null),
			}),
		);
	});

	test('sendMediaPlayerError calls invokeNuiCallback with correct parameters', async () => {
		const mockError = { code: 2, message: 'Test error' };
		const expectedUrl = `${mockBaseURL}_${DuiCallbacks.onPlayerError}`;

		await apiClient.sendMediaPlayerError(mockError);

		expect(global.fetch).toHaveBeenCalledTimes(1);
		expect(global.fetch).toHaveBeenCalledWith(
			expectedUrl,
			expect.objectContaining({
				method: 'POST',
				body: JSON.stringify(mockError),
			}),
		);
	});

	test('sendMediaPlayerEnded calls invokeNuiCallback with correct parameters', async () => {
		const expectedUrl = `${mockBaseURL}_${DuiCallbacks.onPlayerEnd}`;

		await apiClient.sendMediaPlayerEnded();

		expect(global.fetch).toHaveBeenCalledTimes(1);
		expect(global.fetch).toHaveBeenCalledWith(
			expectedUrl,
			expect.objectContaining({
				method: 'POST',
				body: JSON.stringify(null),
			}),
		);
	});

	test('sendMediaPlayerLoaded calls invokeNuiCallback with correct parameters', async () => {
		const expectedUrl = `${mockBaseURL}_${DuiCallbacks.onPlayerLoaded}`;

		await apiClient.sendMediaPlayerLoaded();

		expect(global.fetch).toHaveBeenCalledTimes(1);
		expect(global.fetch).toHaveBeenCalledWith(
			expectedUrl,
			expect.objectContaining({
				method: 'POST',
				body: JSON.stringify(null),
			}),
		);
	});

	test('sendYoutubeAdStatus reports active status without surfacing dropped reports', async () => {
		fetchMock.mockRejectedValue(new Error('NUI unavailable'));
		const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
		const expectedUrl = `${mockBaseURL}_${DuiCallbacks.onYoutubeAdStatus}`;

		apiClient.sendYoutubeAdStatus(YoutubeAdStatus.PreRoll);
		await vi.waitFor(() => expect(global.fetch).toHaveBeenCalledTimes(1));

		expect(global.fetch).toHaveBeenCalledWith(
			expectedUrl,
			expect.objectContaining({
				method: 'POST',
				body: JSON.stringify(YoutubeAdStatus.PreRoll),
			}),
		);
		expect(consoleSpy).not.toHaveBeenCalled();

		consoleSpy.mockRestore();
	});

	test('invokeNuiCallback throws error on unsuccessful response', async () => {
		fetchMock.mockResolvedValue({
			ok: false,
			status: 500,
		} as Response);

		const callback = DuiCallbacks.onPlayerLoaded;
		// @ts-expect-error - accessing private property on purpose for testing
		await expect(apiClient.invokeNuiCallback(callback, null)).rejects.toThrow(
			'HTTP error! status: 500',
		);
	});

	test('invokeNuiCallback catches network errors and passes them on', async () => {
		const networkError = new Error('Network error');
		fetchMock.mockRejectedValue(networkError);

		const callback = DuiCallbacks.onPlayerLoaded;
		const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

		// @ts-expect-error - accessing private property on purpose for testing
		await expect(apiClient.invokeNuiCallback(callback, null)).rejects.toThrow(networkError);

		expect(consoleSpy).toHaveBeenCalledWith(
			expect.stringContaining(`Error invoking NUI callback ${callback}:`),
			networkError,
		);

		consoleSpy.mockRestore();
	});
});
