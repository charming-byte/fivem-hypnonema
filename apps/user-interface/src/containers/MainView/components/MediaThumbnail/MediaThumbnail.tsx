import { Box, Popover, Stack, styled, Typography } from "@mui/material";
import { type CSSProperties, type MouseEvent, useState } from "react";
import Zoom from "@mui/material/Zoom";
import albumPlaceholder from "@/assets/album-placeholder.jpg";
import type { ITrack } from "@hypnonema/generated-types";

const Background = styled(Box)({
  boxSizing: "border-box",
  background: "linear-gradient(89.96deg, #2C2B2B 38.71%, #212121 99.96%)",
  maxWidth: "185px",
  padding: "8px",
  display: "flex",
  flexGrow: 1,
  height: "100%",
  justifyContent: "center",
  position: "relative",
  alignItems: "center",
  boxShadow:
    "0px 0px 5px 2px #000000, 1px 0px 0px #000000, 0px 1px 0px #000000, 0px -1px 0px #000000,  -1px 0px 0px #000000",
  borderRadius: "3px",
  ":before": {
    content: '""',
    position: "absolute",
    top: 0,
    right: 0,
    left: 0,
    bottom: 0,
    zIndex: -1,
    margin: "-1px",
    borderRadius: "inherit",
    boxShadow: "0px -1px 20px 1px rgba(0, 0, 0, 0.7)",
    background:
      "linear-gradient(182deg, rgba(20, 20, 20, 1) 93%, rgba(102, 102, 102, 1) 100%)",
  },
});

const ImageWrapper = styled(Box)({
  boxShadow: "0 0 0 1px rgba(0,0,0,1)",
  maxHeight: "140px",
  margin: "6px",
  display: "flex",
  flexGrow: 1,
  height: "100%",
  justifyContent: "center",
  borderRadius: "4px",
  alignItems: "center",
});

const Image = styled("img")({
  borderRadius: "3px",
  objectFit: "fill",
  display: "flex",
  flexGrow: 1,
  height: "100%",
});

const TOOLTIP_MAX_WIDTH = "320px";

const Tooltip = ({
  track,
  onClose,
  open,
  anchorEl,
}: {
  track: ITrack;
  onClose: () => void;
  open: boolean;
  anchorEl: Element | null;
}) => {
  return (
    <Popover
      sx={{ pointerEvents: "none" }}
      open={open}
      onClose={onClose}
      anchorEl={anchorEl}
      anchorOrigin={{ vertical: "top", horizontal: "center" }}
      transformOrigin={{ vertical: "bottom", horizontal: "center" }}
      TransitionComponent={Zoom}
      slotProps={{ paper: { sx: { maxWidth: TOOLTIP_MAX_WIDTH } } }}
    >
      <Stack direction="column" p={2} maxWidth={TOOLTIP_MAX_WIDTH}>
        <Typography variant="subtitle1" noWrap>
          {track.title}
        </Typography>
        <Typography variant="subtitle2" noWrap>
          {track.url}
        </Typography>
      </Stack>
    </Popover>
  );
};

const getThumbnail = (track?: ITrack) => {
  return track && track.thumbnailUrl != ""
    ? track.thumbnailUrl
    : albumPlaceholder;
};

interface ThumbnailProps {
  track?: ITrack;
  width?: CSSProperties["width"];
  height?: CSSProperties["height"];
}

export const MediaThumbnail = ({
  track,
  width = "64px",
  height = "64px",
}: ThumbnailProps) => {
  const [anchorEl, setAnchorEl] = useState<Element | null>(null);
  const tooltipVisible = Boolean(anchorEl);

  const handleClose = () => setAnchorEl(null);
  const handleMouseLeave = () => setAnchorEl(null);
  const handleMouseEnter = (event: MouseEvent<HTMLDivElement>) =>
    setAnchorEl(event.currentTarget);

  return (
    <>
      <Background
        onMouseEnter={handleMouseEnter}
        onMouseLeave={handleMouseLeave}
      >
        <ImageWrapper>
          <Image
            width={width}
            height={height}
            src={getThumbnail(track)}
            alt="thumbnail"
          />
        </ImageWrapper>
      </Background>
      {track && (
        <Tooltip
          track={track}
          onClose={handleClose}
          open={tooltipVisible}
          anchorEl={anchorEl}
        />
      )}
    </>
  );
};
