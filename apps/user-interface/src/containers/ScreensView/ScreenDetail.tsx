import { Fragment, type ReactNode, useState } from "react";
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Stack,
  Tooltip,
  Typography,
} from "@mui/material";
import type {
  DefaultAudioConfiguration,
  DistanceAttenuation,
  Screen,
  UserInterfaceScreen,
  Vector3,
} from "@hypnonema/generated-types";
import { AudioMode, NuiEvents } from "@hypnonema/generated-types";
import { useSnackbar } from "notistack";
import { Accordion } from "@components/Accordion";
import InfoTooltipIcon from "@components/InfoTooltipIcon";
import { Select } from "@components/Select/Select.tsx";
import { NumberInput } from "@/containers/MainView/components/NumberInput/NumberInput.tsx";
import { post } from "@/utils/misc.ts";
import { screenId } from "./screenId.ts";
import { copyScreenYaml } from "./screenToYaml.ts";
import {
  DEFAULT_ATTENUATION,
  renderDistanceWarning,
  validateScreenAudio,
} from "./audioValidation.ts";

const AXES = ["x", "y", "z"] as const;

const AUDIO_MODE_ITEMS = [{ label: "Default", value: AudioMode.Default }];

// Shared so the audio-mode select and the number inputs render at the same width.
const CONTROL_WIDTH = "12rem";

const HELP = {
  renderDistance:
    "Distance in metres at which the screen stops drawing and drops off the nearby list. Keep the audio max distance at or below this.",
  audioMode:
    "How playback audio is placed in the world. “Default” fades with distance and drops when the line of sight to the screen is blocked.",
  minDistance:
    "Within this distance (metres) the audio plays at full volume; past it, it starts to fade.",
  maxDistance:
    "Distance (metres) at which the audio reaches silence. It fades from full volume at the min distance down to nothing here.",
  occludedVolume:
    "Fraction of the volume (0–1) that is kept when a wall fully blocks the line of sight to the screen.",
};

const getAttenuation = (screen: Screen): DistanceAttenuation =>
  (screen.audio as DefaultAudioConfiguration | undefined)?.attenuation ??
  DEFAULT_ATTENUATION;

// One shared grid so the Transform values and the Rendering & Audio fields sit on the same
// label / value columns. Column 1 is the label; columns 2-4 hold the three transform axes,
// and a single field spans them.
const RowGrid = ({ children }: { children: ReactNode }) => (
  <Box
    sx={{
      display: "grid",
      gridTemplateColumns: "auto repeat(3, 1fr)",
      columnGap: 3,
      rowGap: 1,
      alignItems: "center",
      width: "100%",
    }}
  >
    {children}
  </Box>
);

const RowLabel = ({ children, help }: { children: string; help?: string }) => (
  <Stack direction="row" alignItems="center" spacing={0.8}>
    <Typography variant="body2" color="text.secondary">
      {children}
    </Typography>
    {help && <InfoTooltipIcon tooltip={help} style={{ marginLeft: 0 }} />}
  </Stack>
);

const Num = ({ children }: { children: string }) => (
  <Typography
    variant="body2"
    fontFamily="monospace"
    textAlign="right"
    sx={{ fontVariantNumeric: "tabular-nums" }}
  >
    {children}
  </Typography>
);

const Field = ({ children }: { children: ReactNode }) => (
  <Box sx={{ gridColumn: "2 / -1" }}>{children}</Box>
);

const TransformSection = ({
  rows,
}: {
  rows: { label: string; vector: Vector3 }[];
}) => (
  <RowGrid>
    <span />
    {AXES.map((axis) => (
      <Typography
        key={axis}
        variant="caption"
        color="text.secondary"
        textAlign="right"
      >
        {axis}
      </Typography>
    ))}
    {rows.map(({ label, vector }) => (
      <Fragment key={label}>
        <RowLabel>{label}</RowLabel>
        {AXES.map((axis) => (
          <Num key={axis}>{vector[axis].toFixed(3)}</Num>
        ))}
      </Fragment>
    ))}
  </RowGrid>
);

interface ScreenDetailProps {
  item: UserInterfaceScreen;
  onBack: () => void;
  onDeleted: () => void;
}

