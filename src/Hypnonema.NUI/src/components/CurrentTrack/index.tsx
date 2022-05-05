import { FC, useEffect, useState } from "react";
import PlayArrowIcon from "@mui/icons-material/PlayArrow";
import PauseIcon from "@mui/icons-material/Pause";
import StopIcon from "@mui/icons-material/Stop";
import { IconButton, Slider, Stack, Tooltip, Typography } from "@mui/material";
import RepeatIcon from "@mui/icons-material/Repeat";
import { VolumePopover } from "../VolumePopover";

interface CurrentTrackProps {
  isPaused: boolean;
  startedAt: string;
  duration: number;
  repeat: boolean;
  volume: number;
  onVolumeChange: Function;
  onSeek: Function;
  onResume: Function;
  onPause: Function;
  onRepeat: Function;
  onStop: Function;
}

export const CurrentTrack: FC<CurrentTrackProps> = (props) => {
  const [startedAt, setStartedAt] = useState<string>(props.startedAt);

  const [currentTime, setCurrentTime] = useState<number>(
    Math.floor(
      (new Date().getTime() - new Date(`${startedAt}`).getTime()) / 1000
    )
  );

  const handleChange = (event: Event, newValue: number | number[]) => {
    setCurrentTime(newValue as number);
    props.onSeek(newValue as number);
  };

  useEffect(() => {
    const interval = setInterval(() => {
      if (props.isPaused) {
        return;
      }

      if (currentTime < props.duration) {
        setCurrentTime((currentTime) => currentTime + 1);
      } else {
        // start from beginning again if repeating
        // TODO: find correct way
        if (props.repeat) {
          setCurrentTime(0);
          setStartedAt(new Date().toISOString());
          return;
        }
        setCurrentTime(props.duration);
      }
    }, 1000);
    return () => clearInterval(interval);
  }, [currentTime, props.duration, props.isPaused, props.repeat]);

  function valueLabelFormat(value: number) {
    return new Date(1000 * value).toISOString().substr(11, 8);
  }

  const onPauseOrResume = () => {
    props.isPaused ? props.onResume() : props.onPause();
  };

  return (
    <div>
      <Stack direction="row" spacing={0} alignItems="center" sx={{ mb: 1 }}>
        <Tooltip title={props.isPaused ? "Play" : "Pause"} placement="top">
          <IconButton size="large" onClick={() => onPauseOrResume()}>
            {props.isPaused ? <PlayArrowIcon /> : <PauseIcon />}
          </IconButton>
        </Tooltip>
        <Tooltip title="Stop" placement="top">
          <IconButton size="large" onClick={() => props.onStop()}>
            <StopIcon />
          </IconButton>
        </Tooltip>
        <Tooltip title="Repeat" placement="top">
          <IconButton
            size="large"
            color={props.repeat ? "primary" : "inherit"}
            onClick={() => props.onRepeat(!props.repeat)}
          >
            <RepeatIcon />
          </IconButton>
        </Tooltip>
        <VolumePopover
          volume={props.volume}
          onVolumeChange={props.onVolumeChange}
        />
        <Stack
          spacing={2}
          direction="row"
          sx={{
            mb: 1,
            flexGrow: 1,
          }}
          alignItems="center"
        >
          <Typography sx={{ color: "text.secondary" }}>
            {new Date(1000 * currentTime || 0).toISOString().substr(11, 8)}
          </Typography>
          <Slider
            value={currentTime}
            valueLabelFormat={valueLabelFormat}
            valueLabelDisplay="auto"
            onChange={handleChange}
            max={props.duration}
            min={0}
          />
          <Typography sx={{ color: "text.secondary" }}>
            {new Date(1000 * props.duration || 0).toISOString().substr(11, 8)}
          </Typography>
        </Stack>
      </Stack>
    </div>
  );
};
