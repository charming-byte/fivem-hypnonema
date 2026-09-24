declare global {
	interface Window {
		GetParentResourceName: () => string;
		invokeNative: () => void;
	}
}

export const isDevelopmentBrowser = (): boolean => !window.invokeNative && import.meta.env.DEV;

export const getUrlParameter = (name: string): string | undefined => {
	const urlParams = new URLSearchParams(window.location.search);
	return urlParams.get(name) || undefined;
};
