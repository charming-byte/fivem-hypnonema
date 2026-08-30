import { NumberField } from "@base-ui-components/react/number-field";
import { ComponentProps, type KeyboardEvent, useId, useState } from "react";
import { Typography as MuiTypography } from "@mui/material";
import styled from "styled-components";
import { motion } from "framer-motion";

function PlusIcon(props: ComponentProps<"svg">) {
  return (
    <svg
      width="10"
      height="10"
      viewBox="0 0 10 10"
      fill="none"
      stroke="currentcolor"
      strokeWidth="1.6"
      xmlns="http://www.w3.org/2000/svg"
      {...props}
    >
      <path d="M0 5H5M10 5H5M5 5V0M5 5V10" />
    </svg>
  );
}

function MinusIcon(props: ComponentProps<"svg">) {
  return (
    <svg
      width="10"
      height="10"
      viewBox="0 0 10 10"
      fill="none"
      stroke="currentcolor"
      strokeWidth="1.6"
      xmlns="http://www.w3.org/2000/svg"
      {...props}
    >
      <path d="M0 5H10" />
    </svg>
  );
}

const Root = styled(NumberField.Root)<{ $width?: string }>`
  display: flex;
  flex-direction: row;
  align-items: center;
  gap: 0.5rem;
  ${({ $width }) => $width && `width: ${$width};`}
`;

const Group = styled(NumberField.Group)<{ $fill?: boolean }>`
  display: flex;
  gap: 0.2rem;
  ${({ $fill }) => $fill && "flex: 1 1 auto; min-width: 0;"}
`;

const Decrement = styled(motion.create(NumberField.Decrement))`
  position: relative;
  box-sizing: border-box;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 2rem;
  height: 2rem;
  margin: 0;
  outline: 0;
  padding: 0;
  border: 1px solid oklch(12% 9% 264 / 8%);
  background-color: #5f0877;
  background-clip: padding-box;
  color: #fff;
  user-select: none;

  @media (hover: hover) {
    &:hover {
      background-color: rgb(66, 5, 83);
      box-shadow:
        0 2px 4px -1px rgba(0, 0, 0, 0.2),
        0 4px 5px 0 rgba(0, 0, 0, 0.14),
        0 1px 10px 0 rgba(0, 0, 0, 0.12);
    }
  }

  &[disabled] {
    pointer-events: none;
    background-color: rgba(255, 255, 255, 0.12);
  }

  &:active {
    background-color: rgb(66, 5, 83);
    box-shadow:
      0 2px 4px -1px rgba(0, 0, 0, 0.2),
      0 4px 5px 0 rgba(0, 0, 0, 0.14),
      0 1px 10px 0 rgba(0, 0, 0, 0.12);
  }

  border-radius: 0.275rem 0 0 0.275rem;
`;
const Increment = styled(motion.create(NumberField.Increment))`
  box-sizing: border-box;
  display: flex;
  align-items: center;
  justify-content: center;

  &[disabled] {
    pointer-events: none;
    background-color: rgba(255, 255, 255, 0.12);
  }

  width: 2rem;
  height: 2rem;
  margin: 0;
  outline: 0;
  padding: 0;
  border: 1px solid oklch(12% 9% 264 / 8%);
  background-color: #5f0877;
  background-clip: padding-box;
  color: #fff;
  user-select: none;

  @media (hover: hover) {
    &:hover {
      background-color: rgb(66, 5, 83);
      box-shadow:
        0 2px 4px -1px rgba(0, 0, 0, 0.2),
        0 4px 5px 0 rgba(0, 0, 0, 0.14),
        0 1px 10px 0 rgba(0, 0, 0, 0.12);
    }
  }

  &:active {
    background-color: rgb(66, 5, 83);
    box-shadow:
      0 2px 4px -1px rgba(0, 0, 0, 0.2),
      0 4px 5px 0 rgba(0, 0, 0, 0.14),
      0 1px 10px 0 rgba(0, 0, 0, 0.12);
  }

  border-radius: 0 0.275rem 0.275rem 0;
`;
const Input = styled(NumberField.Input)<{ $fill?: boolean }>`
  box-sizing: border-box;
  margin: 0;
  padding: 0;
  border-bottom: 1px solid rgba(255, 255, 255, 0.7);
  border-left: none;
  border-top: none;
  border-right: none;
  width: ${({ $fill }) => ($fill ? "100%" : "6rem")};
  height: 2rem;
  font-family: inherit;
  font-size: 1rem;
  font-weight: normal;
  background-color: transparent;
  color: #fff;

  text-align: center;
  font-variant-numeric: tabular-nums;

  &:focus {
    z-index: 1;
    outline: 0;
    outline-offset: -1px;
  }
`;

