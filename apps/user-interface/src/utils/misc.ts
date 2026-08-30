declare global {
  interface Window {
    GetParentResourceName: () => string;
    invokeNative: () => void;
  }
}
export const isDevelopmentBrowser = (): boolean =>
  !window.invokeNative && import.meta.env.DEV;

export const getResourceName = (): string =>
  (window.GetParentResourceName && window.GetParentResourceName()) ??
  "hypnonema";

const formatTwoDigits = (value: number): string =>
  value < 10 ? `0${value}` : value.toString();

export const formatTime = (time: number): string => {
  if (time === -1) return "";

  const hours = Math.floor(time / 3600);
  const minutes = Math.floor((time % 3600) / 60);
  const seconds = Math.floor(time % 60);

  const formattedMinutes = formatTwoDigits(minutes);
  const formattedSeconds = formatTwoDigits(seconds);

  return hours > 0
    ? `${hours}:${formattedMinutes}:${formattedSeconds}`
    : `${formattedMinutes}:${formattedSeconds}`;
};

const baseURL = `https://${getResourceName()}/`;
const headers: Record<string, string> = {};

const setContentType = (contentType: string | undefined): void => {
  if (contentType) {
    headers["Content-Type"] = contentType;
  } else {
    delete headers["Content-Type"];
  }
};

const buildRequestOptions = (
  options: RequestInit & { parseResponse?: boolean },
  body?: BodyInit | null,
  method?: string,
): RequestInit => {
  const requestOptions: RequestInit = {
    ...options,
    headers: headers,
    signal: AbortSignal.timeout(4000),
  };

  if (body) {
    requestOptions.body = body;
    setContentType("application/json;charset=UTF-8");
  } else {
    setContentType(undefined);
  }

  if (method) {
    requestOptions.method = method;
  }

  return requestOptions;
};

const fetchJSON = async <T>(
  endpoint: string,
  options: RequestInit & { parseResponse?: boolean },
): Promise<T | undefined> => {
  const res = await fetch(baseURL + endpoint, options);

  if (!res.ok) {
    throw new Error(res.statusText);
  }

  const shouldParseResponse = options.parseResponse !== false;
  return shouldParseResponse && res.status !== 204 ? res.json() : undefined;
};

export const post = <T>(
  endpoint: string,
  body?: BodyInit | object,
  options: Omit<RequestInit, "body"> = {},
): Promise<T | undefined> => {
  const requestBody = body ? JSON.stringify(body) : null;
  const requestOptions = buildRequestOptions(options, requestBody, "POST");
  return fetchJSON(endpoint, requestOptions);
};

// yoinked from https://github.com/project-error/npwd/blob/d8dc5b7f47faf5fc581ffee30f31ff61d184cfe7/phone/src/os/phone/hooks/useClipboard.ts#L1
export const setClipboard = (value: string) => {
  const clipElem = document.createElement("textarea");
  clipElem.value = value;
  document.body.appendChild(clipElem);
  clipElem.select();
  document.execCommand("copy");
  document.body.removeChild(clipElem);
};
