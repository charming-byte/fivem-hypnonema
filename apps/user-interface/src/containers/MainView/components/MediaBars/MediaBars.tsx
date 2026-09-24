import { motion } from "framer-motion";
import styled from "styled-components";
import type { CSSProperties } from "react";
import Zoom from "@mui/material/Zoom";
import { Tooltip } from "@mui/material";

const Wrapper = styled.div`
  display: flex;
  align-items: flex-end;
  gap: 0.15em;
  height: 0.6em;
`;

const Bar = motion.create(styled.div`
  width: 0.2em;
  border-radius: 999px;
  background: white;
`);

export interface MediaBarsProps {
  style?: CSSProperties;
  barCount?: number;
  tooltip?: string;
}

export const MediaBars = ({
  style,
  barCount = 4,
  tooltip = "",
}: MediaBarsProps) => {
  return (
    <Tooltip
      title={tooltip}
      placement="top"
      slots={{
        transition: Zoom,
      }}
    >
      <Wrapper style={style}>
        {Array.from({ length: barCount }).map((_, index) => (
          <Bar
            key={index}
            className="w-2 rounded-full bg-gray-400"
            animate={{
              height: ["20%", "100%", "20%"],
            }}
            transition={{
              duration: 1,
              repeat: Infinity,
              ease: "easeInOut",
              delay: index * 0.11,
            }}
          />
        ))}
      </Wrapper>
    </Tooltip>
  );
};
