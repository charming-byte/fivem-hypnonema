import { MutableRefObject, useEffect, useRef } from "react";

interface NuiMessageData<T = unknown> {
  type: string;
  data: T;
  app: string;
}

type Handler<T> = (data: T) => void;
export const useNuiEvent = <D = unknown>(
  method: string,
  handler: Handler<D>,
): void => {
  const savedHandler: MutableRefObject<Handler<D> | null> = useRef(null);

  useEffect(() => {
    savedHandler.current = handler;
  }, [handler]);

  useEffect(() => {
    const eventListener = (event: MessageEvent<NuiMessageData<D>>) => {
      const { type: eventMethod, data } = event.data;
      if (savedHandler.current) {
        if (eventMethod === method) {
          savedHandler.current(data);
        }
      }
    };

    window.addEventListener("message", eventListener);

    return () => window.removeEventListener("message", eventListener);
  }, [method]);
};
