import { AnimatedLayout } from "@components/AnimatedLayout/AnimatedLayout.tsx";
import {
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Divider,
  styled,
} from "@mui/material";
import { useState } from "react";
import { useNavigate } from "react-router";
import { SettingListItemButton } from "@/containers/SettingsView/components/SettingsList/SettingsListItemButton.tsx";
import { post } from "@/utils/misc.ts";
import { NuiEvents } from "@hypnonema/generated-types";

const Wrapper = styled(Box)({
  width: "100%",
  height: "100%",
  display: "flex",
  alignItems: "flex-start",
  background: "linear-gradient(89.96deg, #2C2B2B 38.71%, #212121 99.96%)",
  boxShadow:
    "inset 0px -1px 0px rgba(0, 0, 0, 0.6), inset 0px -2px 3px 0px rgba(0, 0, 0, 0.4), -1px -2px 0px rgba(0, 0, 0, 0.7), 1px 0px 0 rgba(0, 0, 0, 0.7)",
});

export const SettingsView = () => {
  const [confirmOpen, setConfirmOpen] = useState(false);
  const navigate = useNavigate();

  const closeConfirm = () => setConfirmOpen(false);

  const confirmReset = () => {
    post(NuiEvents.reset);
    closeConfirm();
  };

  return (
    <AnimatedLayout
      style={{
        display: "flex",
        gap: "32px",
        alignItems: "flex-start",
        justifyContent: "center",
        padding: "72px 72px 72px 72px",
        width: "100%",
        flexDirection: "row",
      }}
    >
      <Wrapper>
        <Box
          style={{ width: "100%", display: "flex", flexDirection: "column" }}
        >
          <SettingListItemButton
            title="Screens"
            buttonText="Open"
            helperText="Browse, inspect and delete playback surfaces."
            onClick={() => navigate("/settings/screens")}
          />
          <Divider sx={{ borderColor: "rgba(255,255,255,0.13)" }} />

          <SettingListItemButton
            title="Reset"
            helperText="Restart the client if something goes wrong."
            onClick={() => setConfirmOpen(true)}
          />
          <Divider sx={{ borderColor: "rgba(255,255,255,0.13)" }} />

          {/* <SettingListItemSwitch
            value={audioSwitchValue}
            title="Audio Mode"
            helperText="Switch between stereo audio and spatialized 3D audio mode."
            leftLabel="Stereo"
            rightLabel="3D Sound"
            onToggle={(checked) => {
              setAudioSwitchValue(!audioSwitchValue);
              post(NuiEvents.setAudioMode, {
                mode: checked ? AudioMode.Spatial : AudioMode.Default,
              });
            }}
          />*/}
        </Box>
      </Wrapper>

      <Dialog open={confirmOpen} onClose={closeConfirm}>
        <DialogTitle>Reset client?</DialogTitle>
        <DialogContent>
          <DialogContentText>
            This closes every media player currently rendered on your client and
            rebuilds them from the current server state. Playback for other
            players is not affected.
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button color="inherit" onClick={closeConfirm}>
            Cancel
          </Button>
          <Button color="error" onClick={confirmReset}>
            Reset
          </Button>
        </DialogActions>
      </Dialog>
    </AnimatedLayout>
  );
};
