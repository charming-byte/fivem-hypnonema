import { MediaPlayer } from '@components/MediaPlayer';
import { useMediaPlayerMachine } from '@/hooks/useMediaPlayerMachine.ts';
import { useEffect } from 'react';
import { APIClient, ignoreReportingFailure } from '@/utils/apiClient.ts';
import { getUrlParameter, isDevelopmentBrowser } from '@/utils/misc.ts';

const development = isDevelopmentBrowser();
const browserId = development ? '-1' : getUrlParameter('browserId') || '-1';
const resourceName = development ? 'hypnonema' : getUrlParameter('resourceName') || 'hypnonema';
const apiClient = new APIClient(browserId, resourceName);

export const MediaPlayerContainer = () => {
	const { snapshot, videoCallbacks } = useMediaPlayerMachine(apiClient);

	useEffect(() => {
		document.title = `Hypnonema Browser - ${browserId}`;
		void apiClient.sendDocumentReady().catch(ignoreReportingFailure);

		if (!development) {
			console.log = () => {};
			console.warn = () => {};
		}
	}, []);

	return <MediaPlayer videoCallbacks={videoCallbacks} state={snapshot} />;
};
