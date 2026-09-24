import { type Control, type FieldValues, useForm } from "react-hook-form";
import ReactPlayer from "react-player";
import { Button as MuiButton, InputAdornment, styled } from "@mui/material";
import { PlayArrow } from "@mui/icons-material";
import { motion } from "framer-motion";
import PublicIcon from "@mui/icons-material/Public";

import TextField from "./TextField.tsx";
import type { ReactElement } from "react";
import type { TextFieldProps as MuiTextFieldProps } from "@mui/material/TextField/TextField";
import { useMediaPlayers } from "@/hooks/useMediaPlayers.ts";
import { getOEmbedFromURL } from "@/utils/getOEmbedFromURL.ts";

const Button = styled(motion.create(MuiButton))({
  boxShadow:
    "0px 1px 0px #000000, 0px -1px 0px #000000, 0px 3px 1px -2px rgba(0, 0, 0, 0.2), 0px 2px 2px rgba(0, 0, 0, 0.14), 0px 1px 5px rgba(0, 0, 0, 0.12)",
});

const Form = styled("form")({
  display: "flex",
  gap: "32px",
  alignItems: "center",
  padding: "16px 16px 16px 16px",
  userSelect: "none",
  background: "linear-gradient(89.96deg, #2C2B2B 38.71%, #212121 99.96%)",
  boxShadow:
    "inset 0px 2px 0px rgba(0,0,0,0.6), 0px 1px 0px rgba(0,0,0,0.4), 1px 0px 0px rgba(0,0,0,0.7),-1px 0 0 rgba(0,0,0,0.7)",
  boxSizing: "border-box",
  width: "100%",
  flexDirection: "row",
  height: "97px",
});

export type TextFieldProps = {
  name: string;
  //eslint-disable-next-line
  control: Control<any>;
  label: string;
  placeholder?: string;
  required?: boolean;
  startAdornment: ReactElement;
  variant?: MuiTextFieldProps["variant"];
  disabled?: boolean;
};

export type UrlInputValues = FieldValues & {
  url: string;
};

const UrlInput = () => {
  const { selectedMediaPlayer } = useMediaPlayers();
  const { handleSubmit, setError, reset, control } = useForm<UrlInputValues>({
    defaultValues: { url: "" },
  });

  const processMediaDetails = async (url: string): Promise<void> => {
    const { title, thumbnailUrl } = await getOEmbedFromURL(url);
    selectedMediaPlayer?.play(
      url,
      title,
      thumbnailUrl,
      selectedMediaPlayer.target,
    );
    reset({ url: "" });
  };

  const handlePlay = async (formData: UrlInputValues) => {
    if (ReactPlayer.canPlay(formData.url)) {
      await processMediaDetails(formData.url);
    } else {
      setError("url", { type: "custom", message: "URL cannot be played." });
    }
  };

  return (
    <Form onSubmit={handleSubmit(handlePlay)}>
      <TextField
        placeholder="Enter a URL"
        name="url"
        control={control}
        label="URL"
        variant="standard"
        startAdornment={
          <InputAdornment position="start">
            <PublicIcon />
          </InputAdornment>
        }
        required
      />
      <Button
        whileTap={{ scale: 0.9 }}
        whileHover={{ scale: 1.02 }}
        transition={{ duration: 0.1 }}
        variant="contained"
        startIcon={<PlayArrow />}
        type="submit"
      >
        Play
      </Button>
    </Form>
  );
};

export default UrlInput;
