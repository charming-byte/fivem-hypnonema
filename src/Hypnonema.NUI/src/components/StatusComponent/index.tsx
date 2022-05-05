import * as React from "react";
import { FC, useEffect, useState } from "react";
import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Typography,
} from "@mui/material";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import { DuiState } from "../../types/duiState";
import { CurrentTrack } from "../CurrentTrack";
import { useNuiEvent, useNuiRequest } from "fivem-nui-react-lib";

interface StatusComponentProps {
  duiState: DuiState;
  onSeek: Function;
  onStop: Function;
  onPause: Function;
  onResume: Function;
  onRepeat: Function;
  onVolume: Function;
}

export const StatusComponent: FC<StatusComponentProps> = (props) => {
  const [expanded, setExpanded] = React.useState<string | false>("panel1");

  const handleChange =
    (panel: string) => (event: React.SyntheticEvent, isExpanded: boolean) => {
      setExpanded(isExpanded ? panel : false);
    };

  const onSeek = (time: number) => {
    props.onSeek(time, props.duiState.screenName);
  };

  const onStop = () => {
    props.onStop(props.duiState.screenName);
  };

  const onResume = () => {
    props.onResume(props.duiState.screenName);
  };

  const onPause = () => {
    props.onPause(props.duiState.screenName);
  };

  const onRepeat = (repeat: boolean) => {
    props.onRepeat(props.duiState.screenName, repeat);
  };
  const { send } = useNuiRequest();

  const [volume, setVolume] = useState<number>(100);

  useEffect(() => {
    send(
      `getPlayerVolume:${props.duiState.screenName.replace(/\s+/g, "")}`
    ).then(() => {});
  }, [send, props.duiState.screenName]);

  useNuiEvent<number>(
    "hypnonema",
    `getPlayerVolume:${props.duiState.screenName.replace(/\s+/g, "")}`,
    (r) => setVolume(r)
  );

  const onVolumeChange = (volume: number) => {
    props.onVolume(volume, props.duiState.screenName);
  };

  return (
    <div style={{ marginTop: "1rem" }}>
      <Accordion
        expanded={expanded === "panel1"}
        onChange={handleChange("panel1")}
        sx={{ mt: "1rem" }}
        TransitionProps={{ unmountOnExit: true }}
      >
        <AccordionSummary expandIcon={<ExpandMoreIcon />} id="panel1b-header">
          <Typography sx={{ width: "33%", flexShrink: 0 }}>
            {props.duiState.screenName}
          </Typography>
          <Typography sx={{ color: "text.secondary" }}>
            {props.duiState.currentSource}
          </Typography>
        </AccordionSummary>
        <AccordionDetails>
          <CurrentTrack
            isPaused={props.duiState.isPaused}
            startedAt={props.duiState.startedAt}
            duration={props.duiState.duration}
            repeat={props.duiState.repeat}
            onSeek={onSeek}
            onStop={onStop}
            onPause={onPause}
            onRepeat={onRepeat}
            onResume={onResume}
            volume={volume}
            onVolumeChange={onVolumeChange}
          />
        </AccordionDetails>
      </Accordion>
    </div>
  );
};
