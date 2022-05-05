import { FC } from "react";
import SimpleBar from "simplebar-react";
import styled from "styled-components";
import { Card, CardContent, Container } from "@mui/material";
import { StatusComponent } from "../StatusComponent";
import { DuiState } from "../../types/duiState";
import Typography from "@mui/material/Typography";

const Wrapper = styled.div`
  margin-top: 30px;
  display: flex;
  justify-content: center;
  flex-direction: column;
  align-items: center;
`;

interface StatusListProps {
  duiStates?: DuiState[];
  onSeek: Function;
  onPause: Function;
  onResume: Function;
  onStop: Function;
  onRepeat: Function;
  onVolume: Function;
}

export const StatusList: FC<StatusListProps> = (props) => {
  return (
    <div>
      <Wrapper>
        <Card
          sx={{
            width: "100%",
            maxWidth: "100%",
            boxShadow: "6",
          }}
        >
          <SimpleBar
            autoHide={false}
            style={{
              maxHeight: "420px",
            }}
          >
            <CardContent>
              <Container>
                {props.duiStates?.length !== 0 ? (
                  props.duiStates?.map((duiState, index) => (
                    <StatusComponent
                      key={index}
                      duiState={duiState}
                      onSeek={props.onSeek}
                      onStop={props.onStop}
                      onRepeat={props.onRepeat}
                      onPause={props.onPause}
                      onResume={props.onResume}
                      onVolume={props.onVolume}
                    />
                  ))
                ) : (
                  <Typography>Currently nothing is playing.</Typography>
                )}
              </Container>
            </CardContent>
          </SimpleBar>
        </Card>
      </Wrapper>
    </div>
  );
};
