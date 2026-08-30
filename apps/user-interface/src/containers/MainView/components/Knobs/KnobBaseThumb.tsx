import { mapFrom01Linear } from "@dsp-ts/math";
import { styled } from "@mui/material";

type KnobBaseThumbProps = {
  readonly value01: number | undefined;
};

const StyledKnobWrapper = styled("div")({
  background: "linear-gradient(180deg, #323232 0%, #141414 100%)",
  borderRadius: "33px",
  width: "66px",
  height: "66px",
  boxShadow:
    "inset 0px 1px 0px rgb(99 99 99 / 65%), inset 0px -2px 0px rgba(0, 0, 0, 1)",
  margin: "auto",
});

const StyledThumbOuter = styled("div")({
  background: "linear-gradient(0deg, #282828 0%, #141414 100%)",
  borderRadius: "32px",
  width: "12px",
  height: "12px",
  position: "absolute",
  top: 12,
  left: 12,
  display: "flex",
  alignItems: "center",
  justifyContent: "center",
});

const StyledThumb = styled("div")({
  background: "#555353",
  borderRadius: "32px",
  border: "1px solid rgba(25,25,25,1)",
  borderStyle: "ridge",
  position: "absolute",
  width: "12px",
  height: "12px",
});

export function KnobBaseThumb({ value01 }: KnobBaseThumbProps) {
  const angleMin = -90;
  const angleMax = 170;
  const angle = mapFrom01Linear(value01 || 0, angleMin, angleMax);
  return (
    <div style={{ position: "relative" }}>
      <StyledKnobWrapper>
        <div
          style={{
            rotate: `${angle}deg`,
            height: "66px",
            width: "66px",
          }}
        >
          <StyledThumbOuter>
            <StyledThumb />
          </StyledThumbOuter>
        </div>
      </StyledKnobWrapper>
    </div>
  );
}
