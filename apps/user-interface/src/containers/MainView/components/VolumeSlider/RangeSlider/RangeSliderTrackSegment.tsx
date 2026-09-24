import styled from "styled-components";
import type { CSSProperties } from "react";

const StyledTrackSegment = styled.div<{
  $background?: CSSProperties["background"];
  $border?: CSSProperties["border"];
  $width: CSSProperties["width"];
}>`
  width: ${(props) => `${props.$width}px`};
  height: 32px;
  background: ${(props) =>
    props.$background || "linear-gradient(90deg, #000000 0%, #0a0a0a 100%)"};
  box-shadow: rgba(255, 255, 255, 0.1) 0 1px 0;
  background-clip: padding-box;
  border: ${(props) => `solid 2px ${props.$border || "#111111"} `};
  position: relative;
  box-sizing: border-box;
  border-radius: 3px;
`;

type TrackSegmentProps = {
  background?: CSSProperties["background"];
  width?: CSSProperties["width"];
  border?: CSSProperties["border"];
};

export const RangeSliderTrackSegment = ({
  background,
  width,
  border,
}: TrackSegmentProps) => {
  return (
    <StyledTrackSegment
      $width={width}
      $background={background}
      $border={border}
    />
  );
};