export const ScreenDetail = ({
  item,
  onBack,
  onDeleted,
}: ScreenDetailProps) => {
  const { enqueueSnackbar } = useSnackbar();
  const [confirmOpen, setConfirmOpen] = useState(false);
  const { screen, isPlaying } = item;
  const { transform } = screen.scaleform;

  const id = screenId(screen);

  const [renderDistance, setRenderDistance] = useState(screen.renderDistance);
  const [mode, setMode] = useState<AudioMode>(
    screen.audio?.mode ?? AudioMode.Default,
  );
  const [attenuation, setAttenuation] = useState<DistanceAttenuation>(
    getAttenuation(screen),
  );

  // Re-seed the form when the user drills into a different screen (React's "adjust state
  // during render" pattern - no effect, no dependency list to keep in sync).
  const [seededId, setSeededId] = useState(id);
  if (seededId !== id) {
    setSeededId(id);
    setRenderDistance(screen.renderDistance);
    setMode(screen.audio?.mode ?? AudioMode.Default);
    setAttenuation(getAttenuation(screen));
  }

  const setField =
    (field: keyof DistanceAttenuation) => (value: number | null) =>
      setAttenuation((current) => ({ ...current, [field]: value ?? 0 }));

  const audioErrors = validateScreenAudio(attenuation);
  const warning = renderDistanceWarning(
    attenuation.maxDistance,
    renderDistance,
  );

  const save = async () => {
    if (audioErrors.length > 0) return;

    const updated: Screen = {
      ...screen,
      id,
      renderDistance,
      audio: { mode, attenuation } as DefaultAudioConfiguration,
    };

    await post(NuiEvents.updateScreen, updated);
  };

  const confirmDelete = async () => {
    await post(NuiEvents.deleteScreen, { id });
    setConfirmOpen(false);
    onDeleted();
  };

  return (
    <Box width="100%" sx={{ px: 2.5, py: 1.5 }}>
      <Button color="inherit" size="small" onClick={onBack} sx={{ mb: 2 }}>
        ← Back
      </Button>

      <Typography variant="h6">{screen.name}</Typography>
      <Typography variant="caption" color="text.secondary">
        {id}
        {isPlaying ? " • playing" : ""}
      </Typography>

      <Box mt={3}>
        <Accordion title="Transform" isOpen>
          <TransformSection
            rows={[
              { label: "Position", vector: transform.position },
              { label: "Rotation", vector: transform.rotation },
              { label: "Scale", vector: transform.scale },
            ]}
          />
        </Accordion>
      </Box>

      <Box mt={3}>
        <Accordion title="Rendering & Audio" isOpen>
          <Stack spacing={3} width="100%">
            <RowGrid>
              <RowLabel help={HELP.renderDistance}>Render distance</RowLabel>
              <Field>
                <NumberInput
                  width={CONTROL_WIDTH}
                  value={renderDistance}
                  step={5}
                  min={0}
                  onValueChange={(value) => setRenderDistance(value ?? 0)}
                />
              </Field>

              <RowLabel help={HELP.audioMode}>Audio mode</RowLabel>
              <Field>
                <Select
                  width={CONTROL_WIDTH}
                  value={mode}
                  items={AUDIO_MODE_ITEMS}
                  onChange={(value) => setMode(Number(value) as AudioMode)}
                />
              </Field>

              <RowLabel help={HELP.minDistance}>Audio min distance</RowLabel>
              <Field>
                <NumberInput
                  width={CONTROL_WIDTH}
                  value={attenuation.minDistance}
                  step={1}
                  min={0}
                  onValueChange={setField("minDistance")}
                />
              </Field>

              <RowLabel help={HELP.maxDistance}>Audio max distance</RowLabel>
              <Field>
                <NumberInput
                  width={CONTROL_WIDTH}
                  value={attenuation.maxDistance}
                  step={1}
                  min={0}
                  onValueChange={setField("maxDistance")}
                />
              </Field>

              <RowLabel help={HELP.occludedVolume}>Occluded volume</RowLabel>
              <Field>
                <NumberInput
                  width={CONTROL_WIDTH}
                  value={attenuation.occludedVolume}
                  step={0.05}
                  min={0}
                  max={1}
                  onValueChange={setField("occludedVolume")}
                />
              </Field>
            </RowGrid>

            {warning && <Alert severity="warning">{warning}</Alert>}
            {audioErrors.length > 0 && (
              <Alert severity="error">
                {audioErrors.map((message) => (
                  <div key={message}>{message}</div>
                ))}
              </Alert>
            )}

            <Box>
              <Button
                variant="contained"
                size="small"
                disabled={audioErrors.length > 0}
                onClick={save}
              >
                Save
              </Button>
            </Box>
          </Stack>
        </Accordion>
      </Box>

      <Stack direction="row" spacing={1} mt={3}>
        <Button
          variant="text"
          color="inherit"
          onClick={() => post(NuiEvents.editorStart, { id, asTemplate: false })}
        >
          Edit
        </Button>
        <Button
          variant="text"
          color="inherit"
          onClick={() => post(NuiEvents.editorStart, { id, asTemplate: true })}
        >
          Copy as template
        </Button>
        <Button
          variant="text"
          color="inherit"
          onClick={() => copyScreenYaml(screen, enqueueSnackbar)}
        >
          Copy YAML
        </Button>
        <Tooltip title={isPlaying ? "Stop playback first" : ""}>
          <span>
            <Button
              variant="text"
              color="error"
              disabled={isPlaying}
              onClick={() => setConfirmOpen(true)}
            >
              Delete
            </Button>
          </span>
        </Tooltip>
      </Stack>

      <Dialog open={confirmOpen} onClose={() => setConfirmOpen(false)}>
        <DialogTitle>Delete screen “{screen.name}”?</DialogTitle>
        <DialogContent>
          <DialogContentText>
            This removes the screen from <code>screens.yaml</code> for every
            connected player. It cannot be undone from here.
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button color="inherit" onClick={() => setConfirmOpen(false)}>
            Cancel
          </Button>
          <Button color="error" onClick={confirmDelete}>
            Delete
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};
