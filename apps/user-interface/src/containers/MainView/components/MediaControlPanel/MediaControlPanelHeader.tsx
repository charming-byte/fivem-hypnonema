import { Stack, styled, Tooltip, Typography } from "@mui/material";
import { WarningAmber } from "@mui/icons-material";

const Container = styled(Stack)({});

interface StatusProps {
  text: string;
  severity: "error" | "warning";
}

const Status = ({ text, severity }: StatusProps) => (
  <Tooltip
    sx={{ minWidth: "3em" }}
    title={<Typography fontSize="small">{text}</Typography>}
  >
    <WarningAmber style={{ minWidth: 0 }} fontSize="large" color={severity} />
  </Tooltip>
);

const Label = styled(Typography)({
  fontWeight: 300,
  fontSize: "50px",
  lineHeight: "120%",
  letterSpacing: "-0.5px",
  alignSelf: "flex-end",
  whiteSpace: "nowrap",
  textOverflow: "ellipsis",
  maxWidth: "9em",
  overflow: "hidden",
  userSelect: "none",
});

const Distance = styled(Typography)({
  fontWeight: 400,
  fontSize: "1.25em",
  lineHeight: "123.5%",
  letterSpacing: "0.25px",
  alignSelf: "flex-end",
  userSelect: "none",
});

interface MediaControlPanelHeaderProps {
  label?: string;
  distance?: number;
  warning?: string;
  error?: string;
}

export const MediaControlPanelHeader = ({
  label = "",
  distance = 0,
  warning = "",
  error = "",
}: MediaControlPanelHeaderProps) => (
  <Container direction="row" alignItems="center" gap={2}>
    <Label variant="h3">{label}</Label>
    <Distance variant="h4" pb="0.4em">
      {distance}m away
    </Distance>

    {warning && warning.length > 0 && (
      <Status
        text={`The RenderTarget is already in use by  ${warning}.`}
        severity="warning"
      />
    )}
    {error && error.length > 0 && <Status text={error} severity="error" />}
  </Container>
);
