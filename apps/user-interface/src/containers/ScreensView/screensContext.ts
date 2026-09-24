import { createContext } from "react";
import type { UserInterfaceScreen } from "@hypnonema/generated-types";

export type ScreensContextType = {
  screens: UserInterfaceScreen[];
  error?: string;
  clearError: () => void;
};

export const defaultScreensContextValue: ScreensContextType = {
  screens: [],
  error: undefined,
  clearError: () => {},
};

export const ScreensContext = createContext<ScreensContextType>(
  defaultScreensContextValue,
);
