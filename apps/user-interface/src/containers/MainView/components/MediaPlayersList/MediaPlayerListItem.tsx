import { motion, type Variants } from "framer-motion";
import { MediaPlayerListItemButton } from "./MediaPlayerListItemButton.tsx";
import { ListItemText, Stack, Tooltip } from "@mui/material";
import { amber } from "@mui/material/colors";
import { Campaign } from "@mui/icons-material";
import { MediaPlayer } from "@/models/mediaPlayer.ts";
import { MediaBars } from "@/containers/MainView/components/MediaBars/MediaBars.tsx";

const Wrapper = motion.create(Stack);

const mediaPlayerListItem: Variants = {
  hidden: { opacity: 0, x: -250 },
  show: {
    opacity: 1,
    x: 0,
    transition: { type: "spring", damping: 12, stiffness: 100 },
  },
};

interface MediaPlayerListItemProps {
  mediaPlayer: MediaPlayer;
  selectedItem?: MediaPlayer;
  handleSelect: (mediaPlayer: MediaPlayer) => void;
}

export const ListItem = ({
  mediaPlayer,
  selectedItem,
  handleSelect,
}: MediaPlayerListItemProps) => (
  <Wrapper
    variants={mediaPlayerListItem}
    key={mediaPlayer.handle}
    direction="row"
    style={{ position: "relative" }}
  >
    <MediaPlayerListItemButton
      selected={selectedItem?.handle == mediaPlayer.handle}
      aria-selected={selectedItem?.handle == mediaPlayer.handle}
      onClick={() => handleSelect(mediaPlayer)}
      whileHover={{ scale: 1.02 }}
      whileTap={{ scale: 0.96 }}
      transition={{ type: "spring", damping: 17, stiffness: 150 }}
      disableRipple
    >
      <ListItemText
        primary={mediaPlayer.label}
        secondary={`${mediaPlayer.distance}m away`}
      />
    </MediaPlayerListItemButton>
    {mediaPlayer.isAdActive ? (
      <Tooltip title={`${mediaPlayer.label} is waiting for an ad to finish.`}>
        <Campaign
          style={{ position: "absolute", top: "1.2em", right: "1.2em" }}
          sx={{ color: amber[500] }}
        />
      </Tooltip>
    ) : mediaPlayer.hasTrack && mediaPlayer.isPlaying ? (
      <MediaBars
        tooltip={`${mediaPlayer.label} is playing.`}
        style={{ position: "absolute", top: "1.4em", right: "1.2em" }}
      />
    ) : null}
  </Wrapper>
);
