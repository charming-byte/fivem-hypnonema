import { useSnackbar } from "notistack";
import { useNuiEvent } from "./useNuiEvent.ts";

export const useToast = () => {
  const { enqueueSnackbar, closeSnackbar } = useSnackbar();
  // TODO: Check what type is actually passed from client script
  useNuiEvent("showToast", (message: string) => enqueueSnackbar(message));

  return { showToast: enqueueSnackbar, closeToast: closeSnackbar };
};
