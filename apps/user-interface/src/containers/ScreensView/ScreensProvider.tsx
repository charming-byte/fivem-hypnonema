import { type PropsWithChildren, useEffect, useState } from "react";
import {
  AudioMode,
  NuiEvents,
  type UserInterfaceScreen,
} from "@hypnonema/generated-types";
import { useNuiEvent } from "@/hooks/useNuiEvent.ts";
import { isDevelopmentBrowser } from "@/utils/misc.ts";
import { ScreensContext } from "./screensContext.ts";
import { DEFAULT_ATTENUATION } from "./audioValidation.ts";

const byDistance = (a: UserInterfaceScreen, b: UserInterfaceScreen) =>
  a.distance - b.distance;

const createFakeScreens = (): UserInterfaceScreen[] =>
  [
    { id: "lounge-tv", name: "Lounge TV", distance: 12.4, isPlaying: true },
    { id: "bar-screen", name: "Bar Screen", distance: 38.1, isPlaying: false },
    {
      id: "garage-wall",
      name: "Garage Wall",
      distance: 140.7,
      isPlaying: false,
    },
  ].map(({ id, name, distance, isPlaying }) => ({
    distance,
    isPlaying,
    screen: {
      id,
      name,
      renderDistance: 200,
      audio: { mode: AudioMode.Default, attenuation: DEFAULT_ATTENUATION },
      scaleform: {
        transform: {
          position: { x: 100.5, y: -230.2, z: 45.1 },
          rotation: { x: 0, y: 0, z: 90 },
          scale: { x: 0.97, y: 0.485, z: -0.1 },
        },
        texture: {
          position: { x: 0, y: 0 },
          dimension: { width: 1280, height: 720 },
        },
      },
    },
  }));

export const ScreensProvider = ({ children }: PropsWithChildren) => {
  const [screens, setScreens] = useState<UserInterfaceScreen[]>([]);
  const [error, setError] = useState<string | undefined>(undefined);

  useNuiEvent<string>(NuiEvents.updateScreens, (data) => {
    const parsed = JSON.parse(data) as UserInterfaceScreen[];
    setScreens([...parsed].sort(byDistance));
  });

  useNuiEvent<string>(NuiEvents.screenError, (message) => setError(message));

  // Nothing refreshes the list while the UI is closed - drop it so the next open starts clean.
  useNuiEvent(NuiEvents.closeUi, () => {
    setScreens([]);
    setError(undefined);
  });

  useEffect(() => {
    if (isDevelopmentBrowser())
      setScreens(createFakeScreens().sort(byDistance));
  }, []);

  return (
    <ScreensContext.Provider
      value={{ screens, error, clearError: () => setError(undefined) }}
    >
      {children}
    </ScreensContext.Provider>
  );
};
