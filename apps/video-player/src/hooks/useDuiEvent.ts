import { type RefObject, useEffect, useRef } from 'react';

interface NuiMessageData<T = unknown> {
	type: string;
	data: T;
}

type NuiHandlerSignature<T> = (data: T) => void;

export const useDuiEvent = <T = unknown>(type: string, handler: (data: T) => void) => {
	const savedHandler: RefObject<NuiHandlerSignature<T>> = useRef(() => {});

	// Make sure we handle for a reactive handler
	useEffect(() => {
		savedHandler.current = handler;
	}, [handler]);

	useEffect(() => {
		const eventListener = (event: MessageEvent<NuiMessageData<T>>) => {
			const { type: eventType, data } = event.data;

			if (savedHandler.current) {
				if (eventType === type) {
					savedHandler.current(data);
				}
			}
		};

		window.addEventListener('message', eventListener);

		return () => window.removeEventListener('message', eventListener);
	}, [type]);
};
