import { useState } from "react";
import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TextField,
} from "@mui/material";
import { useSnackbar } from "notistack";
import {
  AudioMode,
  type DefaultAudioConfiguration,
  NuiEvents,
  type ScaleformRenderConfiguration,
  type Screen,
} from "@hypnonema/generated-types";
import { useNuiEvent } from "@/hooks/useNuiEvent.ts";
import { post } from "@/utils/misc.ts";
import { DEFAULT_ATTENUATION } from "@/containers/ScreensView/audioValidation.ts";
import { copyScreenYaml } from "@/containers/ScreensView/screenToYaml.ts";
import { validateNewScreenName } from "@/containers/ScreensView/screenValidation.ts";

// Mirrors Hypnonema.Shared.Media.Screen.RenderDistance's default — the editor only sets the transform.
const DEFAULT_RENDER_DISTANCE = 200;

interface FinishPrompt {
  scaleform: ScaleformRenderConfiguration;
  existingIds: string[];
  // Ticket 08: pre-filled from the screen being edited/copied; blank for a brand-new screen.
  name: string;
  // True when editing an existing screen — "Save" sends UpdateScreen and the id-duplicate check is skipped.
  isEdit: boolean;
}

/**
 * The screen editor's "Fertigstellen" dialog (ticket 07). The native session grabs NUI focus and
 * sends `editorFinishOpen`; this asks for a name and either sends `CreateScreen` (via the native
 * `editorFinishSave` callback) or copies a paste-ready `screens.yaml` block. A `ScreenError` keeps
 * the dialog open with its values; a success closes the whole editor overlay (`editorClose`).
 */
export const EditorFinishDialog = () => {
  const { enqueueSnackbar } = useSnackbar();
  const [prompt, setPrompt] = useState<FinishPrompt | null>(null);
  const [name, setName] = useState("");
  const [errors, setErrors] = useState<string[]>([]);
  const [serverError, setServerError] = useState<string | undefined>(undefined);
  const [saving, setSaving] = useState(false);

  useNuiEvent<FinishPrompt>(NuiEvents.editorFinishOpen, (data) => {
    setPrompt(data);
    setName(data.name ?? "");
    setErrors([]);
    setServerError(undefined);
    setSaving(false);
  });
  useNuiEvent(NuiEvents.editorClose, () => setPrompt(null));
  useNuiEvent<string>(NuiEvents.editorFinishSaved, (savedName) => {
    enqueueSnackbar(`Screen “${savedName}” saved.`);
    setPrompt(null);
  });
  useNuiEvent<string>(NuiEvents.screenError, (message) => {
    setServerError(message);
    setSaving(false);
  });

  if (!prompt) return null;

  const back = () => {
    setPrompt(null);
    void post(NuiEvents.editorFinishCancel);
  };

  // The screen as it would land in screens.yaml: the editor only sets the transform, so
  // renderDistance / audio take the same defaults the server would apply on save.
  const draft: Screen = {
    name: name.trim(),
    scaleform: prompt.scaleform,
    renderDistance: DEFAULT_RENDER_DISTANCE,
    audio: {
      mode: AudioMode.Default,
      attenuation: DEFAULT_ATTENUATION,
    } as DefaultAudioConfiguration,
  };

  const save = () => {
    const found = validateNewScreenName(name, prompt.existingIds);
    setErrors(found);
    if (found.length > 0) return;

    setServerError(undefined);
    setSaving(true);
    // renderDistance and audio are left off — Screen fills its own defaults, and the detail view
    // (ticket 02/03) is where they get edited.
    void post(NuiEvents.editorFinishSave, {
      name: name.trim(),
      scaleform: prompt.scaleform,
    });
  };

  const messages = [...errors, ...(serverError ? [serverError] : [])];

  return (
    <Dialog
      open
      onClose={back}
      maxWidth="xs"
      fullWidth
      // Keep the placeholder surface behind the dialog undimmed (spec: "bleibt dahinter sichtbar").
      slotProps={{ backdrop: { sx: { backgroundColor: "transparent" } } }}
    >
      <DialogTitle>{prompt.isEdit ? "Save changes" : "Save this screen"}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <TextField
            label="Name"
            value={name}
            autoFocus
            fullWidth
            size="small"
            onChange={(event) => setName(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === "Enter") save();
            }}
          />
          {messages.length > 0 && (
            <Alert severity="error">
              {messages.map((message) => (
                <div key={message}>{message}</div>
              ))}
            </Alert>
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button color="inherit" onClick={back}>
          Back to editing
        </Button>
        <Button color="inherit" onClick={() => copyScreenYaml(draft, enqueueSnackbar)}>
          Copy YAML
        </Button>
        <Button variant="contained" onClick={save} disabled={saving}>
          Save
        </Button>
      </DialogActions>
    </Dialog>
  );
};
