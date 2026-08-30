import { type SvgIconProps, Tooltip, type TooltipProps } from "@mui/material";
import {
  cloneElement,
  type CSSProperties,
  isValidElement,
  type MouseEvent,
  type PropsWithChildren,
} from "react";
import StyledIconButton from "./StyledIconButton.tsx";
import Zoom from "@mui/material/Zoom";

export type IconButtonProps = {
  disabled?: boolean;
  onClick?: (event: MouseEvent<HTMLButtonElement>) => void;
  tooltip?: string;
  tooltipPlacement?: TooltipProps["placement"];
  spanStyle?: CSSProperties;
  size?: "small" | "medium" | "large";
  iconSize?: CSSProperties["width"];
};

export const IconButton = ({
  disabled = false,
  onClick = () => {},
  tooltip = "",
  tooltipPlacement = "top",
  spanStyle = {},
  size = "medium",
  iconSize = "56px",
  children,
}: PropsWithChildren<IconButtonProps>) => {
  children =
    isValidElement<SvgIconProps>(children) &&
    cloneElement(children, {
      ...children.props,
      sx: { width: iconSize, height: iconSize, ...children.props.sx },
    });

  return (
    <Tooltip
      title={tooltip}
      placement={tooltipPlacement}
      slots={{ transition: Zoom }}
    >
      <span style={spanStyle}>
        <StyledIconButton
          disabled={disabled}
          size={size}
          onClick={onClick}
          whileHover={{ scale: 1.1 }}
          whileTap={{ scale: 0.9 }}
          disableRipple
        >
          {children}
        </StyledIconButton>
      </span>
    </Tooltip>
  );
};

export default IconButton;
