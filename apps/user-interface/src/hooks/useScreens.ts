import { useContext } from "react";
import { ScreensContext } from "@/containers/ScreensView/screensContext.ts";

export const useScreens = () => useContext(ScreensContext);