const InputWrapper = styled("div")<{ $fill?: boolean }>`
  position: relative;
  ${({ $fill }) => $fill && "flex: 1 1 auto; min-width: 0;"}

  &.focus {
    z-index: 1;
    outline-offset: -1px;
  }

  &:after {
    left: 0;
    top: 0;
    right: 0;
    height: 100%;
    content: "";
    position: absolute;
    pointer-events: none;
    border-bottom: 2px solid #5f0877;
    transition: transform 200ms cubic-bezier(0, 0, 0.2, 1) 0ms;
    transform: scaleX(0);
  }

  &.focus:after {
    transform: scaleX(1);
  }
`;

const Typography = styled(MuiTypography)(() => ({
  "&.MuiTypography-root": {
    userSelect: "none",
    pointerEvents: "none",
    flexShrink: 0,
    minWidth: "2.75rem",
    textAlign: "right",
  },
}));

interface NumberInputProps {
  defaultValue?: number;
  label?: string;
  value?: number;
  onValueChange?: (value: number | null) => void;
  /** Called after the typed value has been committed with Enter. */
  onCommit?: () => void;
  disabled?: boolean;
  /** Amount to increment/decrement with the +/- buttons and arrow keys. */
  step?: number;
  min?: number;
  max?: number;
  /** Fixed overall width. When set, the text field stretches to fill it. */
  width?: string;
}
export const NumberInput = ({
  defaultValue = 100,
  label = "",
  value,
  disabled = false,
  onValueChange = () => {},
  onCommit = () => {},
  step = 1,
  min,
  max,
  width,
}: NumberInputProps) => {
  const id = useId();
  const [focus, setFocus] = useState(false);

  // NumberField only parses the typed text on blur — Enter is treated as a plain
  // navigation key and leaves the value uncommitted. Bouncing focus off the input
  // runs that same commit path, so Enter submits without losing the caret.
  const handleKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key !== "Enter" || disabled) {
      return;
    }

    event.preventDefault();

    const input = event.currentTarget;
    input.blur();
    input.focus();

    onCommit();
  };

  return (
    <Root
      id={id}
      $width={width}
      onValueChange={(value, event) => {
        event?.preventDefault();
        onValueChange(value);
      }}
      value={value}
      disabled={disabled}
      defaultValue={defaultValue}
      step={step}
      min={min}
      max={max}
      onFocus={() => setFocus(true)}
      onBlur={() => setFocus(false)}
    >
      {label && (
        <Typography variant="caption" lineHeight={1.4} fontSize={19}>
          {label}
        </Typography>
      )}

      <Group $fill={!!width}>
        <Decrement whileTap={{ scale: 0.95 }} whileHover={{ scale: 1.03 }}>
          <MinusIcon />
        </Decrement>

        <InputWrapper $fill={!!width} className={focus ? "focus" : ""}>
          <Input $fill={!!width} onKeyDown={handleKeyDown} />
        </InputWrapper>
        <Increment whileTap={{ scale: 0.95 }} whileHover={{ scale: 1.03 }}>
          <PlusIcon />
        </Increment>
      </Group>
    </Root>
  );
};
