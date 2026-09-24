import { CSSProperties } from "react";
import { Tooltip, TooltipProps } from "@mui/material";
import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";
import Zoom from "@mui/material/Zoom";

const infoIconStyles = {
  color: "rgb(199 192 192)",
  marginLeft: "-8px",
  width: "16px",
  height: "16px",
};

interface InfoTooltipProps {
  tooltip: TooltipProps["title"];
  style?: CSSProperties;
}

const InfoTooltipIcon = ({ tooltip, style }: InfoTooltipProps) => {
  return (
    <Tooltip title={tooltip} slots={{ transition: Zoom }}>
      <InfoOutlinedIcon style={style} sx={infoIconStyles} />
    </Tooltip>
  );
};
export default InfoTooltipIcon;
