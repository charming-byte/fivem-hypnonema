import { FC, useMemo } from "react";
import { Container, Typography } from "@mui/material";
import { useAppSelector } from "../hooks/store";
import { StatusList } from "../components/StatusList";
import { useNuiRequest } from "fivem-nui-react-lib";
import { debounce } from "lodash";

export const StatusPage: FC = () => {
  const duiStates = useAppSelector((state) => state.dui.duiStates);
  const { send } = useNuiRequest();

  const onSeekHandler = useMemo(
    () =>
      debounce((time: number, screenName: string) => {
        send("seek", { screenName, time }).then(() => {});
      }, 300),
    [send]
  );

  const onVolumeHandler = useMemo(
    () =>
      debounce((volume: number, screenName: string) => {
        send(`setPlayerVolume:${screenName.replace(/\s+/g, "")}`, {
          volume,
        }).then(() => {});
      }, 300),
    [send]
  );

  const onStop = (screenName: string) => {
    send("stop", { screenName }).then(() => {});
  };

  const onPause = (screenName: string) => {
    send("pause", { screenName }).then(() => {});
  };

  const onResume = (screenName: string) => {
    send("resume", { screenName }).then(() => {});
  };
  const onRepeat = (screenName: string, repeat: boolean) => {
    send("repeat", { screenName, repeat }).then(() => {});
  };

  return (
    <Container>
      <Typography sx={{ marginTop: "50px", marginBottom: "10px" }} variant="h5">
        Status
      </Typography>
      <StatusList
        duiStates={duiStates}
        onSeek={onSeekHandler}
        onVolume={onVolumeHandler}
        onStop={onStop}
        onResume={onResume}
        onPause={onPause}
        onRepeat={onRepeat}
      />
    </Container>
  );
};
